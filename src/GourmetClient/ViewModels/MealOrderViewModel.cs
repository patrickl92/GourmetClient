namespace GourmetClient.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Behaviors;

    using GourmetClient.Model;
    using GourmetClient.Network;
    using GourmetClient.Settings;
    using GourmetClient.Utils;

    using Notifications;

    public class MealOrderViewModel : ViewModelBase
    {
        private readonly GourmetCacheService _cacheService;

        private readonly GourmetSettingsService _settingsService;

        private readonly NotificationService _notificationService;

        private GourmetCache _cache;

        private bool _showWelcomeMessage;

        private GourmetMenuViewModel _menu;

        private bool _isMenuUpdating;

        private string _nameOfUser;

        private DateTime _lastMenuUpdate;

        private bool _isSettingsPopupOpened;

        public MealOrderViewModel()
        {
            _cacheService = InstanceProvider.GourmetCacheService;
            _settingsService = InstanceProvider.SettingsService;
            _notificationService = InstanceProvider.NotificationService;

            UpdateMenuCommand = new AsyncDelegateCommand(ForceUpdateMenu, () => !IsMenuUpdating);
            ExecuteSelectedOrderCommand = new AsyncDelegateCommand(ExecuteSelectedOrder, () => !IsMenuUpdating);
            ToggleMealOrderedMarkCommand = new AsyncDelegateCommand<GourmetMenuMealViewModel>(ToggleMealOrderedMark, CanToggleMealOrderedMark);
        }

        public ICommand UpdateMenuCommand { get; }

        public ICommand ExecuteSelectedOrderCommand { get; }

        public ICommand ToggleMealOrderedMarkCommand { get; }

        public bool ShowWelcomeMessage
        {
            get => _showWelcomeMessage;

            private set
            {
                if (_showWelcomeMessage != value)
                {
                    _showWelcomeMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public GourmetMenuViewModel Menu
        {
            get => _menu;

            private set
            {
                if (_menu != value)
                {
                    _menu = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMenuUpdating
        {
            get => _isMenuUpdating;

            private set
            {
                if (_isMenuUpdating != value)
                {
                    _isMenuUpdating = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string NameOfUser
        {
            get => _nameOfUser;

            private set
            {
                if (_nameOfUser != value)
                {
                    _nameOfUser = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastMenuUpdate
        {
            get => _lastMenuUpdate;

            private set
            {
                if (_lastMenuUpdate != value)
                {
                    _lastMenuUpdate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSettingsPopupOpened
        {
            get => _isSettingsPopupOpened;

            set
            {
                if (_isSettingsPopupOpened != value)
                {
                    _isSettingsPopupOpened = value;
                    OnPropertyChanged();
                }
            }
        }

        public override async void Initialize()
        {
            _settingsService.SettingsSaved += SettingsServiceOnSettingsSaved;

            var userSettings = _settingsService.GetCurrentUserSettings();

            if (!string.IsNullOrEmpty(userSettings.GourmetLoginUsername))
            {
                IsMenuUpdating = true;

                try
                {
                    await UpdateMenu();
                }
                finally
                {
                    IsMenuUpdating = false;
                }
            }
            else
            {
                ShowWelcomeMessage = true;
            }
        }

        private async Task ForceUpdateMenu()
        {
            IsMenuUpdating = true;

            try
            {
                _cacheService.InvalidateCache();
                await UpdateMenu();
            }
            finally
            {
                IsMenuUpdating = false;
            }
        }

        private async Task UpdateMenu()
        {
            _cache = await _cacheService.GetCache();

            LastMenuUpdate = _cache.Timestamp;
            NameOfUser = _cache.UserData?.NameOfUser;

            var viewModelMenu = new GourmetMenuViewModel(_cache.Menu);

            foreach (var mealViewModel in viewModelMenu.Days.SelectMany(day => day.Meals))
            {
                if (string.IsNullOrEmpty(mealViewModel.ProductId))
                {
                    mealViewModel.MealState = GourmetMenuMealState.NotAvailable;
                }
            }

            foreach (var orderedDay in _cache.OrderedMenu.Days)
            {
                var dayViewModel = viewModelMenu.Days.SingleOrDefault(day => day.Date == orderedDay.Date);
                var mealViewModel = dayViewModel?.Meals.SingleOrDefault(meal => meal.MealName == orderedDay.Meal.Name);

                if (mealViewModel == null)
                {
                    mealViewModel = dayViewModel?.AddMeal(new GourmetMenuMeal(string.Empty, orderedDay.Meal.Name, string.Empty));
                }

                if (mealViewModel != null)
                {
                    mealViewModel.IsMealOrdered = true;
                    mealViewModel.IsMealOrderApproved = orderedDay.Meal.IsOrderApproved;
                    mealViewModel.IsOrderCancelable = orderedDay.Meal.IsOrderCancelable;
                    mealViewModel.MealState = GourmetMenuMealState.Ordered;
                }
            }

            Menu = viewModelMenu;
        }

        private async Task ExecuteSelectedOrder()
        {
            if (_menu == null)
            {
                return;
            }

            IsMenuUpdating = true;

            try
            {
                _cacheService.InvalidateCache();
                var currentData = await _cacheService.GetCache();

                var errorDays = new List<DateTime>();
                var mealsToOrder = new List<GourmetMenuMeal>();
                var mealsToCancel = new List<OrderedGourmetMenuMeal>();

                foreach (var dayViewModel in _menu.Days)
                {
                    var mealToOrder = dayViewModel.Meals.SingleOrDefault(meal => meal.MealState == GourmetMenuMealState.MarkedForOrder);

                    if (mealToOrder != null)
                    {
                        var actualDayData = currentData.Menu.Days.FirstOrDefault(day => day.Date == dayViewModel.Date);
                        var actualMealData =
                            actualDayData?.Meals.FirstOrDefault(meal => meal.Name == mealToOrder.MealName);

                        if (string.IsNullOrEmpty(actualMealData?.ProductId))
                        {
                            errorDays.Add(dayViewModel.Date);
                            _notificationService.Send(new Notification(NotificationType.Error, $"{mealToOrder.MealName} für den {dayViewModel.Date:dd.MM.yyyy} ist nicht mehr verfügbar"));
                        }
                        else
                        {
                            mealsToOrder.Add(mealToOrder.GetModel());
                        }
                    }
                }

                foreach (var dayViewModel in _menu.Days.Where(day => !errorDays.Contains(day.Date)))
                {
                    foreach (var mealViewModel in dayViewModel.Meals)
                    {
                        if (mealViewModel.MealState == GourmetMenuMealState.MarkedForCancel)
                        {
                            var actualOrderedDayData = currentData.OrderedMenu.Days.FirstOrDefault(day => day.Date == dayViewModel.Date && day.Meal.Name == mealViewModel.MealName);

                            if (actualOrderedDayData != null && actualOrderedDayData.Meal.IsOrderCancelable)
                            {
                                mealsToCancel.Add(GetOrderedMeal(mealViewModel.GetModel()));
                            }
                            else
                            {
                                _notificationService.Send(new Notification(NotificationType.Error, $"{mealViewModel.MealName} für den {dayViewModel.Date:dd.MM.yyyy} kann nicht storniert werden"));
                            }
                        }
                    }
                }

                await _cacheService.UpdateOrderedMenu(mealsToOrder, mealsToCancel);
                await UpdateMenu();
            }
            catch (Exception exception)
            {
                _notificationService.Send(new ExceptionNotification("Das Ausführen der Bestellung ist fehlgeschlagen", exception));
            }
            finally
            {
                IsMenuUpdating = false;
            }
        }

        private bool CanToggleMealOrderedMark(GourmetMenuMealViewModel mealViewModel)
        {
            if (mealViewModel == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(mealViewModel.ProductId))
            {
                if (mealViewModel.IsMealOrdered && mealViewModel.IsOrderCancelable)
                {
                    // Meal can no longer be ordered but it is ordered and the order can be canceled
                    return true;
                }

                // Meal can no longer be ordered
                return false;
            }

            if (mealViewModel.IsMealOrdered && !mealViewModel.IsOrderCancelable)
            {
                // Meal is ordered and the order cannot be canceled
                return false;
            }

            return true;
        }

        private Task ToggleMealOrderedMark(GourmetMenuMealViewModel mealViewModel)
        {
            if (mealViewModel.MealState == GourmetMenuMealState.Ordered)
            {
                mealViewModel.MealState = GourmetMenuMealState.MarkedForCancel;
            }
            else if (mealViewModel.MealState == GourmetMenuMealState.MarkedForOrder)
            {
                mealViewModel.MealState = GourmetMenuMealState.None;

                var orderedMeal = GetDayViewModel(mealViewModel)?.Meals.FirstOrDefault(meal => meal.IsMealOrdered);

                if (orderedMeal != null)
                {
                    orderedMeal.MealState = GourmetMenuMealState.Ordered;
                }
            }
            else
            {
                var dayViewModel = GetDayViewModel(mealViewModel);

                if (dayViewModel != null)
                {
                    foreach (var mealOfDay in GetMealsWhereOrderCanBeChanged(dayViewModel))
                    {
                        if (mealOfDay == mealViewModel)
                        {
                            mealOfDay.MealState = mealOfDay.IsMealOrdered ? GourmetMenuMealState.Ordered : GourmetMenuMealState.MarkedForOrder;
                        }
                        else
                        {
                            mealOfDay.MealState = mealOfDay.IsMealOrdered ? GourmetMenuMealState.MarkedForCancel : GourmetMenuMealState.None;
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private IEnumerable<GourmetMenuMealViewModel> GetMealsWhereOrderCanBeChanged(GourmetMenuDayViewModel dayViewModel)
        {
            return dayViewModel.Meals.Where(meal => meal.MealState != GourmetMenuMealState.NotAvailable && (!meal.IsMealOrdered || meal.IsOrderCancelable));
        }

        private GourmetMenuDayViewModel GetDayViewModel(GourmetMenuMealViewModel mealViewModel)
        {
            return _menu?.Days.First(day => day.Meals.Contains(mealViewModel));
        }

        private GourmetMenuDay GetDay(GourmetMenuMeal meal)
        {
            return _cache.Menu.Days.First(day => day.Meals.Contains(meal));
        }

        private OrderedGourmetMenuMeal GetOrderedMeal(GourmetMenuMeal meal)
        {
            var day = GetDay(meal);
            var orderedDay = _cache.OrderedMenu.Days.First(d => d.Date == day.Date);
            return orderedDay.Meal;
        }

        private async void SettingsServiceOnSettingsSaved(object sender, EventArgs e)
        {
            IsSettingsPopupOpened = false;
            ShowWelcomeMessage = false;

            await ForceUpdateMenu();
        }
    }
}
