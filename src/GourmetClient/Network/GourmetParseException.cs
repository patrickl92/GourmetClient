namespace GourmetClient.Network
{
	using System;
	using System.Runtime.Serialization;

	public class GourmetParseException : Exception
	{
		public GourmetParseException(string message, string uriInfo, string responseContent)
			: base(message)
        {
            UriInfo = uriInfo;
            ResponseContent = responseContent;
        }

		public GourmetParseException(string message, string uriInfo, string responseContent, Exception innerException)
			: base(message, innerException)
        {
            UriInfo = uriInfo;
            ResponseContent = responseContent;
        }

		protected GourmetParseException(SerializationInfo info, StreamingContext context)
			: base(info, context)
        {
            UriInfo = info.GetString("UriInfo");
            ResponseContent = info.GetString("ResponseContent");
        }

        public string UriInfo { get; }

        public string ResponseContent { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("UriInfo", UriInfo);
            info.AddValue("ResponseContent", ResponseContent);
        }
    }
}
