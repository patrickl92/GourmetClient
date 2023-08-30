using System.ComponentModel;
using System.Reflection;
using System.Windows;
using GourmetClient.Update;

namespace GourmetClient
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Notifications;
    using Utils;

    public partial class App : Application
    {
        private const string ReleaseNotesTokenFileName = "ReleaseNotes.token";

        public static string LocalAppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GourmetClient");

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 1 && e.Args[0] == "/update")
            {
                StartUpdater(e.Args[1]);
            }
            else
            {
                var force = e.Args.Any(arg => arg == "/force");
                var checkForPreRelease = e.Args.Any(arg => arg == "/checkForPreRelease");
                
                StartApplication(force, checkForPreRelease || InstanceProvider.UpdateService.CurrentVersion.IsPrerelease);
            }
        }

        private void StartApplication(bool force, bool checkForPreRelease)
        {
            if (!force)
            {
                var runningInstance = GetRunningInstance();
                if (runningInstance != null)
                {
                    if (runningInstance.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(runningInstance.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(runningInstance.MainWindowHandle);
                    }

                    Current.Shutdown();
                    return;
                }
            }

            if (InstanceProvider.SettingsService.GetCurrentUpdateSettings().CheckForUpdates)
            {
                CheckForUpdates(checkForPreRelease);
            }

            var mainWindow = new MainWindow();

            if (!File.Exists(GetReleaseNotesTokenFilePath()))
            {
                mainWindow.Loaded += MainWindowOnLoaded;
            }

            mainWindow.Show();
        }

        private void MainWindowOnLoaded(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)sender;
            mainWindow.Loaded -= MainWindowOnLoaded;

            var releaseNotesWindow = new ReleaseNotesWindow
            {
                Owner = mainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            releaseNotesWindow.Show();

            File.Create(GetReleaseNotesTokenFilePath()).Dispose();
        }

        private async void StartUpdater(string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                MessageBox.Show("Der Pfad zum Zielverzeichnis ist ungültig. Das Verzeichnis existiert nicht.", "GourmetClient Updater", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            var maxTries = 50;
            var counter = 0;

            while (GetRunningInstance() != null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                counter++;

                if (counter >= maxTries)
                {
                    MessageBox.Show("GourmetClient wurde nicht beendet. Update kann nicht gestartet werden.", "GourmetClient Updater", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }
            }

            var executeUpdateWindow = new ExecuteUpdateWindow();
            executeUpdateWindow.Closing += ExecuteUpdateWindowOnClosing;

            executeUpdateWindow.Show();

            Exception updateException = null;

            try
            {
                await executeUpdateWindow.StartUpdate(targetPath);
            }
            catch (OperationCanceledException)
            {
            }
            catch (GourmetUpdateException exception)
            {
                updateException = exception;
            }
            finally
            {
                executeUpdateWindow.Closing -= ExecuteUpdateWindowOnClosing;
            }

            if (updateException != null)
            {
                new ExceptionNotificationDetailWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Notification = new ExceptionNotification("Bei der Durchführung des Updates ist ein Fehler aufgetreten", updateException)
                }.ShowDialog();
            }

            executeUpdateWindow.Close();
            Current.Shutdown();
        }

        private void ExecuteUpdateWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private Process GetRunningInstance()
        {
            var currentProcess = Process.GetCurrentProcess();
            return Process.GetProcessesByName(currentProcess.ProcessName).FirstOrDefault(process => process.Id != currentProcess.Id);
        }

        private async void CheckForUpdates(bool checkForPreRelease)
        {
            var updateRelease = await InstanceProvider.UpdateService.CheckForUpdate(checkForPreRelease);
            if (updateRelease != null)
            {
                InstanceProvider.NotificationService.Send(new UpdateNotification("Es ist eine neue Version verfügbar", () => StartUpdate(updateRelease)));
            }
        }

        private void StartUpdate(ReleaseDescription updateRelease)
        {
            var downloadUpdateWindow = new DownloadUpdateWindow
            {
                Owner = Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            downloadUpdateWindow.StartUpdate(updateRelease).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Dispatcher.Invoke(() => InstanceProvider.NotificationService.Send(new ExceptionNotification("Aktualisieren der Version ist fehlgeschlagen", task.Exception)));
                }

                Dispatcher.Invoke(() => downloadUpdateWindow.Close());
            });

            downloadUpdateWindow.ShowDialog();
        }

        private static string GetReleaseNotesTokenFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, ReleaseNotesTokenFileName);
        }

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
