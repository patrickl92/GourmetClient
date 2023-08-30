namespace GourmetClient.Update
{
	using System;
	using System.Runtime.Serialization;

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

		protected GourmetUpdateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
        {
        }

    }
}
