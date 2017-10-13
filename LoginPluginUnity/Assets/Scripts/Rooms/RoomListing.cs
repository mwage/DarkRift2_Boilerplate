using UnityEngine;
using UnityEngine.UI;

namespace Rooms
{
    public class RoomListing : MonoBehaviour
    {
        public Text RoomNameText;
        public Text GameModeText;
        public Text PlayerCountText;

        public Room Room { get; private set; }

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Initialize(Room room)
        {
            Room = room;
            _button.onClick.AddListener(() => RoomManager.JoinRoom(Room.Id, PlayerColor.Green));
            RoomNameText.text = Room.Name;
            GameModeText.text = Room.GameType.ToString().GetFriendlyName();
            PlayerCountText.text = Room.CurrentPlayers + "/" + Room.MaxPlayers;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            _button.interactable = RoomManager.CurrentRoom == null;
        }
    }
}