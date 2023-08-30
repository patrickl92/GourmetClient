namespace GourmetClient.Behaviors
{
    using System.Windows;

    public class BoolToVisibilityConverter : BoolConverterBase<Visibility>
    {
        public BoolToVisibilityConverter()
            : base(Visibility.Visible, Visibility.Collapsed)
        {
        }
    }
}
