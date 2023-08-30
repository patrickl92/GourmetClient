using System.Windows.Controls;

namespace GourmetClient.Views
{
    using System.Diagnostics;
    using System.Windows;

    public partial class LoadingSpinner : UserControl
    {
        public static readonly DependencyProperty IsSpinningProperty = DependencyProperty.Register(
            "IsSpinning",
            typeof(bool),
            typeof(LoadingSpinner),
            new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty SpinnerThicknessProperty = DependencyProperty.Register(
            "SpinnerThickness",
            typeof(double),
            typeof(LoadingSpinner),
            new FrameworkPropertyMetadata(2.0, OnSpinnerThicknessChanged));

        public LoadingSpinner()
        {
            InitializeComponent();

            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateSize();
        }

        public bool IsSpinning
        {
            get => (bool)GetValue(IsSpinningProperty);
            set => SetValue(IsSpinningProperty, value);
        }

        public double SpinnerThickness
        {
            get => (double)GetValue(SpinnerThicknessProperty);
            set => SetValue(SpinnerThicknessProperty, value);
        }

        private static void OnSpinnerThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LoadingSpinner)?.RecalculateSize();
        }

        private void RecalculateSize()
        {
            SpinnerPathFigure.StartPoint = new Point(ActualWidth / 2, SpinnerThickness / 2);
            SpinnerArcSegment.Point = new Point(ActualWidth - (SpinnerThickness / 2), ActualHeight / 2);
            SpinnerArcSegment.Size = new Size(ActualWidth / 2, ActualHeight / 2);
        }
    }
}
