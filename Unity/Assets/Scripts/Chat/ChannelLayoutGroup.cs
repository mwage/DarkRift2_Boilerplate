using System.Collections.Generic;
using Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChannelLayoutGroup : MonoBehaviour
    {
        [SerializeField] private ChatLayoutGroup _chatLayoutGroup;
        [SerializeField] private GameObject _channelPrefab;
        [SerializeField] private GameObject _roomChannelPrefab;

        public readonly List<ChannelListing> ActiveChannels = new List<ChannelListing>();
        public Filter ActiveFilter { get; private set; }
        private ToggleGroup _toggleGroup;
        private ChatWindowManager _chatWindowManager;

        public void Initialize(ChatWindowManager window)
        {
            _toggleGroup = GetComponent<ToggleGroup>();
            _chatWindowManager = window;

            RoomManager.onSuccessfulJoinRoom += AddRoomChannel;
            RoomManager.onSuccessfulLeaveRoom += RemoveRoomChannel;
            ChatManager.onSuccessfulJoinGroup += AddGroupChannel;
            ChatManager.onSuccessfulLeaveGroup += RemoveGroupChannel;
        }

        public void Destroy()
        {
            RoomManager.onSuccessfulJoinRoom -= AddRoomChannel;
            RoomManager.onSuccessfulLeaveRoom -= RemoveRoomChannel;
            ChatManager.onSuccessfulJoinGroup -= AddGroupChannel;
            ChatManager.onSuccessfulLeaveGroup -= RemoveGroupChannel;
        }

        public void AddPrivateChannel(string channelName)
        {
            if (ActiveChannels.Exists(c => c.Name == channelName && c.MessageType == MessageType.Private))
                return;

            AddChannel(MessageType.Private, channelName);
            _chatLayoutGroup.PrivateMessages[channelName] = new List<ChatListing>();
        }

        public void AddGroupChannel(string channelName)
        {
            if (ActiveChannels.Exists(c => c.Name == channelName && c.MessageType == MessageType.ChatGroup))
                return;

            AddChannel(MessageType.ChatGroup, channelName);
            _chatLayoutGroup.GroupMessages[channelName] = new List<ChatListing>();
            _chatWindowManager.ActivateInputField(MessageType.ChatGroup, channelName);
        }

        private void RemoveGroupChannel(string channelName)
        {
            RemoveChannel(MessageType.ChatGroup, channelName);
        }

        private void AddRoomChannel(List<Player> players)
        {
            var listing = AddChannel(MessageType.Room, "Room");
            _chatWindowManager.SelectChannel(listing);
        }

        private void RemoveRoomChannel()
        {
            RemoveChannel(MessageType.Room, "Room");
        }

        private ChannelListing AddChannel(MessageType messageType, string channelName)
        {
            var channelListing = messageType == MessageType.Room ?
                Instantiate(_roomChannelPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<ChannelListing>() :
                Instantiate(_channelPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<ChannelListing>();

            channelListing.Initialize(messageType, channelName, this);
            channelListing.GetComponent<Toggle>().group = _toggleGroup;
            ActiveChannels.Add(channelListing);
            return channelListing;
        }

        public void RemoveChannel(MessageType messageType, string channelName)
        {
            int index;
            if (messageType == MessageType.Private)
            {
                index = ActiveChannels.FindIndex(c => c.Name == channelName);
            }
            else if (messageType == MessageType.ChatGroup)
            {
                index = ActiveChannels.FindIndex(c => c.Name == channelName);
            }
            else if (messageType == MessageType.Room)
            {
                index = ActiveChannels.FindIndex(c => c.MessageType == MessageType.Room);
            }
            else
            {
                Debug.Log("Failed to remove channel. Invalid MessageType.");
                return;
            }

            if (index != -1)
            {
                Destroy(ActiveChannels[index].gameObject);
                ActiveChannels.RemoveAt(index);
                FilterAll();
                _chatWindowManager.NextOutput();
            }
        }

        public void SetFilter(MessageType messageType, string channelName)
        {
            ActiveFilter = new Filter(messageType, channelName);
            _chatLayoutGroup.ApplyFilter();
        }

        public void FilterAll()
        {
            ActiveFilter = new Filter(MessageType.All, "");
            _chatLayoutGroup.ApplyFilter();
        }
    }
}
