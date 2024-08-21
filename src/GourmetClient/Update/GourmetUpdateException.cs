namespace GourmetClient.Update
{
	using System;

    public class GourmetUpdateException : Exception
	{
		public GourmetUpdateException(string message)
			: base(message)
        {
        }

		public GourmetUpdateException(string message, Exception innerException)
			: base(message, innerException)
        {
        }
    }
}
