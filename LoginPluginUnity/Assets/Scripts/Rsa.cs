using System.Security.Cryptography;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;

public class Rsa : MonoBehaviour
{
    private UnityClient _client;

    public static RSAParameters Key;

    private void Awake()
    {
        _client = GetComponent<UnityClient>();
        _client.MessageReceived += OnDataHandler;
    }

    private void OnDataHandler(object sender, MessageReceivedEventArgs e)
    {
        var message = e.Message as TagSubjectMessage;

        if (message != null && message.Tag == Tags.Login)
        {
            if (message.Subject == LoginSubjects.Keys)
            {
                var reader = message.GetReader();
                Key.Exponent = reader.ReadBytes();
                Key.Modulus = reader.ReadBytes();
            }
        }
    }

    public static byte[] Encrypt(byte[] input, RSAParameters key)
    {
        byte[] encrypted;

        using (var rsa = new RSACryptoServiceProvider(4096))
        {
            rsa.PersistKeyInCsp = false;
            rsa.ImportParameters(key);
            encrypted = rsa.Encrypt(input, false);
        }
        return encrypted;
    }
}