namespace GourmetClient.ViewModels
{
	using System;
	using GourmetClient.Model;
	using GourmetClient.Utils;

	public class GourmetMenuMealViewModel : ObservableObject
	{
		private readonly GourmetMenuMeal _menuMeal;

		private bool _isMealOrdered;

		private GourmetMenuMealState _mealState;

		private bool _isMealOrderApproved;

		private bool _isOrderCancelable;

		public GourmetMenuMealViewModel(GourmetMenuMeal meal)
		{
			_menuMeal = meal ?? throw new ArgumentNullException(nameof(meal));
		}

		public string ProductId => _menuMeal.ProductId;

		public string MealName => _menuMeal.Name;

		public string MealDescription => _menuMeal.Description;

		public bool IsMealOrdered
		{
			get => _isMealOrdered;

			set
			{
				if (_isMealOrdered != value)
				{
					_isMealOrdered = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsMealOrderApproved
		{
			get => _isMealOrderApproved;

			set
			{
				if (_isMealOrderApproved != value)
				{
					_isMealOrderApproved = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsOrderCancelable
		{
			get => _isOrderCancelable;

			set
			{
				if (_isOrderCancelable != value)
				{
					_isOrderCancelable = value;
					OnPropertyChanged();
				}
			}
		}

		public GourmetMenuMealState MealState
		{
			get => _mealState;

			set
			{
				if (_mealState != value)
				{
					_mealState = value;
					OnPropertyChanged();
				}
			}
		}

		public GourmetMenuMeal GetModel()
		{
			return _menuMeal;
		}
	}
}
