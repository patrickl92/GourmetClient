namespace GourmetClient.Network
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Threading.Tasks;
    using Model;

    using Notifications;

    using Settings;

    using Utils;

    public class BillingCacheService
    {
        private readonly GourmetSettingsService _settingsService;

        private readonly GourmetWebClient _gourmetWebClient;

        private readonly VentopayWebClient _ventopayWebClient;

        private readonly NotificationService _notificationService;

        public BillingCacheService()
        {
            _settingsService = InstanceProvider.SettingsService;
            _gourmetWebClient = InstanceProvider.GourmetWebClient;
            _ventopayWebClient = InstanceProvider.VentopayWebClient;
            _notificationService = InstanceProvider.NotificationService;
        }

        public async Task<IReadOnlyCollection<BillingPosition>> GetBillingPositions(int month, int year, IProgress<int> progress)
        {
            var gourmetProgressValue = 0;
            var ventopayProgressValue = 0;

            void UpdateProgress(ref int progressValue, int newValue)
            {
                progressValue = newValue;
                progress.Report((gourmetProgressValue + ventopayProgressValue) / 2);
            }

            var gourmetProgress = new Progress<int>(value => UpdateProgress(ref gourmetProgressValue, value));
            var ventopayProgress = new Progress<int>(value => UpdateProgress(ref ventopayProgressValue, value));

            var gourmetTask = GetGourmetBillingPositions(month, year, gourmetProgress);
            var ventopayTask = GetVentopayBillingPositions(month, year, ventopayProgress);

            var billingPositions = new List<BillingPosition>();
            
            var gourmetResult = await gourmetTask.ConfigureAwait(false);
            var ventopayResult = await ventopayTask.ConfigureAwait(false);

            billingPositions.AddRange(gourmetResult);
            billingPositions.AddRange(ventopayResult);

            return billingPositions;
        }

        private async Task<IReadOnlyList<BillingPosition>> GetGourmetBillingPositions(int month, int year, IProgress<int> progress)
        {
            var userSettings = _settingsService.GetCurrentUserSettings();

            if (string.IsNullOrEmpty(userSettings.GourmetLoginUsername))
            {
                _notificationService.Send(new Notification(NotificationType.Warning, "Zugangsdaten für Gourmet sind nicht konfiguriert. Abrechnungsdaten sind unvollständig"));
                progress.Report(100);

                return Array.Empty<BillingPosition>();
            }
            
            try
            {
                await using var loginHandle = await _gourmetWebClient.Login(userSettings.GourmetLoginUsername, userSettings.GourmetLoginPassword ?? new SecureString());
                if (!loginHandle.LoginSuccessful)
                {
                    _notificationService.Send(new Notification(NotificationType.Error, "Abrechnungsdaten von Gourmet konnten nicht geladen werden. Ursache: Login fehlgeschlagen"));
                    return Array.Empty<BillingPosition>();
                }

                return await _gourmetWebClient.GetBillingPositions(month, year, progress);
            }
            catch (Exception exception) when (exception is GourmetRequestException || exception is GourmetParseException)
            {
                _notificationService.Send(new ExceptionNotification("Abrechnungsdaten von Gourmet konnten nicht geladen werden", exception));
                return Array.Empty<BillingPosition>();
            }
            finally
            {
                progress.Report(100);
            }
        }

        private async Task<IReadOnlyList<BillingPosition>> GetVentopayBillingPositions(int month, int year, IProgress<int> progress)
        {
            var userSettings = _settingsService.GetCurrentUserSettings();

            if (string.IsNullOrEmpty(userSettings.VentopayUsername))
            {
                _notificationService.Send(new Notification(NotificationType.Warning, "Zugangsdaten für Ventopay sind nicht konfiguriert. Abrechnungsdaten sind unvollständig"));
                progress.Report(100);

                return Array.Empty<BillingPosition>();
            }

            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);
            
            try
            {
                await using var loginHandle = await _ventopayWebClient.Login(userSettings.VentopayUsername, userSettings.VentopayPassword ?? new SecureString());
                if (!loginHandle.LoginSuccessful)
                {
                    _notificationService.Send(new Notification(NotificationType.Error, "Abrechnungsdaten von Ventopay konnten nicht geladen werden. Ursache: Login fehlgeschlagen"));
                    return Array.Empty<BillingPosition>();
                }

                return await _ventopayWebClient.GetBillingPositions(fromDate, toDate, progress);
            }
            catch (Exception exception) when (exception is GourmetRequestException || exception is GourmetParseException)
            {
                _notificationService.Send(new ExceptionNotification("Abrechnungsdaten von Ventopay konnten nicht geladen werden", exception));
                return Array.Empty<BillingPosition>();
            }
            finally
            {
                progress.Report(100);
            }
        }
    }
}
