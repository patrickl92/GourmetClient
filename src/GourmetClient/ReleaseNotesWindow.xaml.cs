using System.Windows;

namespace GourmetClient
{
    public partial class ReleaseNotesWindow : Window
    {
        public ReleaseNotesWindow()
        {
            InitializeComponent();
        }

        private void CloseButtonOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
