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
        public static List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public static List<string> SavedChatGroups { get; private set; }

        public static Dictionary<MessageType, Color> ChatColors { get; } = new Dictionary<MessageType, Color>();

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
//            ChatColors[MessageType.ChatGroup] = new Color(0, 0.7f, 0);
            ChatColors[MessageType.ChatGroup] = Color.green;
            ChatColors[MessageType.Error] = Color.red;
            ChatColors[MessageType.Info] = Color.blue;
            ChatColors[MessageType.Room] = new Color(1, 0.4f, 0);
            ChatColors[MessageType.Private] = Color.magenta;
            ChatColors[MessageType.All] = Color.black;

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
            if (GameControl.Client == null)
                return;
            
            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        #region Network Calls

        public static void SendPrivateMessage(string receiver, string message)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(receiver);
                writer.Write(message);

                using (var msg = Message.Create(ChatTags.PrivateMessage, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void SendRoomMessage(string message)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(RoomManager.CurrentRoom.Id);
                writer.Write(message);

                using (var msg = Message.Create(ChatTags.RoomMessage, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void SendGroupMessage(string groupName, string message)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(groupName);
                writer.Write(message);

                using (var msg = Message.Create(ChatTags.GroupMessage, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void ServerMessage(string content, MessageType messageType)
        {
            var message = new ChatMessage("", content, messageType, "", isServerMessage: true);
            Messages.Add(message);

            onServerMessage?.Invoke(message);
        }

        public static void JoinChatGroup(string groupName)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(groupName);

                using (var msg = Message.Create(ChatTags.JoinGroup, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void LeaveChatGroup(string groupName)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(groupName);

                using (var msg = Message.Create(ChatTags.LeaveGroup, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        private static void GetActiveGroups()
        {
            using (var msg = Message.CreateEmpty(ChatTags.GetActiveGroups))
            {
                GameControl.Client.SendMessage(msg, SendMode.Reliable);
            }
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
                {
                    if (string.IsNullOrWhiteSpace(parameter))
                    {
                        ServerMessage("You have to enter a channel name.", MessageType.Error);
                    }
                    else if (parameter.Length > 10)
                    {
                        ServerMessage("Channel name can't have more than 10 characters!", MessageType.Error);
                    }
                    else
                    {
                        JoinChatGroup(parameter);
                    }
                    break;
                }
                case "/leave":
                {
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
                }
                case "/list":
                    GetActiveGroups();
                    break;
                case "/quit":
                    Application.Quit();
                    break;
                case "/logout":
                    LoginManager.Logout();
                    break;
                default:
                    ServerMessage("Unknown command.", MessageType.Error);
                    break;
            }
        }

        // Server Responses
        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Tags.TagsPerPlugin * Tags.Chat || message.Tag >= Tags.TagsPerPlugin * (Tags.Chat + 1))
                    return;

                // Private message received
                switch (message.Tag)
                {
                    // Private message received
                    case ChatTags.PrivateMessage:
                    {
                        using (var reader = message.GetReader())
                        {
                            var senderName = reader.ReadString();
                            var content = reader.ReadString();
                            var chatMessage = new ChatMessage(senderName, content, MessageType.Private, senderName);

                            Messages.Add(chatMessage);

                            onPrivateMessage?.Invoke(chatMessage);
                        }

                        break;
                    }

                    // Private message sent
                    case ChatTags.SuccessfulPrivateMessage:
                    {
                        using (var reader = message.GetReader())
                        {
                            var senderName = reader.ReadString();
                            var receiver = reader.ReadString();
                            var content = reader.ReadString();
                            var chatMessage = new ChatMessage(senderName, content, MessageType.Private, receiver, true);
                            Messages.Add(chatMessage);

                            onPrivateMessage?.Invoke(chatMessage);
                        }
                        break;
                    }

                    // Room message received
                    case ChatTags.RoomMessage:
                    {
                        using (var reader = message.GetReader())
                        {
                            var senderName = reader.ReadString();
                            var content = reader.ReadString();
                            var chatMessage = new ChatMessage(senderName, content, MessageType.Room, "Room");
                            Messages.Add(chatMessage);

                            onRoomMessage?.Invoke(chatMessage);
                        }
                        break;
                    }

                    // Group message received
                    case ChatTags.GroupMessage:
                    {
                        using (var reader = message.GetReader())
                        {
                            var groupName = reader.ReadString();
                            var senderName = reader.ReadString();
                            var content = reader.ReadString();
                            var chatMessage = new ChatMessage(senderName, content, MessageType.ChatGroup, groupName);
                            Messages.Add(chatMessage);

                            onGroupMessage?.Invoke(chatMessage);
                        }
                        break;
                    }

                    case ChatTags.MessageFailed:
                    {
                        var content = "Failed to send message.";
                        using (var reader = message.GetReader())
                        {
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
                        }
                           
                        ServerMessage(content, MessageType.Error);
                        break;
                    }

                    case ChatTags.JoinGroup:
                    {
                        using (var reader = message.GetReader())
                        {
                            var group = reader.ReadSerializable<ChatGroup>();
                            ServerMessage("You joined the channel: " + group.Name, MessageType.ChatGroup);

                            if (!SavedChatGroups.Contains(group.Name))
                            {
                                SavedChatGroups.Add(group.Name);
                                ArrayPrefs.SetStringArray("ChatGroups", SavedChatGroups.ToArray());
                            }

                            onSuccessfulJoinGroup?.Invoke(group.Name);
                        }
                        break;
                    }

                    case ChatTags.JoinGroupFailed:
                    {
                        var content = "Failed to join chat group.";
                        using (var reader = message.GetReader())
                        {
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
                 
                        break;
                    }

                    case ChatTags.LeaveGroup:
                    {
                        using (var reader = message.GetReader())
                        {
                            var groupName = reader.ReadString();
                            ServerMessage("You left the channel: " + groupName, MessageType.ChatGroup);

                            if (SavedChatGroups.Remove(groupName))
                            {
                                ArrayPrefs.SetStringArray("ChatGroups", SavedChatGroups.ToArray());
                            }

                            onSuccessfulLeaveGroup?.Invoke(groupName);
                        }
                        break;
                    }

                    case ChatTags.LeaveGroupFailed:
                    {
                        var content = "Failed to leave chat group.";
                        using (var reader = message.GetReader())
                        {
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
                        }
                        ServerMessage(content, MessageType.Error);
                        break;
                    }

                    case ChatTags.GetActiveGroups:
                    {
                        using (var reader = message.GetReader())
                        {
                            var groupList = reader.ReadStrings().ToList();
                            groupList.Sort(string.CompareOrdinal);
                            foreach (var group in groupList)
                            {
                                ServerMessage(group, MessageType.All);
                            }
                        }
                        break;
                    }

                    case ChatTags.GetActiveGroupsFailed:
                    {
                        var content = "Failed to get list of chat groups.";
                        using (var reader = message.GetReader())
                        {
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
                        }
                        ServerMessage(content, MessageType.Error);
                        break;
                    }
                }
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
