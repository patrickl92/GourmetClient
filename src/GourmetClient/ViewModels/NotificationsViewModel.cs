namespace GourmetClient.ViewModels
{
    using GourmetClient.Behaviors;
    using GourmetClient.Notifications;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using Utils;

    public class NotificationsViewModel : ObservableObject
    {
        private readonly NotificationService _notificationService;

        public NotificationsViewModel()
        {
            _notificationService = InstanceProvider.NotificationService;

            DismissNotificationCommand = new AsyncDelegateCommand<Notification>(DismissNotification);
            StartUpdateCommand = new AsyncDelegateCommand<UpdateNotification>(StartUpdate, p => p?.StartUpdateAction != null);
            ShowExceptionDetailsCommand = new AsyncDelegateCommand<ExceptionNotification>(ShowExceptionDetails, p => p?.Exception != null);
        }

        public ICommand DismissNotificationCommand { get; }

        public ICommand StartUpdateCommand { get; }

        public ICommand ShowExceptionDetailsCommand { get; }

        public IReadOnlyList<Notification> Notifications => _notificationService.Notifications;

        private Task DismissNotification(Notification notification)
        {
            _notificationService.Dismiss(notification);
            return Task.CompletedTask;
        }

        private Task ShowExceptionDetails(ExceptionNotification notification)
        {
            var window = new ExceptionNotificationDetailWindow
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Notification = notification
            };
            window.ShowDialog();

            return Task.CompletedTask;
        }

        private Task StartUpdate(UpdateNotification notification)
        {
            notification.StartUpdateAction();
            return Task.CompletedTask;
        }
    }
}
