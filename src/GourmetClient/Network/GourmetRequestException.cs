namespace GourmetClient.Network
{
	using System;
	using System.Runtime.Serialization;

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

		protected GourmetRequestException(SerializationInfo info, StreamingContext context)
			: base(info, context)
        {
            UriInfo = info.GetString("UriInfo");
        }

		public string UriInfo { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("UriInfo", UriInfo);
        }
    }
}
