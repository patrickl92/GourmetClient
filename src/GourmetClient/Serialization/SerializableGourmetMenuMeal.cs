namespace GourmetClient.Serialization
{
	using System;
	using Model;

    internal class SerializableGourmetMenuMeal
	{
		public SerializableGourmetMenuMeal()
		{
			// Used for deserialization
			Name = string.Empty;
			Description = string.Empty;
			ProductId = string.Empty;
		}

		public SerializableGourmetMenuMeal(GourmetMenuMeal meal)
		{
			meal = meal ?? throw new ArgumentNullException(nameof(meal));

			Name = meal.Name;
			Description = meal.Description;
			ProductId = meal.ProductId;
		}

		public string ProductId { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public GourmetMenuMeal ToGourmetMenuMeal()
		{
			return new GourmetMenuMeal(ProductId, Name, Description);
		}
	}
}
