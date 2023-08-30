namespace GourmetClient.Utils
{
	using GourmetClient.Network;
	using GourmetClient.Settings;

    using Notifications;

    using Update;

    public static class InstanceProvider
	{
		private static GourmetWebClient _gourmetWebClient;

		private static VentopayWebClient _ventopayWebClient;

		private static GourmetCacheService _gourmetCacheService;

        private static NotificationService _notificationService;

		private static BillingCacheService _billingCacheService;

		private static GourmetSettingsService _settingsService;

		private static UpdateService _updateService;

		public static GourmetWebClient GourmetWebClient => _gourmetWebClient ??= new GourmetWebClient();

		public static VentopayWebClient VentopayWebClient => _ventopayWebClient ??= new VentopayWebClient();

		public static GourmetCacheService GourmetCacheService => _gourmetCacheService ??= new GourmetCacheService();

		public static NotificationService NotificationService => _notificationService ??= new NotificationService();

		public static BillingCacheService BillingCacheService => _billingCacheService ??= new BillingCacheService();

		public static GourmetSettingsService SettingsService => _settingsService ??= new GourmetSettingsService();

		public static UpdateService UpdateService => _updateService ??= new UpdateService();
	}
}
