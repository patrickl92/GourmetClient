using GourmetClient.ViewModels;

namespace GourmetClient.Views
{
    using System.Windows.Controls;
	
	public partial class AboutView : UserControl
	{
		public AboutView()
        {
			InitializeComponent();

            DataContext = new AboutViewModel();
        }
	}
}
