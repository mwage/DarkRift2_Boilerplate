using Chat;
using UnityEngine;
using UnityEngine.UI;

namespace Friends
{
    public class FriendListing : MonoBehaviour
    {
        [SerializeField] private Text _friendNameText;
        public string FriendName { get; private set; }

        private Button _button;

        public void Initialize(string friend)
        {
            _button = GetComponent<Button>();
            FriendName = friend;
            _friendNameText.text = friend;
            _button.onClick.AddListener(PrivateMessage);
        }

        public void RemoveFriend()
        {
            FriendManager.RemoveFriend(FriendName);
        }

        public void AcceptRequest()
        {
            FriendManager.AcceptFriendRequest(FriendName);
        }

        public void DeclineRequest()
        {
            FriendManager.DeclineFriendRequest(FriendName);
        }

        private void PrivateMessage()
        {
            ChatManager.ActivateChatInput(MessageType.Private, FriendName);
        }
    }
}