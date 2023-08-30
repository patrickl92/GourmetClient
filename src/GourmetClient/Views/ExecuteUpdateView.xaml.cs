using GourmetClient.ViewModels;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GourmetClient.Views
{
    public partial class ExecuteUpdateView : UserControl
    {
        public ExecuteUpdateView()
        {
            InitializeComponent();

            DataContext = new ExecuteUpdateViewModel();
        }

        public Task ExecuteUpdate(string targetPath)
        {
            return ((ExecuteUpdateViewModel)DataContext).ExecuteUpdate(targetPath);
        }
    }
}
