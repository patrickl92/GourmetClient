namespace GourmetClient.Network
{
	using System;

    public class GourmetRequestException : Exception
	{
		public GourmetRequestException(string message, string uriInfo)
			: base(message)
        {
            UriInfo = uriInfo;
        }

		public GourmetRequestException(string message, string uriInfo, Exception innerException)
			: base(message, innerException)
        {
            UriInfo = uriInfo;
        }

		public string UriInfo { get; }
    }
}
