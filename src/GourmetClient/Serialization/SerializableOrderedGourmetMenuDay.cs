namespace GourmetClient.Serialization
{
	using System;
	using Model;

    internal class SerializableOrderedGourmetMenuDay
	{
		public SerializableOrderedGourmetMenuDay()
		{
			// Used for deserialization
		}

		public SerializableOrderedGourmetMenuDay(OrderedGourmetMenuDay orderedMenuDay)
		{
			orderedMenuDay = orderedMenuDay ?? throw new ArgumentNullException(nameof(orderedMenuDay));

			Day = orderedMenuDay.Date.Day;
			Month = orderedMenuDay.Date.Month;
			Year = orderedMenuDay.Date.Year;
			Meal = new SerializableOrderedGourmetMenuMeal(orderedMenuDay.Meal);
		}

		public int Day { get; set; }

		public int Month { get; set; }

		public int Year { get; set; }
		
		public SerializableOrderedGourmetMenuMeal Meal { get; set; }

		public OrderedGourmetMenuDay ToOrderedGourmetMenuDay()
		{
			var date = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Utc);
			return new OrderedGourmetMenuDay(date, Meal?.ToOrderedGourmetMenuMeal());
		}
	}
}
