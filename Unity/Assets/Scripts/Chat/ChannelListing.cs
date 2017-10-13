using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChannelListing : MonoBehaviour
    {
        [SerializeField] private Text _channelNameText;

        public string Name { get; private set; }
        public MessageType MessageType { get; private set; }
        private ChannelLayoutGroup _channelLayoutGroup;

        public void Initialize(MessageType messageType, string channelName, ChannelLayoutGroup layoutGroup)
        {
            Name = channelName;
            MessageType = messageType;
            _channelLayoutGroup = layoutGroup;

            _channelNameText.text = channelName;
            _channelNameText.color = ChatManager.ChatColors[messageType];
        }

        public void ChannelSelected()
        {
            ChatManager.ActivateChatInput(MessageType, Name);
            _channelLayoutGroup.SetFilter(MessageType, Name);
        }

        public void Remove()
        {
            if (MessageType == MessageType.Private)
            {
                _channelLayoutGroup.RemoveChannel(MessageType, Name);
            }
            else if (MessageType == MessageType.ChatGroup)
            {
                ChatManager.LeaveChatGroup(Name);
            }
        }
    }
}
