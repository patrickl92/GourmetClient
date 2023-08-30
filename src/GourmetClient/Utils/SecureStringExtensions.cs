namespace GourmetClient.Utils
{
	using System;
	using System.Runtime.InteropServices;
	using System.Security;

	public static class SecureStringExtensions
	{
		public static string ToPlainPassword(this SecureString secureString)
		{
			secureString = secureString ?? throw new ArgumentNullException(nameof(secureString));

			IntPtr valuePtr = IntPtr.Zero;

			try
			{
				valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
				return Marshal.PtrToStringUni(valuePtr);
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
			}
		}
	}
}
