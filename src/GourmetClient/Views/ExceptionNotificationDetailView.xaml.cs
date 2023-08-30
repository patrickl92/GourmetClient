using System.Windows.Controls;

namespace GourmetClient.Views
{
    using Notifications;

    using ViewModels;

    public partial class ExceptionNotificationDetailView : UserControl
    {
        public ExceptionNotificationDetailView()
        {
            InitializeComponent();
        }

        public ExceptionNotification Notification
        {
            get => (DataContext as ExceptionNotificationDetailViewModel)?.GetNotification();
            set => DataContext = value != null ? new ExceptionNotificationDetailViewModel(value) : null;
        }
    }
}
