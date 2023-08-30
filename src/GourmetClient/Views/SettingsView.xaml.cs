namespace GourmetClient.Views
{
	using System.Windows;
	using GourmetClient.ViewModels;

	/// <summary>
	/// Interaction logic for SettingsView.xaml
	/// </summary>
	public partial class SettingsView : InitializableView
	{
		public SettingsView()
		{
			InitializeComponent();

			DataContext = new SettingsViewModel();
		}

		private void LoginPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				((SettingsViewModel)DataContext).LoginPassword = LoginPasswordBox.SecurePassword;
			}
		}

        private void VentopayPasswordBox_OnPasswordChangedPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                ((SettingsViewModel)DataContext).VentopayPassword = VentopayPasswordBox.SecurePassword;
            }
        }
    }
}
