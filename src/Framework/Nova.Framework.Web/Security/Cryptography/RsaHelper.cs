using System.Security.Cryptography;

namespace Nova.Framework.Web.Security.Cryptography;

public static class RsaHelper
{
    /// <summary>
    /// 使用 RSA 私钥解密数据 (OAEP SHA-256)
    /// </summary>
    public static byte[] Decrypt(byte[] encryptedData, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());
        return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }
    
    /// <summary>
    /// 生成全新的 RSA 密钥对 (PEM 格式)
    /// </summary>
    public static (string PublicKey, string PrivateKey) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (
            rsa.ExportSubjectPublicKeyInfoPem(),
            rsa.ExportPkcs8PrivateKeyPem()
        );
    }
}
