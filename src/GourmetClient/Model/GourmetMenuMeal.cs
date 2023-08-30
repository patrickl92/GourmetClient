namespace GourmetClient.Model
{
	using System;

	public class GourmetMenuMeal
	{
		public GourmetMenuMeal(string productId, string name, string description)
		{
			ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description ?? throw new ArgumentNullException(nameof(description));
		}

		public string ProductId { get; }

		public string Name { get; }

		public string Description { get; }
	}
}
