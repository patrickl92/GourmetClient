namespace GourmetClient.Views
{
    using GourmetClient.Update;
    using System.Threading.Tasks;

    using ViewModels;

    public partial class DownloadUpdateView : InitializableView
    {
        public DownloadUpdateView()
        {
            InitializeComponent();

            DataContext = new DownloadUpdateViewModel();
        }

        public Task StartUpdate(ReleaseDescription updateRelease)
        {
            return ((DownloadUpdateViewModel)DataContext).StartUpdate(updateRelease);
        }
    }
}
