using GourmetClient.Notifications;

using System.Windows;

namespace GourmetClient
{
    public partial class ExceptionNotificationDetailWindow : Window
    {
        public ExceptionNotificationDetailWindow()
        {
            InitializeComponent();
        }

        public ExceptionNotification Notification
        {
            get => NotificationDetailView.Notification;
            set => NotificationDetailView.Notification = value;
        }

        private void CloseButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
