using Chat;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
using Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menus
{
    public class MenuManager : MonoBehaviour
    {
        public MainMenu MainMenu;
        public RoomMenu RoomMenu;
        public ChatWindowManager ChatWindowManager;

        private void Awake()
        {
            RoomManager.CurrentRoom = null;
            GameControl.Client.MessageReceived += OnDataHandler;
        }

        private void OnDestroy()
        {
            if (GameControl.Client == null)
                return;

            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        private void Update()
        {
            #region NavigateMenu

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ChatWindowManager.ChatPanels.activeSelf || ChatWindowManager.FriendPanels.activeSelf)
                {
                    ChatWindowManager.DeactivatePanels();
                }
                else if (RoomMenu.gameObject.activeSelf)
                {
                    RoomMenu.BackToMenu();
                }
            }
            #endregion
        }

        // Handle Logout
        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                if (message.Tag == LoginTags.LogoutSuccess)
                {
                    RoomManager.CurrentRoom = null;
                    RoomManager.LeaveRoom();
                    SceneManager.LoadScene("Login");
                }
            }
        }
    }
}