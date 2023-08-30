namespace GourmetClient.Utils
{
	using System;

	public static class DateTimeHelper
	{
		public static DateTime GetToday()
		{
			return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Utc);
		}
	}
}
