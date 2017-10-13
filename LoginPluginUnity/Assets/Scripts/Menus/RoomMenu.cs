using System.Collections.Generic;
using Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Menus
{
    public class RoomMenu : MonoBehaviour
    {
        [SerializeField] private RoomLayoutGroup _roomLayoutGroup;
        [SerializeField] private PlayerLayoutGroup _playerLayoutGroup;
        [SerializeField] private RoomNameInput _roomNameInput;

        public GameObject OutOfLobby;
        public GameObject CreatingLobby;
        public GameObject InLobby;
        public Button StartButton;

        private MainMenuManager _mainMenuManager;
        private MainMenu _mainMenu;

        private void Awake()
        {
            _mainMenuManager = transform.parent.GetComponent<MainMenuManager>();
            _mainMenu = _mainMenuManager.MainMenu;

            RoomManager.onSuccessfulLeaveRoom += OnLeaveRoom;
            RoomManager.onSuccessfulJoinRoom += OnJoinedRoom;
        }

        private void OnDestroy()
        {
            RoomManager.onSuccessfulLeaveRoom -= OnLeaveRoom;
            RoomManager.onSuccessfulJoinRoom -= OnJoinedRoom;
        }

        private void OnEnable()
        {
            if (RoomManager.CurrentRoom == null)
            {
                OutOfLobby.SetActive(true);
                CreatingLobby.SetActive(false);
                InLobby.SetActive(false);
                Refresh();
            }
        }

        #region Buttons

        private void Update()
        {
            StartButton.interactable = RoomManager.IsHost;
        }

        public void Refresh()
        {
            _roomLayoutGroup.DeleteOldRooms();
            RoomManager.GetOpenRooms();
        }

        public void CreateRoom()
        {
            OutOfLobby.SetActive(false);
            CreatingLobby.SetActive(true);
        }

        public void CreateRoomRLR()
        {
            RoomManager.CreateRoom(_roomNameInput.CustomRoomName, GameType.RunlingRun, true, PlayerColor.Green);
        }

        public void CreateRoomSLA()
        {
            RoomManager.CreateRoom(_roomNameInput.CustomRoomName, GameType.Arena, true, PlayerColor.Green);
        }

        public void LeaveRoom()
        {
            RoomManager.LeaveRoom();
        }

        public void StartGame()
        {
            //            PhotonNetwork.room.IsOpen = false;
            //            PhotonNetwork.room.IsVisible = false;
            //
            //            switch ((string) PhotonNetwork.room.CustomProperties["GM"])
            //            {
            //                case "RR":
            //                    Debug.Log("Start RLR Game");
            //                    PhotonNetwork.LoadLevel(3);
            //                    break;
            //                case "AR":
            //                    Debug.Log("Start Arena Game");
            //                    PhotonNetwork.LoadLevel(5);
            //                    break;
            //                default:
            //                    Debug.Log("Couldn't load game, invalid selection");
            //                    PhotonNetwork.LoadLevel(2);
            //                    break;
            //            }
        }

        public void Back()
        {
            CreatingLobby.SetActive(false);
            OutOfLobby.SetActive(true);
        }

        public void BackToMenu()
        {
            if (RoomManager.CurrentRoom == null)
            {
                InLobby.SetActive(false);
                CreatingLobby.SetActive(false);
            }
            gameObject.SetActive(false);
            _mainMenu.gameObject.SetActive(true);
        }

        #endregion

        #region Network Callbacks

        public void OnJoinedRoom(List<Player> playerList)
        {
            CreatingLobby.SetActive(false);
            OutOfLobby.SetActive(false);
            InLobby.SetActive(true);
            _playerLayoutGroup.JoinedRoom(playerList);
        }

        public void OnLeaveRoom()
        {
            Refresh();
            OutOfLobby.SetActive(true);
            InLobby.SetActive(false);
            _playerLayoutGroup.RemovePlayers();
        }
        #endregion
    }
}