using UnityEngine;
using UnityEngine.UI;

namespace Rooms
{
    public class RoomNameInput : MonoBehaviour
    {
        public string CustomRoomName { get; private set; }
        private InputField _inputField;

        private void Start()
        {
            _inputField = GetComponent<InputField>();

            if (PlayerPrefs.HasKey("RoomName"))
            {
                CustomRoomName = PlayerPrefs.GetString("RoomName");
                _inputField.text = CustomRoomName;
            }
        }

        public void SetCustomRoomName(string value)
        {
            CustomRoomName = value;
            PlayerPrefs.SetString("RoomName", value);
        }
    }
}
