using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChatListing : MonoBehaviour
    {
        private Text _messageText;

        /// <summary>
        /// For Private, Room and Group Messages
        /// </summary>
        public void Initialize(MessageType messageType, string channel, string sender, string content)
        {
            _messageText = GetComponent<Text>();
            _messageText.text = "[" + channel + "]  " + sender + ": " + content;
            _messageText.color = ChatManager.ChatColors[messageType];
        }

        /// <summary>
        /// For Server Messages
        /// </summary>
        public void Initialize(MessageType messageType, string content)
        {
            _messageText = GetComponent<Text>();
            _messageText.text = content;
            _messageText.color = ChatManager.ChatColors[messageType];
        }
    }
}
