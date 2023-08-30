namespace GourmetClient.Model
{
	using System;
	using System.Collections.Generic;

	public class GourmetMenu
	{
		public GourmetMenu()
			: this(new GourmetMenuDay[0])
		{
		}

		public GourmetMenu(IReadOnlyList<GourmetMenuDay> days)
		{
			Days = days ?? throw new ArgumentNullException(nameof(days));
		}

		public IReadOnlyList<GourmetMenuDay> Days { get; }
	}
}
