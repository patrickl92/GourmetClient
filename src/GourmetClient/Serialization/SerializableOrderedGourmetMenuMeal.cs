namespace GourmetClient.Serialization
{
	using System;
	using Model;

    internal class SerializableOrderedGourmetMenuMeal
	{
		public SerializableOrderedGourmetMenuMeal()
		{
			// Used for deserialization
			OrderId = string.Empty;
			Name = string.Empty;
		}

		public SerializableOrderedGourmetMenuMeal(OrderedGourmetMenuMeal orderedMeal)
		{
			orderedMeal = orderedMeal ?? throw new ArgumentNullException(nameof(orderedMeal));

			OrderId = orderedMeal.OrderId;
			Name = orderedMeal.Name;
			IsOrderApproved = orderedMeal.IsOrderApproved;
			IsOrderCancelable = orderedMeal.IsOrderCancelable;
		}

		public string OrderId { get; set; }

		public string Name { get; set; }

		public bool IsOrderApproved { get; set; }

		public bool IsOrderCancelable { get; set; }

		public OrderedGourmetMenuMeal ToOrderedGourmetMenuMeal()
		{
			return new OrderedGourmetMenuMeal(OrderId, Name, IsOrderApproved, IsOrderCancelable);
		}
	}
}
