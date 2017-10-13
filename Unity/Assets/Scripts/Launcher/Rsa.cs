using System;
using System.Security.Cryptography;
using UnityEngine;

namespace Launcher
{
    public class Rsa : MonoBehaviour
    {
        public static string Key;

        private void Awake()
        {
            try
            {
                var keyFile = Resources.Load("PublicKey") as TextAsset;
                Key = keyFile.text;
            }
            catch (Exception e)
            {
                Debug.Log("Failed to load key: " + e.Message + " - " + e.StackTrace);
            }
        }

        public static byte[] Encrypt(byte[] input)
        {
            byte[] encrypted;

            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(Key);
                encrypted = rsa.Encrypt(input, true);
            }
            return encrypted;
        }
    }
}