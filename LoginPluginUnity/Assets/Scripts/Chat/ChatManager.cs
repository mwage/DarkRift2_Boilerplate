using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
using Login;
using Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chat
{
    public class ChatManager : MonoBehaviour
    {
        public static List<ChatMessage> Messages = new List<ChatMessage>();
        public static List<string> SavedChatGroups;

        public static Dictionary<MessageType, Color> ChatColors = new Dictionary<MessageType, Color>();

        #region Events

        public delegate void ActivateChatEventHandler(MessageType messageType, string channel);
        public delegate void PrivateMessageEventHandler(ChatMessage message);
        public delegate void RoomMessageEventHandler(ChatMessage message);
        public delegate void GroupMessageEventHandler(ChatMessage message);
        public delegate void ServerMessageEventHandler(ChatMessage message);
        public delegate void SuccessfulJoinGroupEventHandler(string groupName);
        public delegate void SuccessfulLeaveGroupEventHandler(string groupName);

        public static event ActivateChatEventHandler onActivateChat;
        public static event PrivateMessageEventHandler onPrivateMessage;
        public static event RoomMessageEventHandler onRoomMessage;
        public static event GroupMessageEventHandler onGroupMessage;
        public static event ServerMessageEventHandler onServerMessage;
        public static event SuccessfulJoinGroupEventHandler onSuccessfulJoinGroup;
        public static event SuccessfulLeaveGroupEventHandler onSuccessfulLeaveGroup;


        #endregion

        private void Start()
        {
            // Set ChatColors
            ChatColors[MessageType.ChatGroup] = Color.green;
            ChatColors[MessageType.Error] = Color.red;
            ChatColors[MessageType.Info] = Color.cyan;
            ChatColors[MessageType.Room] = new Color(1, 0.5f, 0);
            ChatColors[MessageType.Private] = Color.magenta;
            ChatColors[MessageType.All] = Color.white;

            // Get all saved Chatgroups
            if (PlayerPrefs.GetInt("SetChatGroups") == 0)
            {
                SavedChatGroups = new List<string> { "General" };
                ArrayPrefs.SetStringArray("ChatGroups", SavedChatGroups.ToArray());
                PlayerPrefs.SetInt("SetChatGroups", 1);
            }
            else
            {
                SavedChatGroups = ArrayPrefs.GetStringArray("ChatGroups").ToList();
            }

            GameControl.Client.MessageReceived += OnDataHandler;
        }

        private void OnDestroy()
        {
            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        #region Network Calls

        public static void SendPrivateMessage(string receiver, string message)
        {
            var writer = new DarkRiftWriter();
            writer.Write(receiver);
            writer.Write(message);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.PrivateMessage, writer),
                SendMode.Reliable);
        }

        public static void SendRoomMessage(string message)
        {
            var writer = new DarkRiftWriter();
            writer.Write(RoomManager.CurrentRoom.Id);
            writer.Write(message);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.RoomMessage, writer),
                SendMode.Reliable);
        }

        public static void SendGroupMessage(string groupName, string message)
        {
            var writer = new DarkRiftWriter();
            writer.Write(groupName);
            writer.Write(message);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.GroupMessage, writer),
                SendMode.Reliable);
        }

        public static void ServerMessage(string content, MessageType messageType)
        {
            var message = new ChatMessage("", content, messageType, "", isServerMessage: true);
            Messages.Add(message);

            onServerMessage?.Invoke(message);
        }

        public static void JoinChatGroup(string groupName)
        {
            var writer = new DarkRiftWriter();
            writer.Write(groupName);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.JoinGroup, writer),
                SendMode.Reliable);
        }

        public static void LeaveChatGroup(string groupName)
        {
            var writer = new DarkRiftWriter();
            writer.Write(groupName);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.LeaveGroup, writer),
                SendMode.Reliable);
        }

        private static void GetActiveGroups()
        {
            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Chat, ChatSubjects.GetActiveGroups, new DarkRiftWriter()), SendMode.Reliable);
        }

        #endregion

        // UI Helper
        public static void ActivateChatInput(MessageType messageType, string channel)
        {
            onActivateChat?.Invoke(messageType, channel);
        }

        // Commands
        public static void Command(string message)
        {
            var split = message.Split(' ');
            var command = split[0];
            var parameter = message.Length == command.Length ? " " : message.Substring(command.Length + 1);

            switch (command)
            {
                case "/join":
                    if (string.IsNullOrWhiteSpace(parameter))
                    {
                        ServerMessage("You have to enter a channel name.", MessageType.Error);
                    }
                    else if (parameter.Length > 10)
                    {
                        ServerMessage("Channel can't have more than 10 characters!", MessageType.Error);
                    }

                    else
                    {
                        JoinChatGroup(parameter);
                    }
                    break;
                case "/leave":

                    if (string.IsNullOrWhiteSpace(parameter))
                    {
                        ServerMessage("You have to enter a channel name.", MessageType.Error);
                    }
                    else if (parameter.Length > 10)
                    {
                        ServerMessage("Channel can't have more than 10 characters!", MessageType.Error);
                    }
                    else
                    {
                        LeaveChatGroup(parameter);
                    }
                    break;
                case "/list":
                    GetActiveGroups();
                    break;
                case "/quit":
                    Application.Quit();
                    break;
                case "/logout":
                    LoginManager.Logout();
                    break;
                case "/wage":
                    ServerMessage("I Made Dis!", MessageType.Info);
                    break;
                default:
                    ServerMessage("Unknown command.", MessageType.Error);
                    break;
            }
        }

        // Server Responses
        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            var message = e.Message as TagSubjectMessage;

            if (message == null || message.Tag != Tags.Chat)
                return;

            // Private message received
            if (message.Subject == ChatSubjects.PrivateMessage)
            {
                var reader = message.GetReader();
                var senderName = reader.ReadString();
                var content = reader.ReadString();
                var chatMessage = new ChatMessage(senderName, content, MessageType.Private, senderName);
                Messages.Add(chatMessage);

                onPrivateMessage?.Invoke(chatMessage);
            }
            // Private message sent
            else if (message.Subject == ChatSubjects.SuccessfulPrivateMessage)
            {
                var reader = message.GetReader();
                var senderName = reader.ReadString();
                var receiver = reader.ReadString();
                var content = reader.ReadString();
                var chatMessage = new ChatMessage(senderName, content, MessageType.Private, receiver, true);
                Messages.Add(chatMessage);

                onPrivateMessage?.Invoke(chatMessage);
            }
            // Room message received
            else if (message.Subject == ChatSubjects.RoomMessage)
            {
                var reader = message.GetReader();
                var senderName = reader.ReadString();
                var content = reader.ReadString();
                var chatMessage = new ChatMessage(senderName, content, MessageType.Room, "Room");
                Messages.Add(chatMessage);
                onRoomMessage?.Invoke(chatMessage);
            }
            // Group message received
            else if (message.Subject == ChatSubjects.GroupMessage)
            {
                var reader = message.GetReader();
                var groupName = reader.ReadString();
                var senderName = reader.ReadString();
                var content = reader.ReadString();
                var chatMessage = new ChatMessage(senderName, content, MessageType.ChatGroup, groupName);
                Messages.Add(chatMessage);

                onGroupMessage?.Invoke(chatMessage);
            }
            // Message failed
            else if (message.Subject == ChatSubjects.MessageFailed)
            {
                var content = "Failed to send message.";
                var reader = message.GetReader();
                if (reader.Length != 1)
                {
                    Debug.LogWarning("Invalid Message Failed Error data received.");
                }
                else
                {
                    switch (reader.ReadByte())
                    {
                        case 0:
                            Debug.Log("Invalid Message data sent!");
                            break;
                        case 1:
                            Debug.Log("You're not logged in!");
                            SceneManager.LoadScene("Login");
                            break;
                        case 2:
                            Debug.Log("You're not part of this chatgroup.");
                            content = "Not connected to this chat channel. Try leaving and rejoining!";
                            break;
                        case 3:
                            Debug.Log("Failed to send message. Player is offline.");
                            content = "Player is offline.";
                            break;
                        default:
                            Debug.Log("Invalid errorId!");
                            break;
                    }
                }
                ServerMessage(content, MessageType.Error);
            }
            // Successfully joined group
            else if (message.Subject == ChatSubjects.JoinGroup)
            {
                var reader = message.GetReader();
                var group = reader.ReadSerializable<ChatGroup>();
                ServerMessage("You joined the channel: " + group.Name, MessageType.ChatGroup);

                if (!SavedChatGroups.Contains(group.Name))
                {
                    SavedChatGroups.Add(group.Name);
                    ArrayPrefs.SetStringArray("ChatGroups", SavedChatGroups.ToArray());
                }

                onSuccessfulJoinGroup?.Invoke(group.Name);
            }
            // Failed to join group
            else if (message.Subject == ChatSubjects.JoinGroupFailed)
            {
                var content = "Failed to join chat group.";
                var reader = message.GetReader();
                if (reader.Length != 1)
                {
                    Debug.LogWarning("Invalid Join Group Failed Error data received.");
                }
                else
                {
                    switch (reader.ReadByte())
                    {
                        case 0:
                            Debug.Log("Invalid Join Group data sent!");
                            break;
                        case 1:
                            Debug.Log("You're not logged in!");
                            SceneManager.LoadScene("Login");
                            break;
                        case 2:
                            Debug.Log("Alreay in this chatgroup.");
                            content = "You are already in this chat group.";
                            break;
                        default:
                            Debug.Log("Invalid errorId!");
                            break;
                    }
                }
                ServerMessage(content, MessageType.Error);
            }
            // Successfully left group
            else if (message.Subject == ChatSubjects.LeaveGroup)
            {
                var reader = message.GetReader();
                var groupName = reader.ReadString();
                ServerMessage("You left the channel: " + groupName, MessageType.ChatGroup);

                if (SavedChatGroups.Remove(groupName))
                {
                    ArrayPrefs.SetStringArray("ChatGroups", SavedChatGroups.ToArray());
                }

                onSuccessfulLeaveGroup?.Invoke(groupName);
            }
            // Failed to leave group
            else if (message.Subject == ChatSubjects.LeaveGroupFailed)
            {
                var content = "Failed to leave chat group.";
                var reader = message.GetReader();
                if (reader.Length != 1)
                {
                    Debug.LogWarning("Invalid Leave Group Failed Error data received.");
                }
                else
                {
                    switch (reader.ReadByte())
                    {
                        case 0:
                            Debug.Log("Invalid Leave Group data sent!");
                            break;
                        case 1:
                            Debug.Log("You're not logged in!");
                            SceneManager.LoadScene("Login");
                            break;
                        case 2:
                            Debug.Log("No such chatgroup chatgroup.");
                            content = "There is no chat group with this name.";
                            break;
                        default:
                            Debug.Log("Invalid errorId!");
                            break;
                    }
                }
                ServerMessage(content, MessageType.Error);
            }
            // Received all active chat groups
            else if (message.Subject == ChatSubjects.GetActiveGroups)
            {
                var reader = message.GetReader();
                var groupList = reader.ReadStrings().ToList();
                groupList.Sort(string.CompareOrdinal);
                foreach (var group in groupList)
                {
                    ServerMessage(group, MessageType.All);
                }
            }
            // Failed to get grouplist
            else if (message.Subject == ChatSubjects.GetActiveGroupsFailed)
            {
                var content = "Failed to get list of chat groups.";
                var reader = message.GetReader();
                if (reader.Length != 1)
                {
                    Debug.LogWarning("Invalid Get Active Groups Failed Error data received.");
                }
                else
                {
                    switch (reader.ReadByte())
                    {
                        case 1:
                            Debug.Log("You're not logged in!");
                            SceneManager.LoadScene("Login");
                            break;
                        default:
                            Debug.Log("Invalid errorId!");
                            break;
                    }
                }
                ServerMessage(content, MessageType.Error);
            }
        }
    }

    public enum MessageType
    {
        Private,
        Room,
        ChatGroup,
        Error,
        Info,
        All
    }
}
