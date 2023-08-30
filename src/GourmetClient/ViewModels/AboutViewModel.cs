using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using GourmetClient.Behaviors;
using GourmetClient.Utils;

namespace GourmetClient.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        public AboutViewModel()
        {
            AppVersion = InstanceProvider.UpdateService.CurrentVersion.ToString();

            ShowReleaseNotesCommand = new DelegateCommand(ShowReleaseNotes);
            OpenIconAuthorWebPageCommand = new DelegateCommand(() => OpenUrlInBrowser("https://www.flaticon.com/authors/smashicons"));
            OpenIconWebPageCommand = new DelegateCommand(() => OpenUrlInBrowser("https://www.flaticon.com"));
        }

        public string AppVersion { get; }

        public ICommand ShowReleaseNotesCommand { get; }

        public ICommand OpenIconAuthorWebPageCommand { get; }

        public ICommand OpenIconWebPageCommand { get; }

        public override void Initialize()
        {
        }

        private void ShowReleaseNotes()
        {
            var releaseNotesWindow = new ReleaseNotesWindow
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            releaseNotesWindow.ShowDialog();
        }

        private void OpenUrlInBrowser(string url)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
