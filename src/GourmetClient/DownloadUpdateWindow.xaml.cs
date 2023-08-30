using System.Windows;

namespace GourmetClient
{
    using GourmetClient.Update;
    using System.Threading.Tasks;

    public partial class DownloadUpdateWindow : Window
    {
        public DownloadUpdateWindow()
        {
            InitializeComponent();
        }

        public Task StartUpdate(ReleaseDescription updateRelease)
        {
            return DownloadUpdateView.StartUpdate(updateRelease);
        }
    }
}
