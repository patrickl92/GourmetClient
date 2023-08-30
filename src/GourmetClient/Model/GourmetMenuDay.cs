namespace GourmetClient.Model
{
	using System;
	using System.Collections.Generic;

	public class GourmetMenuDay
	{
		public GourmetMenuDay(DateTime date, IReadOnlyList<GourmetMenuMeal> meals)
		{
			Meals = meals ?? throw new ArgumentNullException(nameof(meals));

			Date = date;
		}

		public DateTime Date { get; }

		public IReadOnlyList<GourmetMenuMeal> Meals { get; }
	}
}
