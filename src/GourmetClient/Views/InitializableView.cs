namespace GourmetClient.Views
{
	using System.Windows;
	using System.Windows.Controls;
	using GourmetClient.ViewModels;

	public abstract class InitializableView : UserControl
	{
		protected InitializableView()
		{
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			var viewModel = DataContext as ViewModelBase;
			viewModel?.Initialize();
		}
	}
}
