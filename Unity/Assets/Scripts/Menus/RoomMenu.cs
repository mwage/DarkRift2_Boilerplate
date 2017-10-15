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

        public GameObject OutOfRoom;
        public GameObject CreatingRoom;
        public GameObject InRoom;
        public Button StartButton;

        private MenuManager _mainMenuManager;
        private MainMenu _mainMenu;

        private void Awake()
        {
            _mainMenuManager = transform.parent.GetComponent<MenuManager>();
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
                OutOfRoom.SetActive(true);
                CreatingRoom.SetActive(false);
                InRoom.SetActive(false);
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
            OutOfRoom.SetActive(false);
            CreatingRoom.SetActive(true);
        }

        public void Create()
        {
            RoomManager.CreateRoom(_roomNameInput.CustomRoomName, true);
        }

        public void LeaveRoom()
        {
            RoomManager.LeaveRoom();
        }

        public void BackToRoomlist()
        {
            CreatingRoom.SetActive(false);
            OutOfRoom.SetActive(true);
        }

        public void BackToMenu()
        {
            if (RoomManager.CurrentRoom == null)
            {
                InRoom.SetActive(false);
                CreatingRoom.SetActive(false);
            }
            gameObject.SetActive(false);
            _mainMenu.gameObject.SetActive(true);
        }

        public void StartGame()
        {
            RoomManager.StartGame();
        }

        #endregion

        #region Network Callbacks

        public void OnJoinedRoom(List<Player> playerList)
        {
            CreatingRoom.SetActive(false);
            OutOfRoom.SetActive(false);
            InRoom.SetActive(true);
            _playerLayoutGroup.JoinedRoom(playerList);
        }

        public void OnLeaveRoom()
        {
            Refresh();
            OutOfRoom.SetActive(true);
            InRoom.SetActive(false);
            _playerLayoutGroup.RemovePlayers();
        }
        #endregion
    }
}