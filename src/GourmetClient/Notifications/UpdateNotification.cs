namespace GourmetClient.Notifications
{
    using System;
    using System.Threading.Tasks;

    public class UpdateNotification : Notification
    {
        public UpdateNotification(string message, Action startUpdateCallback)
            : base(NotificationType.Information, message)
        {
            StartUpdateAction = startUpdateCallback;
        }

        public Action StartUpdateAction { get; }
    }
}
