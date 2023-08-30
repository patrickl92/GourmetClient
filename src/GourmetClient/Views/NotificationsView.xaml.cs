using System.Windows.Controls;

namespace GourmetClient.Views
{
    using ViewModels;

    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();

            DataContext = new NotificationsViewModel();
        }
    }
}
