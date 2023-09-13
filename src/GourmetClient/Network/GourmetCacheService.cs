using System.Security;
using System.Text.Json;

namespace GourmetClient.Network
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Model;
    using Serialization;
    using Settings;
    using Utils;

    using Notifications;

    public class GourmetCacheService
    {
        private readonly GourmetWebClient _webClient;

        private readonly GourmetSettingsService _settingsService;

        private readonly NotificationService _notificationService;

        private readonly string _cacheFileName;

        private GourmetCache _cache;

        public GourmetCacheService()
        {
            _webClient = InstanceProvider.GourmetWebClient;
            _settingsService = InstanceProvider.SettingsService;
            _notificationService = InstanceProvider.NotificationService;

            _cacheFileName = Path.Combine(App.LocalAppDataPath, "GourmetCache.json");
        }

        public void InvalidateCache()
        {
            _cache = new GourmetCache();
        }

        public async Task<GourmetCache> GetCache()
        {
            if (_cache == null)
            {
                _cache = await GetCacheFromFile();
            }

            var userSettings = _settingsService.GetCurrentUserSettings();

            if (_cache.Timestamp.Add(userSettings.CacheValidity) < DateTime.Now)
            {
                await UpdateCache();
            }

            return _cache;
        }

        public async Task UpdateOrderedMenu(IReadOnlyList<GourmetMenuMeal> mealsToOrder, IReadOnlyList<OrderedGourmetMenuMeal> mealsToCancel)
        {
            mealsToOrder = mealsToOrder ?? throw new ArgumentNullException(nameof(mealsToOrder));
            mealsToCancel = mealsToCancel ?? throw new ArgumentNullException(nameof(mealsToCancel));

            var userSettings = _settingsService.GetCurrentUserSettings();

            if (string.IsNullOrEmpty(userSettings.GourmetLoginUsername))
            {
                return;
            }

            await using var loginHandle = await _webClient.Login(userSettings.GourmetLoginUsername, userSettings.GourmetLoginPassword ?? new SecureString());

            if (!loginHandle.LoginSuccessful)
            {
                return;
            }

            try
            {
                foreach (var orderedMeal in mealsToCancel)
                {
                    await _webClient.CancelOrder(orderedMeal);
                }

                foreach (var meal in mealsToOrder)
                {
                    await _webClient.AddMealToOrderedMenu(meal);
                }

                await _webClient.ConfirmOrder();
            }
            finally
            {
                InvalidateCache();
            }
        }

        private async Task UpdateCache()
        {
            var serverData = await GetDataFromServer();
            var cachedMenu = await GetCacheFromFile();

            var updatedMenu = MergeMenus(serverData.Menu, cachedMenu.Menu);

            _cache = new GourmetCache(DateTime.Now, serverData.UserData, updatedMenu, serverData.OrderedMenu);

            await SaveMenuCache(_cache);
        }

        private GourmetMenu MergeMenus(GourmetMenu serverMenu, GourmetMenu cachedMenu)
        {
            var today = DateTimeHelper.GetToday();
            var mergedDays = new List<GourmetMenuDay>();

            if (cachedMenu != null)
            {
                foreach (var cachedDay in cachedMenu.Days)
                {
                    if (cachedDay.Date < today)
                    {
                        // Ignore past days
                        continue;
                    }

                    var meals = new List<GourmetMenuMeal>();
                    var serverDay = serverMenu.Days.SingleOrDefault(day => day.Date == cachedDay.Date);

                    if (serverDay != null)
                    {
                        foreach (var serverMeal in serverDay.Meals)
                        {
                            var description = serverMeal.Description;

                            if (string.IsNullOrEmpty(description))
                            {
                                // Used the cached description if available
                                var cachedMeal = cachedDay.Meals.FirstOrDefault(meal => meal.Name == serverMeal.Name);
                                if (cachedMeal != null)
                                {
                                    description = cachedMeal.Description;
                                }
                            }

                            meals.Add(new GourmetMenuMeal(serverMeal.ProductId, serverMeal.Name, description));
                        }
                    }

                    foreach (var cachedMeal in cachedDay.Meals)
                    {
                        if (meals.All(meal => meal.Name != cachedMeal.Name))
                        {
                            // Set ProductID to empty string if the cached meal is no longer provided from the server
                            meals.Add(new GourmetMenuMeal(string.Empty, cachedMeal.Name, cachedMeal.Description));
                        }
                    }

                    mergedDays.Add(new GourmetMenuDay(cachedDay.Date, meals));
                }
            }

            foreach (var serverDay in serverMenu.Days)
            {
                if (serverDay.Date < today)
                {
                    // Ignore past days
                    continue;
                }

                if (mergedDays.All(day => day.Date != serverDay.Date))
                {
                    mergedDays.Add(new GourmetMenuDay(serverDay.Date, serverDay.Meals.ToArray()));
                }
            }

            return new GourmetMenu(mergedDays);
        }

        private async Task<(GourmetUserData UserData, GourmetMenu Menu, OrderedGourmetMenu OrderedMenu)> GetDataFromServer()
        {
            var userSettings = _settingsService.GetCurrentUserSettings();

            if (string.IsNullOrEmpty(userSettings.GourmetLoginUsername))
            {
                return (null, new GourmetMenu(), new OrderedGourmetMenu());
            }

            try
            {
                await using var loginHandle = await _webClient.Login(userSettings.GourmetLoginUsername, userSettings.GourmetLoginPassword ?? new SecureString());

                if (!loginHandle.LoginSuccessful)
                {
                    _notificationService.Send(new Notification(NotificationType.Error, "Daten konnten nicht aktualisiert werden. Ursache: Login fehlgeschlagen"));
                    return (null, new GourmetMenu(), new OrderedGourmetMenu());
                }

                var userData = await _webClient.GetUserData();
                var menu = await _webClient.GetMenu();
                var orderedMenu = await _webClient.GetOrderedMenu();

                return (userData, menu, orderedMenu);
            }
            catch (Exception exception) when (exception is GourmetRequestException || exception is GourmetParseException)
            {
                _notificationService.Send(new ExceptionNotification("Daten konnten nicht aktualisiert werden", exception));
                return (null, new GourmetMenu(), new OrderedGourmetMenu());
            }
        }

        private async Task<GourmetCache> GetCacheFromFile()
        {
            if (!File.Exists(_cacheFileName))
            {
                return new GourmetCache();
            }

            try
            {
                await using var fileStream = new FileStream(_cacheFileName, FileMode.Open, FileAccess.Read, FileShare.None);
                var serializedCache = await JsonSerializer.DeserializeAsync<SerializableGourmetCache>(fileStream);
                
                return serializedCache.ToGourmetMenuCache();
            }
            catch
            {
                return new GourmetCache();
            }
        }

        private async Task SaveMenuCache(GourmetCache menuCache)
        {
            menuCache = menuCache ?? throw new ArgumentNullException(nameof(menuCache));

            var serializedCache = new SerializableGourmetCache(menuCache);

            try
            {
                var cacheDirectory = Path.GetDirectoryName(_cacheFileName);
                if (cacheDirectory != null && !Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }
                
                await using var fileStream = new FileStream(_cacheFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(fileStream, serializedCache, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                InvalidateCache();
            }
        }
    }
}
