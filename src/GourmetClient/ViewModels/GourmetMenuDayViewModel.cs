namespace GourmetClient.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using GourmetClient.Model;

	public class GourmetMenuDayViewModel
	{
		private readonly GourmetMenuDay _menuDay;

		private readonly ObservableCollection<GourmetMenuMealViewModel> _meals;

		public GourmetMenuDayViewModel(GourmetMenuDay menuDay)
		{
			_menuDay = menuDay ?? throw new ArgumentNullException(nameof(menuDay));

			_meals = new ObservableCollection<GourmetMenuMealViewModel>(_menuDay.Meals.Select(meal => new GourmetMenuMealViewModel(meal)));
		}

		public DateTime Date => _menuDay.Date;

		public IReadOnlyList<GourmetMenuMealViewModel> Meals => _meals;

		public GourmetMenuMealViewModel AddMeal(GourmetMenuMeal meal)
		{
			var viewModel = new GourmetMenuMealViewModel(meal);
			_meals.Add(viewModel);

			return viewModel;
		}
	}
}
