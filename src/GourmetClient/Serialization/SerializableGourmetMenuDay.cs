namespace GourmetClient.Serialization
{
	using System;
    using System.Linq;
	using Model;

    internal class SerializableGourmetMenuDay
	{
		public SerializableGourmetMenuDay()
		{
			// Used for deserialization
		}

		public SerializableGourmetMenuDay(GourmetMenuDay menuDay)
		{
			menuDay = menuDay ?? throw new ArgumentNullException(nameof(menuDay));

			Day = menuDay.Date.Day;
			Month = menuDay.Date.Month;
			Year = menuDay.Date.Year;
			Meals = menuDay.Meals.Select(meal => new SerializableGourmetMenuMeal(meal)).ToArray();
		}

		public int Day { get; set; }

		public int Month { get; set; }

		public int Year { get; set; }

		public SerializableGourmetMenuMeal[] Meals { get; set; }

		public GourmetMenuDay ToGourmetMenuDay()
		{
			var date = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Utc);
			return new GourmetMenuDay(date, Meals?.Select(meal => meal.ToGourmetMenuMeal()).ToArray() ?? Array.Empty<GourmetMenuMeal>());
		}
	}
}
