using System.Threading.Tasks;
using System.Windows;

namespace GourmetClient
{
    public partial class ExecuteUpdateWindow : Window
    {
        public ExecuteUpdateWindow()
        {
            InitializeComponent();
        }

        public Task StartUpdate(string targetPath)
        {
            return ExecuteUpdateView.ExecuteUpdate(targetPath);
        }
    }
}
