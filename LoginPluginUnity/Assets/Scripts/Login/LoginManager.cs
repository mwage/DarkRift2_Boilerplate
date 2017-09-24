using System.Text;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
using UnityEngine;

namespace Login
{
    public class LoginManager : MonoBehaviour
    {
        public static bool IsLoggedIn { private set; get; }

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
            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        #region DarkRift Calls

        public static void Login(string username, string password)
        {
            var writer = new DarkRiftWriter();
            writer.Write(username);
            writer.Write(Rsa.Encrypt(Encoding.UTF8.GetBytes(password), Rsa.Key));

            var message = new TagSubjectMessage(Tags.Login, LoginSubjects.LoginUser, writer);
            GameControl.Client.SendMessage(message, SendMode.Reliable);
        }

        public static void AddUser(string username, string password)
        {
            var writer = new DarkRiftWriter();
            writer.Write(username);
            writer.Write(Rsa.Encrypt(Encoding.UTF8.GetBytes(password), Rsa.Key));

            var message = new TagSubjectMessage(Tags.Login, LoginSubjects.AddUser, writer);
            GameControl.Client.SendMessage(message, SendMode.Reliable);
        }

        public static void Logout()
        {
            IsLoggedIn = false;
            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Login, LoginSubjects.LogoutUser, new DarkRiftWriter()), SendMode.Reliable);
        }
        #endregion

        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            var message = e.Message as TagSubjectMessage;

            if (message != null && message.Tag == Tags.Login)
            {
                if (message.Subject == LoginSubjects.LoginSuccess)
                {
                    IsLoggedIn = true;
                    onSuccessfulLogin?.Invoke();
                }
                if (message.Subject == LoginSubjects.LoginFailed)
                {
                    var reader = message.GetReader();

                    if (reader.Length != 1)
                    {
                        Debug.LogWarning("Invalid LoginFailed Error data received.");
                        return;
                    }

                    var errorData = reader.ReadByte();
                    onFailedLogin?.Invoke(errorData);
                }
                if (message.Subject == LoginSubjects.AddUserSuccess)
                {
                    onSuccessfulAddUser?.Invoke();
                }
                if (message.Subject == LoginSubjects.AddUserFailed)
                {
                    var reader = message.GetReader();

                    if (reader.Length != 1)
                    {
                        Debug.LogWarning("Invalid LoginFailed Error data received.");
                        return;
                    }

                    var errorData = reader.ReadByte();
                    onFailedAddUser?.Invoke(errorData);
                }
            }
        }
    }
}