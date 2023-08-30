namespace GourmetClient.Notifications
{
    using System.Collections.ObjectModel;

    public class NotificationService
    {
        private readonly ObservableCollection<Notification> _notifications;

        public NotificationService()
        {
            _notifications = new ObservableCollection<Notification>();
            Notifications = new ReadOnlyObservableCollection<Notification>(_notifications);
        }

        public ReadOnlyObservableCollection<Notification> Notifications { get; }

        public void Send(Notification notification)
        {
            _notifications.Add(notification);
        }

        public void Dismiss(Notification notification)
        {
            _notifications.Remove(notification);
        }
    }
}
