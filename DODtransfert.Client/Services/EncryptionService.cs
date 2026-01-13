using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DODtransfert.Client.Services;

public class EncryptionService
{
    private const int KeySize = 256;
    private const int IvSize = 128;
    private const int DerivationIterations = 10000;

    public byte[] Encrypt(byte[] data, string password)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var salt = GenerateRandomBytes(16);
        var key = DeriveKey(password, salt);
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        msEncrypt.Write(salt, 0, salt.Length);
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }

        return msEncrypt.ToArray();
    }

    public byte[] Decrypt(byte[] encryptedData, string password)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var msDecrypt = new MemoryStream(encryptedData);
        
        var salt = new byte[16];
        msDecrypt.Read(salt, 0, salt.Length);
        
        var iv = new byte[16];
        msDecrypt.Read(iv, 0, iv.Length);
        
        var key = DeriveKey(password, salt);
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var msResult = new MemoryStream();
        
        csDecrypt.CopyTo(msResult);
        return msResult.ToArray();
    }

    public string EncryptString(string plainText, string password)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = Encrypt(plainBytes, password);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string DecryptString(string encryptedText, string password)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var decryptedBytes = Decrypt(encryptedBytes, password);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, DerivationIterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize / 8);
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    public string GenerateSharedKey()
    {
        var keyBytes = GenerateRandomBytes(32);
        return Convert.ToBase64String(keyBytes);
    }
}
