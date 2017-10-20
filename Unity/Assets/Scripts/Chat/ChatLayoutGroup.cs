using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChatLayoutGroup : MonoBehaviour
    {
        [SerializeField] private GameObject _messagePrefab;
        [SerializeField] private ChannelLayoutGroup _channelLayoutGroup;
        [SerializeField] private ScrollRect _scrollRect;

        public readonly Dictionary<string, List<ChatListing>> PrivateMessages = new Dictionary<string, List<ChatListing>>();
        public readonly Dictionary<string, List<ChatListing>> GroupMessages = new Dictionary<string, List<ChatListing>>();
        private readonly List<ChatListing> _roomMessages = new List<ChatListing>();
        private readonly List<ChatListing> _serverMessages = new List<ChatListing>();

        public void Initialize()
        {

            _channelLayoutGroup.FilterAll();
            AddOldMessages();

            ChatManager.onPrivateMessage += NewPrivateMessage;
            ChatManager.onRoomMessage += NewRoomMessage;
            ChatManager.onGroupMessage += NewGroupMessage;
            ChatManager.onServerMessage += NewServerMessage;
        }

        public void Destroy()
        {
            ChatManager.onPrivateMessage -= NewPrivateMessage;
            ChatManager.onRoomMessage -= NewRoomMessage;
            ChatManager.onGroupMessage -= NewGroupMessage;
            ChatManager.onServerMessage -= NewServerMessage;
        }

        public void AddOldMessages()
        {
            foreach (var message in ChatManager.Messages)
            {
                if (message.IsServerMessage)
                {
                    NewServerMessage(message);
                    continue;
                }

                switch (message.MessageType)
                {
                    case MessageType.Private:
                        NewPrivateMessage(message);
                        break;
                    case MessageType.Room:
                        NewRoomMessage(message);
                        break;
                    case MessageType.ChatGroup:
                        NewGroupMessage(message);
                        break;
                    default:
                        Debug.Log("Can't handle this message type.");
                        break;
                }
            }
        }

        private void NewPrivateMessage(ChatMessage message)
        {
            var channel = message.ChannelName;
            _channelLayoutGroup.AddPrivateChannel(channel);

            var chatListing = Instantiate(_messagePrefab, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ChatListing>();
            chatListing.Initialize(message.MessageType, message.IsSender ? "To" : "From", message.Sender, message.Content);
            PrivateMessages[channel].Add(chatListing);

            // Check if filter hides new message
            var filter = _channelLayoutGroup.ActiveFilter;
            if (filter.MessageType == MessageType.All ||
                filter.MessageType == MessageType.Private && filter.ChannelName == channel)
            {
                ScrollToEnd();
            }
            else
            {
                chatListing.gameObject.SetActive(false);
            }
        }

        private void NewRoomMessage(ChatMessage message)
        {
            var chatListing = Instantiate(_messagePrefab, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ChatListing>();
            chatListing.Initialize(message.MessageType, "Room", message.Sender, message.Content);
            _roomMessages.Add(chatListing);

            // Check if filter hides new message
            var filter = _channelLayoutGroup.ActiveFilter;
            if (filter.MessageType == MessageType.All || filter.MessageType == MessageType.Room)
            {
                ScrollToEnd();
            }
            else
            {
                chatListing.gameObject.SetActive(false);
            }
        }

        private void NewGroupMessage(ChatMessage message)
        {
            var groupName = message.ChannelName;

            var chatListing = Instantiate(_messagePrefab, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ChatListing>();
            chatListing.Initialize(message.MessageType, message.ChannelName, message.Sender, message.Content);
            GroupMessages[groupName].Add(chatListing);

            // Check if filter hides new message
            var filter = _channelLayoutGroup.ActiveFilter;
            if (filter.MessageType == MessageType.All || filter.MessageType == MessageType.ChatGroup && filter.ChannelName == groupName)
            {
                ScrollToEnd();
            }
            else
            {
                chatListing.gameObject.SetActive(false);
            }
        }

        private void NewServerMessage(ChatMessage message)
        {
            var chatListing = Instantiate(_messagePrefab, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ChatListing>();
            chatListing.Initialize(message.MessageType, message.Content);

            if (message.MessageType == MessageType.Room)
            {
                _roomMessages.Add(chatListing);
            }
            else
            {
                _serverMessages.Add(chatListing);
            }

            // Check if filter hides new message
            var filter = _channelLayoutGroup.ActiveFilter;
            if (filter.MessageType == MessageType.All)
            {
                ScrollToEnd();
            }
            else
            {
                chatListing.gameObject.SetActive(false);
            }
        }

        public void ApplyFilter()
        {
            var filter = _channelLayoutGroup.ActiveFilter;
            switch (filter.MessageType)
            {
                case MessageType.All:
                    SetPrivate(true);
                    SetGroup(true);
                    SetMessages(_roomMessages, true);
                    SetMessages(_serverMessages, true);
                    break;
                case MessageType.Room:
                    SetPrivate(false);
                    SetGroup(false);
                    SetMessages(_roomMessages, true);
                    SetMessages(_serverMessages, false);
                    break;
                case MessageType.Private:
                    SetPrivate(true, filter.ChannelName);
                    SetGroup(false);
                    SetMessages(_roomMessages, false);
                    SetMessages(_serverMessages, false);
                    break;
                case MessageType.ChatGroup:
                    SetPrivate(false);
                    SetGroup(true, filter.ChannelName);
                    SetMessages(_roomMessages, false);
                    SetMessages(_serverMessages, false);
                    break;
                default:
                    Debug.Log("Can't filter MessageType: " + filter.MessageType);
                    break;
            }
            ScrollToEnd();
        }

        private void SetPrivate(bool active, string channelName = null)
        {
            foreach (var channel in PrivateMessages.Values)
            {
                if (channelName != null && PrivateMessages[channelName] != channel)
                {
                    SetMessages(channel, !active);
                }
                else
                {
                    SetMessages(channel, active);
                }
            }
        }

        private void SetGroup(bool active, string channelName = null)
        {
            foreach (var channel in GroupMessages.Values)
            {
                if (channelName != null && GroupMessages[channelName] != channel)
                {
                    SetMessages(channel, !active);
                }
                else
                {
                    SetMessages(channel, active);
                }
            }
        }

        private static void SetMessages(List<ChatListing> channel, bool active)
        {
            foreach (var message in channel)
            {
                message.gameObject.SetActive(active);
            }
        }

        private void ScrollToEnd()
        {
            if (_scrollRect.verticalNormalizedPosition < 0.0001f)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
