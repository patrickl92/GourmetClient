namespace GourmetClient.Serialization
{
	using System;
	using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using Settings;
	using Utils;

    internal class SerializableUserSettings
	{
		public SerializableUserSettings()
		{
			// Used for deserialization
			CacheValidityMinutes = 0;
			GourmetLoginUsername = string.Empty;
			GourmetLoginPassword = string.Empty;
            VentopayUsername = string.Empty;
			VentopayPassword = string.Empty;
        }

		public SerializableUserSettings(UserSettings userSettings)
		{
			userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));

			CacheValidityMinutes = (int)userSettings.CacheValidity.TotalMinutes;
			GourmetLoginUsername = userSettings.GourmetLoginUsername;
			GourmetLoginPassword = Encrypt(userSettings.GourmetLoginPassword);
			VentopayUsername = userSettings.VentopayUsername;
			VentopayPassword = Encrypt(userSettings.VentopayPassword);
		}

		public int CacheValidityMinutes { get; set; }

		public string GourmetLoginUsername { get; set; }

		public string GourmetLoginPassword { get; set; }

		public string VentopayUsername { get; set; }

		public string VentopayPassword { get; set; }

		public UserSettings ToUserSettings()
		{
			var userSettings = new UserSettings
			{
				GourmetLoginUsername = GourmetLoginUsername,
				GourmetLoginPassword = Decrypt(GourmetLoginPassword),
				VentopayUsername = VentopayUsername,
				VentopayPassword = Decrypt(VentopayPassword)
			};

			if (CacheValidityMinutes > 0)
			{
				userSettings.CacheValidity = TimeSpan.FromMinutes(CacheValidityMinutes);
			}

			return userSettings;
		}

		private static string Encrypt(SecureString secureString)
		{
            if (secureString == null)
            {
                return null;
            }

			return EncryptionHelper.Encrypt(secureString.ToPlainPassword(), GetEncryptionKey());
		}

		private static SecureString Decrypt(string encryptedText)
		{
            if (encryptedText == null)
            {
                return null;
            }

			try
			{
				return StringToSecureString(EncryptionHelper.Decrypt(encryptedText, GetEncryptionKey()));
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static SecureString StringToSecureString(string plainText)
		{
			var secureString = new SecureString();

			foreach (var c in plainText.ToCharArray())
			{
				secureString.AppendChar(c);
			}

			return secureString;
		}

        private static string GetEncryptionKey()
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{userName}@{machineName}"));

            return Encoding.UTF8.GetString(hash);
        }
    }
}
