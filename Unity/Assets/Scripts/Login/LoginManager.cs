using System.Collections.Generic;
using System.Text;
using Chat;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;

namespace Login
{
    public class LoginManager : MonoBehaviour
    {
        public static bool IsLoggedIn { get; private set; }

        public delegate void SuccessfulLoginEventHandler();
        public delegate void FailedLoginEventHandler(byte errorId);
        public delegate void SuccessfulAddUserEventHandler();
        public delegate void FailedAddUserEventHandler(byte errorId);

        public static event SuccessfulLoginEventHandler onSuccessfulLogin;
        public static event FailedLoginEventHandler onFailedLogin;
        public static event SuccessfulAddUserEventHandler onSuccessfulAddUser;
        public static event FailedAddUserEventHandler onFailedAddUser;

        private void Awake()
        {
            GameControl.Client.MessageReceived += OnDataHandler;
        }

        private void OnDestroy()
        {
            if (GameControl.Client == null)
                return;

            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        #region Network Calls

        public static void Login(string username, string password)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(username);
                writer.Write(Rsa.Encrypt(Encoding.UTF8.GetBytes(password)));

                using (var msg = Message.Create(LoginTags.LoginUser, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void AddUser(string username, string password)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(username);
                writer.Write(Rsa.Encrypt(Encoding.UTF8.GetBytes(password)));

                using (var msg = Message.Create(LoginTags.AddUser, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void Logout()
        {
            IsLoggedIn = false;
            ChatManager.Messages = new List<ChatMessage>();

            using (var msg = Message.CreateEmpty(LoginTags.LogoutUser))
            {
                GameControl.Client.SendMessage(msg, SendMode.Reliable);
            }
        }
        #endregion

        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag >= Tags.TagsPerPlugin * (Tags.Login + 1))
                    return;

                switch (message.Tag)
                {
                    case LoginTags.LoginSuccess:
                    {
                        IsLoggedIn = true;

                        onSuccessfulLogin?.Invoke();
                        break;
                    }

                    case LoginTags.LoginFailed:
                    {
                        using (var reader = message.GetReader())
                        {
                            if (reader.Length != 1)
                            {
                                Debug.LogWarning("Invalid LoginFailed Error data received.");
                                return;
                            }

                            onFailedLogin?.Invoke(reader.ReadByte());
                        }

                        break;
                    }

                    case LoginTags.AddUserSuccess:
                    {
                        onSuccessfulAddUser?.Invoke();
                        break;
                    }

                    case LoginTags.AddUserFailed:
                    {
                        using (var reader = message.GetReader())
                        {
                            if (reader.Length != 1)
                            {
                                Debug.LogWarning("Invalid LoginFailed Error data received.");
                                return;
                            }

                            onFailedAddUser?.Invoke(reader.ReadByte());
                        }

                        break;
                    }
                }
            }
        }
    }
}