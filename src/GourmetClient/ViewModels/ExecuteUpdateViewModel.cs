namespace GourmetClient.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Behaviors;
    using Update;

    using Utils;

    public class ExecuteUpdateViewModel : ViewModelBase
    {
        private readonly UpdateService _updateService;

        private UpdateStepState _createBackupStepState;

        private UpdateStepState _removePreviousVersionStepState;

        private UpdateStepState _copyNewFilesStepState;

        private UpdateStepState _cleanupStepState;

        private Task _updateTask;

        public ExecuteUpdateViewModel()
        {
            _updateService = InstanceProvider.UpdateService;
        }

        public UpdateStepState CreateBackupStepState
        {
            get => _createBackupStepState;
            private set
            {
                _createBackupStepState = value;
                OnPropertyChanged();
            }
        }

        public UpdateStepState RemovePreviousVersionStepState
        {
            get => _removePreviousVersionStepState;
            private set
            {
                _removePreviousVersionStepState = value;
                OnPropertyChanged();
            }
        }

        public UpdateStepState CopyNewFilesStepState
        {
            get => _copyNewFilesStepState;
            private set
            {
                _copyNewFilesStepState = value;
                OnPropertyChanged();
            }
        }

        public UpdateStepState CleanupStepState
        {
            get => _cleanupStepState;
            private set
            {
                _cleanupStepState = value;
                OnPropertyChanged();
            }
        }

        public override void Initialize()
        {
        }

        public async Task ExecuteUpdate(string targetPath)
        {
            var runningTask = _updateTask;
            if (runningTask != null)
            {
                await runningTask;
                return;
            }

            try
            {
                _updateTask = ExecuteUpdate(targetPath, CancellationToken.None);
                await _updateTask;
            }
            finally
            {
                _updateTask = null;
            }
        }

        private async Task ExecuteUpdate(string targetPath, CancellationToken cancellationToken)
        {
            //CreateBackupStepState = UpdateStepState.Running;

            //try
            //{
            //    await _updateService.CreateBackup(targetPath, cancellationToken);
            //}
            //catch (GourmetUpdateException)
            //{
            //    CreateBackupStepState = UpdateStepState.Error;
            //    throw;
            //}

            //CreateBackupStepState = UpdateStepState.Finished;
            RemovePreviousVersionStepState = UpdateStepState.Running;

            try
            {
                await _updateService.RemovePreviousVersion(targetPath, cancellationToken);
            }
            catch (GourmetUpdateException)
            {
                RemovePreviousVersionStepState = UpdateStepState.Error;
                throw;
            }

            RemovePreviousVersionStepState = UpdateStepState.Finished;
            CopyNewFilesStepState = UpdateStepState.Running;

            try
            {
                await _updateService.CopyCurrentVersion(targetPath, cancellationToken);
            }
            catch (GourmetUpdateException)
            {
                CopyNewFilesStepState = UpdateStepState.Error;
                throw;
            }

            CopyNewFilesStepState = UpdateStepState.Finished;
            CleanupStepState = UpdateStepState.Running;

            try
            {
                await _updateService.RemoveUpdateFiles(CancellationToken.None);
            }
            catch (GourmetUpdateException)
            {
                CleanupStepState = UpdateStepState.Error;
                throw;
            }

            CleanupStepState = UpdateStepState.Finished;

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!_updateService.StartNewVersion(targetPath))
            {
            }
        }
    }
}
