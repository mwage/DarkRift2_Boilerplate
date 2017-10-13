using Login;
using Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menus
{
    public class MainMenu : MonoBehaviour
    {
        public Text RoomButtonText;
        public Button RoomButton;
        public Text LogoutButtonText;

        private MenuManager _mainMenuManager;
        private RoomMenu _roomMenu;

        private void Awake()
        {
            _mainMenuManager = transform.parent.GetComponent<MenuManager>();
            _roomMenu = _mainMenuManager.RoomMenu;
        }

        private void Update()
        {
            RoomButtonText.text = RoomManager.CurrentRoom == null ? "Rooms" : "Back to Lobby";
            RoomButton.interactable = LoginManager.IsLoggedIn;
            LogoutButtonText.text = LoginManager.IsLoggedIn ? "Logout" : "Login";
        }

        #region Buttons

        public void Rooms()
        {
            if (LoginManager.IsLoggedIn)
            {
                gameObject.SetActive(false);
                _roomMenu.gameObject.SetActive(true);
            }
            else
            {
                SceneManager.LoadScene("Login");
            }
        }

        public void Logout()
        {
            LoginManager.Logout();
        }

        public void Quit()
        {
            Application.Quit();
        }
        #endregion
    }
}