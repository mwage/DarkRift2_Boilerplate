using System.Security.Cryptography;
using System.Text;

namespace LoginPlugin
{
    internal class Encryption
    {
        public static string Decrypt(byte[] input, string key)
        {
            byte[] decrypted;
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(key);
                decrypted = rsa.Decrypt(input, true);
            }
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}