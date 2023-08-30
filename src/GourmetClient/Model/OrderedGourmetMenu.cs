namespace GourmetClient.Model
{
	using System;
	using System.Collections.Generic;

	public class OrderedGourmetMenu
	{
		public OrderedGourmetMenu()
			: this(new OrderedGourmetMenuDay[0])
		{
		}

		public OrderedGourmetMenu(IReadOnlyList<OrderedGourmetMenuDay> days)
		{
			Days = days ?? throw new ArgumentNullException(nameof(days));
		}

		public IReadOnlyList<OrderedGourmetMenuDay> Days { get; }
	}
}
