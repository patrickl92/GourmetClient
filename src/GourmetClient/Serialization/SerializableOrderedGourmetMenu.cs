namespace GourmetClient.Serialization
{
	using System;
    using System.Linq;
	using Model;

    internal class SerializableOrderedGourmetMenu
	{
		public SerializableOrderedGourmetMenu()
		{
			// Used for deserialization
		}

		public SerializableOrderedGourmetMenu(OrderedGourmetMenu orderedMenu)
		{
			orderedMenu = orderedMenu ?? throw new ArgumentNullException(nameof(orderedMenu));

			Days = orderedMenu.Days.Select(day => new SerializableOrderedGourmetMenuDay(day)).ToArray();
		}

		public SerializableOrderedGourmetMenuDay[] Days { get; set; }

		public OrderedGourmetMenu ToOrderedGourmetMenu()
		{
			return new OrderedGourmetMenu(Days?.Select(day => day.ToOrderedGourmetMenuDay()).ToArray() ?? Array.Empty<OrderedGourmetMenuDay>());
		}
	}
}
