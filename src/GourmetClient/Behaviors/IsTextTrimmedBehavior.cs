namespace GourmetClient.Behaviors
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using Microsoft.Xaml.Behaviors;

	public class IsTextTrimmedBehavior : Behavior<TextBlock>
	{
		private static readonly DependencyPropertyKey IsTextTrimmedKey = DependencyProperty.RegisterAttachedReadOnly(
			"IsTextTrimmed",
			typeof(bool),
			typeof(IsTextTrimmedBehavior),
			new PropertyMetadata(false));

		public static readonly DependencyProperty IsTextTrimmedProperty = IsTextTrimmedKey.DependencyProperty;

		[AttachedPropertyBrowsableForType(typeof(TextBlock))]
		public static bool GetIsTextTrimmed(TextBlock target)
		{
			return (bool)target.GetValue(IsTextTrimmedProperty);
		}

		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.SizeChanged += AssociatedObjectOnSizeChanged;
			AssociatedObject.DataContextChanged += AssociatedObjectOnDataContextChanged;

			RecalculateIsTextTrimmed(AssociatedObject);
		}

		protected override void OnDetaching()
		{
			AssociatedObject.SizeChanged -= AssociatedObjectOnSizeChanged;
			AssociatedObject.DataContextChanged -= AssociatedObjectOnDataContextChanged;

			base.OnDetaching();
		}

		private void AssociatedObjectOnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			RecalculateIsTextTrimmed(AssociatedObject);
		}

		private void AssociatedObjectOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			RecalculateIsTextTrimmed(AssociatedObject);
		}

		private static void RecalculateIsTextTrimmed(TextBlock textBlock)
		{
			if (textBlock.TextTrimming == TextTrimming.None)
			{
				textBlock.SetValue(IsTextTrimmedKey, false);
			}
			else
			{
				var isTextTrimmed = CalculateIsTextTrimmed(textBlock);
				textBlock.SetValue(IsTextTrimmedKey, isTextTrimmed);
			}
		}

		private static bool CalculateIsTextTrimmed(TextBlock textBlock)
		{
			if (!textBlock.IsArrangeValid)
			{
				return GetIsTextTrimmed(textBlock);
			}

			Typeface typeface = new Typeface(
				textBlock.FontFamily,
				textBlock.FontStyle,
				textBlock.FontWeight,
				textBlock.FontStretch);

			// FormattedText is used to measure the whole width of the text held up by TextBlock container
			FormattedText formattedText = new FormattedText(
				textBlock.Text,
				System.Threading.Thread.CurrentThread.CurrentCulture,
				textBlock.FlowDirection,
				typeface,
				textBlock.FontSize,
				textBlock.Foreground,
				VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

			formattedText.MaxTextWidth = textBlock.ActualWidth;

			// When the maximum text width of the FormattedText instance is set to the actual
			// width of the textBlock, if the textBlock is being trimmed to fit then the formatted
			// text will report a larger height than the textBlock. Should work whether the
			// textBlock is single or multi-line.
			// The "formattedText.MinWidth > formattedText.MaxTextWidth" check detects if any 
			// single line is too long to fit within the text area, this can only happen if there is a 
			// long span of text with no spaces.
			return (formattedText.Height > textBlock.ActualHeight || formattedText.MinWidth > formattedText.MaxTextWidth);
		}
	}
}
