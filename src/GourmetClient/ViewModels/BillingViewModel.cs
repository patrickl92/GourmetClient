namespace GourmetClient.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Model;

    using Network;

    using Utils;

    public class BillingViewModel : ViewModelBase
    {
        private readonly BillingCacheService _billingCacheService;

        private readonly ObservableCollection<DateTime> _availableMonths;

        private readonly ObservableCollection<GroupedBillingPositionsViewModel> _mealBillingPositions;

        private readonly ObservableCollection<GroupedBillingPositionsViewModel> _drinkBillingPositions;

        private readonly ObservableCollection<GroupedBillingPositionsViewModel> _unknownBillingPositions;

        private DateTime _selectedMonth;

        private double _sumCostMealBillingPositions;

        private double _sumCostDrinkBillingPositions;

        private double _sumCostUnknownBillingPositions;

        private bool _updating;

        private int _updateProgress;

        public BillingViewModel()
        {
            _billingCacheService = InstanceProvider.BillingCacheService;

            _availableMonths = new ObservableCollection<DateTime>();
            _mealBillingPositions = new ObservableCollection<GroupedBillingPositionsViewModel>();
            _drinkBillingPositions = new ObservableCollection<GroupedBillingPositionsViewModel>();
            _unknownBillingPositions = new ObservableCollection<GroupedBillingPositionsViewModel>();
        }

        public IReadOnlyList<DateTime> AvailableMonths => _availableMonths;

        public IReadOnlyList<GroupedBillingPositionsViewModel> MealBillingPositions => _mealBillingPositions;

        public IReadOnlyList<GroupedBillingPositionsViewModel> DrinkBillingPositions => _drinkBillingPositions;

        public IReadOnlyList<GroupedBillingPositionsViewModel> UnknownBillingPositions => _unknownBillingPositions;

        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (_selectedMonth != value)
                {
                    _selectedMonth = value;
                    OnPropertyChanged();
                    UpdateBillingPositions();
                }
            }
        }

        public double SumCostMealBillingPositions
        {
            get => _sumCostMealBillingPositions;
            private set
            {
                _sumCostMealBillingPositions = value;
                OnPropertyChanged();
            }
        }

        public double SumCostDrinkBillingPositions
        {
            get => _sumCostDrinkBillingPositions;
            private set
            {
                _sumCostDrinkBillingPositions = value;
                OnPropertyChanged();
            }
        }

        public double SumCostUnknownBillingPositions
        {
            get => _sumCostUnknownBillingPositions;
            private set
            {
                _sumCostUnknownBillingPositions = value;
                OnPropertyChanged();
            }
        }

        public bool IsUpdating
        {
            get => _updating;
            private set
            {
                _updating = value;
                OnPropertyChanged();
            }
        }

        public int UpdateProgress
        {
            get => _updateProgress;
            private set
            {
                _updateProgress = value;
                OnPropertyChanged();
            }
        }

        public override void Initialize()
        {
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            _availableMonths.Clear();
            _availableMonths.Add(DateTime.MinValue);

            for (int i = 0; i <= 3; i++)
            {
                _availableMonths.Add(currentMonth.AddMonths(i * (-1)));
            }

            SelectedMonth = DateTime.MinValue;
        }

        public void OnActivated()
        {

        }

        private async void UpdateBillingPositions()
        {
            IsUpdating = true;
            UpdateProgress = 0;

            _mealBillingPositions.Clear();
            _drinkBillingPositions.Clear();
            _unknownBillingPositions.Clear();

            SumCostMealBillingPositions = 0;
            SumCostDrinkBillingPositions = 0;
            SumCostUnknownBillingPositions = 0;

            if (_selectedMonth != DateTime.MinValue)
            {
                var progress = new Progress<int>();
                progress.ProgressChanged += OnUpdateProgressChanged;

                IReadOnlyCollection<BillingPosition> billingPositions;
                try
                {
                    billingPositions = await _billingCacheService.GetBillingPositions(_selectedMonth.Month, _selectedMonth.Year, progress);
                }
                finally
                {
                    progress.ProgressChanged -= OnUpdateProgressChanged;
                }

                var mealMenuBillingPositions = FindMealMenusBillingPositions(billingPositions).ToList();
                var remainingBillingPositions = billingPositions.Except(mealMenuBillingPositions).ToList();

                foreach (var viewModel in GroupMealMenusBillingPositions(mealMenuBillingPositions))
                {
                    _mealBillingPositions.Add(viewModel);
                }

                foreach (var viewModel in GroupBillingPositions(BillingPositionType.Meal, remainingBillingPositions))
                {
                    _mealBillingPositions.Add(viewModel);
                }

                foreach (var viewModel in GroupBillingPositions(BillingPositionType.Drink, remainingBillingPositions))
                {
                    _drinkBillingPositions.Add(viewModel);
                }

                foreach (var viewModel in GroupBillingPositions(BillingPositionType.Unknown, remainingBillingPositions))
                {
                    _unknownBillingPositions.Add(viewModel);
                }
            }

            SumCostMealBillingPositions = _mealBillingPositions.Sum(p => p.SumCost);
            SumCostDrinkBillingPositions = _drinkBillingPositions.Sum(p => p.SumCost);
            SumCostUnknownBillingPositions = _unknownBillingPositions.Sum(p => p.SumCost);

            UpdateProgress = 100;
            IsUpdating = false;
        }

        private IEnumerable<BillingPosition> FindMealMenusBillingPositions(IEnumerable<BillingPosition> billingPositions)
        {
            foreach (var billingPosition in billingPositions.Where(p => p.PositionType == BillingPositionType.Meal))
            {
                if (billingPosition.PositionName.StartsWith("Menü I ") || billingPosition.PositionName.StartsWith("Menü 1") ||
                    billingPosition.PositionName.StartsWith("Menü II ") || billingPosition.PositionName.StartsWith("Menü 2") ||
                    billingPosition.PositionName.StartsWith("Menü III ") || billingPosition.PositionName.StartsWith("Menü 3") ||
                    billingPosition.PositionName.StartsWith("Menü IV ") || billingPosition.PositionName.StartsWith("Menü 4"))
                {
                    yield return billingPosition;
                }
            }
        }

        private IEnumerable<GroupedBillingPositionsViewModel> GroupMealMenusBillingPositions(IReadOnlyCollection<BillingPosition> billingPositions)
        {
            var menu1Positions = billingPositions.Where(p => p.PositionName.StartsWith("Menü I ") || p.PositionName.StartsWith("Menü 1")).ToList();
            var menu2Positions = billingPositions.Where(p => p.PositionName.StartsWith("Menü II ") || p.PositionName.StartsWith("Menü 2")).ToList();
            var menu3Positions = billingPositions.Where(p => p.PositionName.StartsWith("Menü III ") || p.PositionName.StartsWith("Menü 3")).ToList();
            var menu4Positions = billingPositions.Where(p => p.PositionName.StartsWith("Menü IV ") || p.PositionName.StartsWith("Menü 4")).ToList();

            var groupedPositions = new List<GroupedBillingPositionsViewModel>();

            groupedPositions.AddRange(GroupMealMenusBillingPositions(menu1Positions, "Menü 1"));
            groupedPositions.AddRange(GroupMealMenusBillingPositions(menu2Positions, "Menü 2"));
            groupedPositions.AddRange(GroupMealMenusBillingPositions(menu3Positions, "Menü 3"));
            groupedPositions.AddRange(GroupMealMenusBillingPositions(menu4Positions, "Menü 4"));

            return groupedPositions;
        }

        private IEnumerable<GroupedBillingPositionsViewModel> GroupMealMenusBillingPositions(IReadOnlyCollection<BillingPosition> billingPositions, string groupName)
        {
            foreach (var singleCostGroup in billingPositions.GroupBy(position => position.SumCost / position.Count))
            {
                yield return new GroupedBillingPositionsViewModel(BillingPositionType.Meal, groupName, singleCostGroup.Sum(p => p.Count), singleCostGroup.Key, singleCostGroup.Sum(p => p.SumCost));
            }
        }

        private IEnumerable<GroupedBillingPositionsViewModel> GroupBillingPositions(BillingPositionType positionType, IReadOnlyCollection<BillingPosition> billingPositions)
        {
            var filteredPositions = billingPositions.Where(p => p.PositionType == positionType).ToList();

            foreach (var nameGroup in filteredPositions.GroupBy(p => p.PositionName))
            {
                foreach (var singleCostGroup in nameGroup.GroupBy(position => position.SumCost / position.Count))
                {
                    yield return new GroupedBillingPositionsViewModel(positionType, nameGroup.Key, singleCostGroup.Sum(p => p.Count), singleCostGroup.Key, singleCostGroup.Sum(p => p.SumCost));
                }
            }
        }

        private void OnUpdateProgressChanged(object sender, int e)
        {
            UpdateProgress = e;
        }
    }
}
