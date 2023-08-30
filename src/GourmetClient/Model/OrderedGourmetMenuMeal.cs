namespace GourmetClient.Model
{
	using System;

	public class OrderedGourmetMenuMeal
	{
		public OrderedGourmetMenuMeal(string orderId, string name, bool isOrderApproved, bool isOrderCancelable)
		{
			OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
			Name = name ?? throw new ArgumentNullException(nameof(name));

			IsOrderApproved = isOrderApproved;
			IsOrderCancelable = isOrderCancelable;
		}

		public string OrderId { get; }

		public string Name { get; }

		public bool IsOrderApproved { get; }

		public bool IsOrderCancelable { get; }
	}
}
