using System.Security.Cryptography;
using System.Text;

namespace BBS2K
{
    public static class AesEncryption
    {
        private const string AESKEY = "XxyyQNwIl1CDD6cd";
        private static readonly byte[] _key;


        static AesEncryption()
        {
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(AESKEY));
        }
        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            byte[] iv = new byte[16];
            Array.Copy(cipherTextBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(cipherTextBytes, iv.Length, cipherTextBytes.Length - iv.Length);
            ms.Position = 0;

            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
