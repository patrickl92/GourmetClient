namespace GourmetClient.Notifications
{
    public class Notification
	{
		public Notification(NotificationType notificationType, string message)
		{
			NotificationType = notificationType;
			Message = message;
		}

		public NotificationType NotificationType { get; }

		public string Message { get; }
	}
}
