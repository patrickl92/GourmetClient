namespace GourmetClient.Model
{
	using System;

	public class OrderedGourmetMenuDay
	{
		public OrderedGourmetMenuDay(DateTime date, OrderedGourmetMenuMeal meal)
		{
			Meal = meal ?? throw new ArgumentNullException(nameof(meal));

			Date = date;
		}

		public DateTime Date { get; }

		public OrderedGourmetMenuMeal Meal { get; }
	}
}
