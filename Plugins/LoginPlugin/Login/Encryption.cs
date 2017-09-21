using System.Security.Cryptography;
using System.Text;

namespace LoginPlugin
{
    internal class Encryption
    {
        public static RSAParameters GenerateKeys(out RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                privateKey = rsa.ExportParameters(false);
                return rsa.ExportParameters(true);
            }
        }

        public static string Decrypt(byte[] input, RSAParameters key)
        {
            byte[] decrypted;
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(key);
                decrypted = rsa.Decrypt(input, false);
            }
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
