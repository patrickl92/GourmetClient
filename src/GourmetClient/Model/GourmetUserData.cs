namespace GourmetClient.Model
{
	using System;

	public class GourmetUserData
	{
		public GourmetUserData(string nameOfUser)
		{
			NameOfUser = nameOfUser ?? throw new ArgumentNullException(nameof(nameOfUser));
		}

		public string NameOfUser { get; }
	}
}
