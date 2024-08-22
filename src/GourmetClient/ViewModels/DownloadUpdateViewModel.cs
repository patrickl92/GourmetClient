namespace GourmetClient.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Behaviors;

    using Notifications;

    using Update;

    using Utils;

    public class DownloadUpdateViewModel : ViewModelBase
    {
        private readonly UpdateService _updateService;

        private readonly NotificationService _notificationService;

        private int _downloadProgress;

        private UpdateStepState _downloadStepState;

        private UpdateStepState _extractStepState;

        private Task _updateTask;

        private CancellationTokenSource _cancellationTokenSource;

        public DownloadUpdateViewModel()
        {
            _updateService = InstanceProvider.UpdateService;
            _notificationService = InstanceProvider.NotificationService;

            CancelCommand = new AsyncDelegateCommand(CancelUpdate, () => _cancellationTokenSource != null);
        }

        public ICommand CancelCommand { get; }

        public int DownloadProgress
        {
            get => _downloadProgress;
            private set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public UpdateStepState DownloadStepState
        {
            get => _downloadStepState;
            private set
            {
                _downloadStepState = value;
                OnPropertyChanged();
            }
        }

        public UpdateStepState ExtractStepState
        {
            get => _extractStepState;
            private set
            {
                _extractStepState = value;
                OnPropertyChanged();
            }
        }

        public override void Initialize()
        {
        }

        public async Task StartUpdate(ReleaseDescription updateRelease)
        {
            var runningTask = _updateTask;
            if (runningTask != null)
            {
                await runningTask;
                return;
            }

            _cancellationTokenSource ??= new CancellationTokenSource();

            CommandManager.InvalidateRequerySuggested();

            try
            {
                _updateTask = StartUpdate(updateRelease, _cancellationTokenSource.Token);
                await _updateTask;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;

                _updateTask = null;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task StartUpdate(ReleaseDescription updateRelease, CancellationToken cancellationToken)
        {
            DownloadProgress = 0;
            DownloadStepState = UpdateStepState.Running;

            var progress = new Progress<int>();
            progress.ProgressChanged += OnDownloadProgressChanged;

            string packagePath;
            try
            {
                packagePath = await _updateService.DownloadUpdatePackage(updateRelease, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (GourmetUpdateException exception)
            {
                DownloadStepState = UpdateStepState.Error;
                _notificationService.Send(new ExceptionNotification("Beim Herunterladen der neuen Version ist ein Fehler aufgetreten", exception));
                return;
            }
            finally
            {
                progress.ProgressChanged -= OnDownloadProgressChanged;
            }

            DownloadStepState = UpdateStepState.Finished;
            ExtractStepState = UpdateStepState.Running;

            string extractedPackageLocation;
            try
            {
                extractedPackageLocation = await _updateService.ExtractUpdatePackage(packagePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (GourmetUpdateException exception)
            {
                ExtractStepState = UpdateStepState.Error;
                _notificationService.Send(new ExceptionNotification("Beim Entpacken der neuen Version ist ein Fehler aufgetreten", exception));
                return;
            }

            ExtractStepState = UpdateStepState.Finished;

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!_updateService.StartUpdate(extractedPackageLocation))
            {
                _notificationService.Send(new Notification(NotificationType.Error, "Update konnte nicht gestartet werden"));
            }
        }

        private Task CancelUpdate()
        {
            _cancellationTokenSource?.Cancel();
            return _updateTask ?? Task.CompletedTask;
        }

        private void OnDownloadProgressChanged(object sender, int e)
        {
            DownloadProgress = e;
        }
    }
}
