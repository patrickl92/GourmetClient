namespace GourmetClient.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using GourmetClient.Model;

	public class GourmetMenuViewModel
	{
		public GourmetMenuViewModel(GourmetMenu menu)
		{
			menu = menu ?? throw new ArgumentNullException(nameof(menu));

			Days = menu.Days.OrderBy(day => day.Date).Select(day => new GourmetMenuDayViewModel(day)).ToArray();
		}

		public IReadOnlyList<GourmetMenuDayViewModel> Days { get; }
	}
}
