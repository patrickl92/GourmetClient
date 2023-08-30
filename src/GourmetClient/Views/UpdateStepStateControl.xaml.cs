using System.Windows;
using System.Windows.Controls;

namespace GourmetClient.Views
{
    using Update;

    public partial class UpdateStepStateControl : UserControl
    {
        public static readonly DependencyProperty StepStateProperty = DependencyProperty.Register(
            "StepState",
            typeof(UpdateStepState),
            typeof(UpdateStepStateControl),
            new FrameworkPropertyMetadata(UpdateStepState.NotStarted));

        public static readonly DependencyProperty StepTextProperty = DependencyProperty.Register(
            "StepText",
            typeof(string),
            typeof(UpdateStepStateControl),
            new FrameworkPropertyMetadata(string.Empty));

        public UpdateStepStateControl()
        {
            InitializeComponent();
        }

        public UpdateStepState StepState
        {
            get => (UpdateStepState)GetValue(StepStateProperty);
            set => SetValue(StepStateProperty, value);
        }

        public string StepText
        {
            get => (string)GetValue(StepTextProperty);
            set => SetValue(StepTextProperty, value);
        }
    }
}
