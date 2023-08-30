namespace GourmetClient.ViewModels
{
	using System.Security;
	using System.Threading.Tasks;
    using System.Windows.Input;

    using Behaviors;

    using Settings;
	using Utils;

	public class SettingsViewModel : ViewModelBase
	{
		private readonly GourmetSettingsService _settingsService;

		private string _loginUsername;

		private SecureString _loginPassword;

        private string _ventopayUsername;

		private SecureString _ventopayPassword;

        private bool _checkForUpdates;

		public SettingsViewModel()
		{
			_settingsService = InstanceProvider.SettingsService;

			SaveSettingsCommand = new AsyncDelegateCommand(SaveSettings);
		}

		public ICommand SaveSettingsCommand { get; }

		public string LoginUsername
		{
			get => _loginUsername;
            set
			{
				if (_loginUsername != value)
				{
					_loginUsername = value;
					OnPropertyChanged();
				}
			}
		}

		public SecureString LoginPassword
		{
			get => _loginPassword;
            set
			{
				_loginPassword = value;
				OnPropertyChanged();
			}
		}

		public string VentopayUsername
		{
			get => _ventopayUsername;
            set
			{
				if (_ventopayUsername != value)
				{
                    _ventopayUsername = value;
					OnPropertyChanged();
				}
			}
		}

		public SecureString VentopayPassword
		{
			get => _ventopayPassword;
            set
			{
                _ventopayPassword = value;
				OnPropertyChanged();
			}
		}

        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set
            {
                if (_checkForUpdates != value)
                {
                    _checkForUpdates = value;
                    OnPropertyChanged();
                }
            }
        }

		public override void Initialize()
		{
			var userSettings = _settingsService.GetCurrentUserSettings();
            var updateSettings = _settingsService.GetCurrentUpdateSettings();

			LoginUsername = userSettings.GourmetLoginUsername;
			LoginPassword = userSettings.GourmetLoginPassword;
            VentopayUsername = userSettings.VentopayUsername;
			VentopayPassword = userSettings.VentopayPassword;

            CheckForUpdates = updateSettings.CheckForUpdates;
        }

		private Task SaveSettings()
		{
			var userSettings = _settingsService.GetCurrentUserSettings();
            var updateSettings = _settingsService.GetCurrentUpdateSettings();

            userSettings.GourmetLoginUsername = LoginUsername;
			userSettings.GourmetLoginPassword = LoginPassword;
            userSettings.VentopayUsername = VentopayUsername;
            userSettings.VentopayPassword = VentopayPassword;

            _settingsService.SaveUserSettings(userSettings);

            if (updateSettings.CheckForUpdates != CheckForUpdates)
            {
                updateSettings.CheckForUpdates = CheckForUpdates;
                _settingsService.SaveUpdateSettings(updateSettings);
            }

            return Task.CompletedTask;
		}
	}
}
