using System.Security.Cryptography;

namespace Nova.Framework.Web.Security.Cryptography;

public static class AesGcmHelper
{
    private const int NonceSize = 12; // 96-bit for AES-GCM
    private const int TagSize = 16;   // 128-bit MAC
    
    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(key);

        using var aesGcm = new AesGcm(key, TagSize);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Format: [Nonce] + [CipherText] + [Tag]
        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return result;
    }

    public static byte[] Decrypt(byte[] encryptedData, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(key);

        if (encryptedData.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("Invalid encrypted data length.", nameof(encryptedData));
        }

        using var aesGcm = new AesGcm(key, TagSize);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertextLength = encryptedData.Length - NonceSize - TagSize;
        var ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(encryptedData, NonceSize + ciphertextLength, tag, 0, TagSize);

        var plaintext = new byte[ciphertextLength];
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
