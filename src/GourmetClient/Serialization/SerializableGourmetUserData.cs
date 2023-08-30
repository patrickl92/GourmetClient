namespace GourmetClient.Serialization
{
	using System;
	using Model;

    internal class SerializableGourmetUserData
	{
		public SerializableGourmetUserData()
		{
			// Used for deserialization
			NameOfUser = string.Empty;
		}

		public SerializableGourmetUserData(GourmetUserData userData)
		{
			userData = userData ?? throw new ArgumentNullException(nameof(userData));

			NameOfUser = userData.NameOfUser;
		}

		public string NameOfUser { get; set; }

		public GourmetUserData ToGourmetUserData()
		{
			return new GourmetUserData(NameOfUser);
		}
	}
}
