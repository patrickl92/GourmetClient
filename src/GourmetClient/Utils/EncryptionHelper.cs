using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GourmetClient.Utils;

public static class EncryptionHelper
{
    // This constant is used to determine the key size of the encryption algorithm in bits.
    // We divide this by 8 within the code below to get the equivalent number of bytes.
    private const int KeySize = 128;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is prepended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        byte[] saltStringBytes = Generate128BitsOfRandomEntropy();
        byte[] ivStringBytes = Generate128BitsOfRandomEntropy();
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            
        using Aes symmetricKey = Aes.Create();
        symmetricKey.BlockSize = 128;
        symmetricKey.Mode = CipherMode.CBC;
        symmetricKey.Padding = PaddingMode.PKCS7;

        byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, saltStringBytes, DerivationIterations, HashAlgorithmName.SHA256, KeySize / 8);

        using ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();

        // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
        byte[] cipherTextBytes = saltStringBytes;
        cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
        cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();

        return Convert.ToBase64String(cipherTextBytes);
    }

    public static string Decrypt(string cipherText, string passPhrase)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);

        // Get the salt bytes by extracting the first 32 bytes from the supplied cipherText bytes.
        byte[] saltStringBytes = cipherTextBytesWithSaltAndIv
            .Take(KeySize / 8)
            .ToArray();

        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        byte[] ivStringBytes = cipherTextBytesWithSaltAndIv
            .Skip(KeySize / 8)
            .Take(KeySize / 8)
            .ToArray();

        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv
            .Skip((KeySize / 8) * 2)
            .Take(cipherTextBytesWithSaltAndIv.Length - ((KeySize / 8) * 2))
            .ToArray();

        using Aes symmetricKey = Aes.Create();
        symmetricKey.BlockSize = 128;
        symmetricKey.Mode = CipherMode.CBC;
        symmetricKey.Padding = PaddingMode.PKCS7;

        byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, saltStringBytes, DerivationIterations, HashAlgorithmName.SHA256, KeySize / 8);

        using ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
        using var memoryStream = new MemoryStream(cipherTextBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

        var plainTextBytes = new byte[cipherTextBytes.Length];
        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    }

    private static byte[] Generate128BitsOfRandomEntropy()
    {
        return RandomNumberGenerator.GetBytes(16);
    }
}