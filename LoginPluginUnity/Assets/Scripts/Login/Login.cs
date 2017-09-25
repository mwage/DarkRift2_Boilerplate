using Launcher;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Login
{
    public class Login : MonoBehaviour
    {
        public InputField UsernameInput;
        public InputField PasswordInput;

        public GameObject ControlPanel;
        public Text ConnectionText;
        public Text FeedbackText;
        public LoadingAnimation LoadingAnimation;
        public Button LoginButton;
        public Button AddUserButton;
        public GameObject CancelButton;

        private void Awake()
        {
            CancelButton.SetActive(false);

            if (PlayerPrefs.HasKey("username"))
            {
                UsernameInput.text = PlayerPrefs.GetString("username");
            }

            LoginManager.onSuccessfulLogin += LoadMainMenu;
            LoginManager.onFailedLogin += FailedLogin;
            LoginManager.onSuccessfulAddUser += SuccessfulRegister;
            LoginManager.onFailedAddUser += FailedAddUser;
        }

        // If you don't unsubscribe, Unity won't clean up the class instance and you end up with Eventcalls pointing nowhere
        private void OnDestroy()
        {
            LoginManager.onSuccessfulLogin -= LoadMainMenu;
            LoginManager.onFailedLogin -= FailedLogin;
            LoginManager.onSuccessfulAddUser -= SuccessfulRegister;
            LoginManager.onFailedAddUser -= FailedAddUser;
        }

        #region Buttons

        private void Update()
        {
            var condition = GameControl.Client.Connected
                            && UsernameInput.text.Length >= 2 && PasswordInput.text.Length >= 2
                            && Rsa.Key.Exponent != null && Rsa.Key.Modulus != null;

            LoginButton.interactable = condition;
            AddUserButton.interactable = LoginButton.IsInteractable();

            // Maybe add an option to try to reconnect
            if (!GameControl.Client.Connected)
                Debug.Log("not connected");
        }

        public void OfflineMode()
        {
            ConnectingScreen("Starting...");
            LoadMainMenu();
        }

        public void Cancel()
        {
            LoginScreen("Enter Username and Password!", Color.white);
        }

        public void LoginButtonFunction()
        {
            PlayerPrefs.SetString("username", UsernameInput.text);
            ConnectingScreen("Connecting...");
            LoginManager.Login(UsernameInput.text, PasswordInput.text);
        }

        public void AddUser()
        {
            ConnectingScreen("Creating User...");
            LoginManager.AddUser(UsernameInput.text, PasswordInput.text);
        }

        #endregion


        #region ProcessServerResponse

        private void FailedLogin(byte errorId)
        {
            if (errorId == 1)
            {
                PasswordInput.text = "";
                LoginScreen("Username/Password Combination unknown", Color.red);
            }
            else
            {
                LoginScreen("Server couldn't process Information", Color.red);
            }
        }

        private void FailedAddUser(byte errorId)
        {
            if (errorId == 1)
            {
                LoginScreen("Username already taken.", Color.red);
            }
            else if (errorId == 3)
            {
                LoginScreen("Already logged in!", Color.red);
            }
            else
            {
                // could differentiate between 0 (Wrong Information) and 2 (Database Error).
                LoginScreen("Server couldn't process Information", Color.red);
            }
        }

        private void SuccessfulRegister()
        {
            LoginButtonFunction();
        }

        #endregion

        private void ConnectingScreen(string connectionText)
        {
            ControlPanel.SetActive(false);
            CancelButton.SetActive(true);
            ConnectionText.text = connectionText;

            if (!LoadingAnimation.Particles.activeSelf)
            {
                LoadingAnimation.StartLoaderAnimation();
            }
        }

        private void LoginScreen(string text1, Color textColor)
        {
            ConnectionText.text = "";
            FeedbackText.text = text1;
            FeedbackText.color = textColor;
            LoadingAnimation.StopLoaderAnimation();
            CancelButton.SetActive(false);
            ControlPanel.SetActive(true);
        }

        private static void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}