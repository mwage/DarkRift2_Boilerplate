using Chat;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rooms
{
    public class RoomManager : MonoBehaviour
    {
        public static bool IsHost { get; private set; }
        public static Room CurrentRoom { get; set; }

        #region Events

        public delegate void SuccessfulLeaveRoomEventHandler();
        public delegate void SuccessfulJoinRoomEventHandler(List<Player> playerList);
        public delegate void ReceivedOpenRoomsEventHandler(List<Room> roomList);
        public delegate void PlayerJoinedEventHandler(Player player);
        public delegate void PlayerLeftEventHandler(uint leftId, uint newHostId);

        public static event SuccessfulLeaveRoomEventHandler onSuccessfulLeaveRoom;
        public static event SuccessfulJoinRoomEventHandler onSuccessfulJoinRoom;
        public static event ReceivedOpenRoomsEventHandler onReceivedOpenRooms;
        public static event PlayerJoinedEventHandler onPlayerJoined;
        public static event PlayerLeftEventHandler onPlayerLeft;

        #endregion

        private void Awake()
        {
            GameControl.Client.MessageReceived += OnDataHandler;
        }

        private void OnDestroy()
        {
            if (GameControl.Client == null)
                return;

            GameControl.Client.MessageReceived -= OnDataHandler;
        }

        #region Network Calls

        public static void CreateRoom(string roomname, bool isVisible)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(roomname);
                writer.Write(isVisible);

                using (var msg = Message.Create(RoomTags.Create, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void JoinRoom(ushort roomId)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(roomId);

                using (var msg = Message.Create(RoomTags.Join, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        public static void LeaveRoom()
        {
            using (var msg = Message.CreateEmpty(RoomTags.Leave))
            {
                GameControl.Client.SendMessage(msg, SendMode.Reliable);
            }
        }

        public static void GetOpenRooms()
        {
            using (var msg = Message.CreateEmpty(RoomTags.GetOpenRooms))
            {
                GameControl.Client.SendMessage(msg, SendMode.Reliable);
            }
        }

        public static void StartGame()
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(CurrentRoom.Id);

                using (var msg = Message.Create(RoomTags.StartGame, writer))
                {
                    GameControl.Client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        #endregion

        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Tags.TagsPerPlugin * Tags.Room || message.Tag >= Tags.TagsPerPlugin * (Tags.Room + 1))
                    return;

                switch (message.Tag)
                {
                    case RoomTags.CreateSuccess:
                    {
                        using (var reader = message.GetReader())
                        {
                            var room = reader.ReadSerializable<Room>();
                            var player = reader.ReadSerializable<Player>();

                            IsHost = player.IsHost;
                            CurrentRoom = room;
                            ChatManager.ServerMessage("Created room " + room.Name + "!", MessageType.Room);

                            onSuccessfulJoinRoom?.Invoke(new List<Player> {player});
                        }
                        break;
                    }

                    case RoomTags.CreateFailed:
                    {
                        ChatManager.ServerMessage("Failed to create room.", MessageType.Error);

                        using (var reader = message.GetReader())
                        {
                            if (reader.Length != 1)
                            {
                                Debug.LogWarning("Invalid CreateRoomFailed Error data received.");
                                return;
                            }

                            switch (reader.ReadByte())
                            {
                                case 0:
                                    Debug.Log("Invalid CreateRoom data sent!");
                                    break;
                                case 1:
                                    Debug.Log("Player not logger in!");
                                    SceneManager.LoadScene("Login");
                                    break;
                                default:
                                    Debug.Log("Invalid errorId!");
                                    break;
                            }
                        }
                        break;
                    }

                    case RoomTags.JoinSuccess:
                    {
                        var playerList = new List<Player>();
                        using (var reader = message.GetReader())
                        {
                            CurrentRoom = reader.ReadSerializable<Room>();
                            while (reader.Position < reader.Length)
                            {
                                var player = reader.ReadSerializable<Player>();
                                playerList.Add(player);
                                ChatManager.ServerMessage(player.Name + " joined the room.", MessageType.Room);
                            }

                            IsHost = playerList.Find(p => p.Id == GameControl.Client.ID).IsHost;
                        }

                        onSuccessfulJoinRoom?.Invoke(playerList);
                        break;
                    }

                    case RoomTags.JoinFailed:
                    {
                        var content = "Failed to join room.";
                        using (var reader = message.GetReader())
                        {
                            if (reader.Length != 1)
                            {
                                Debug.LogWarning("Invalid JoinRoomRoomFailed Error data received.");
                            }
                            else
                            {
                                switch (reader.ReadByte())
                                {
                                    case 0:
                                        Debug.Log("Invalid JoinRoom data sent!");
                                        break;
                                    case 1:
                                        Debug.Log("Player not logger in!");
                                        SceneManager.LoadScene("Login");
                                        break;
                                    case 2:
                                        Debug.Log("Player already is in a room!");
                                        content = "Already in a room.";
                                        break;
                                    case 3:
                                        Debug.Log("Room doesn't exist anymore");
                                        content = "The room doesn't exist anymore.";
                                        break;
                                    default:
                                        Debug.Log("Invalid errorId!");
                                        break;
                                }
                            }
                        }

                        ChatManager.ServerMessage(content, MessageType.Error);
                        break;
                    }

                    case RoomTags.LeaveSuccess:
                    {
                        ChatManager.ServerMessage("You left the room.", MessageType.Room);
                        CurrentRoom = null;
                        onSuccessfulLeaveRoom?.Invoke();
                        break;
                    }

                    // Another player joined
                    case RoomTags.PlayerJoined:
                    {
                        using (var reader = message.GetReader())
                        {
                            var player = reader.ReadSerializable<Player>();
                            ChatManager.ServerMessage(player.Name + " joined the room.", MessageType.Room);

                            onPlayerJoined?.Invoke(player);
                        }
                        break;
                    }

                    // Another player left
                    case RoomTags.PlayerLeft:
                    {
                        using (var reader = message.GetReader())
                        {
                            var leftId = reader.ReadUInt32();
                            var newHostId = reader.ReadUInt32();
                            var leaverName = reader.ReadString();
                            ChatManager.ServerMessage(leaverName + " left the room.", MessageType.Room);

                            if (newHostId == GameControl.Client.ID)
                            {
                                IsHost = true;
                            }

                            onPlayerLeft?.Invoke(leftId, newHostId);
                        }
                        break;
                    }

                    case RoomTags.GetOpenRooms:
                    {
                        var roomList = new List<Room>();

                        using (var reader = message.GetReader())
                        {
                            while (reader.Position < reader.Length)
                            {
                                roomList.Add(reader.ReadSerializable<Room>());
                            }
                        }

                        onReceivedOpenRooms?.Invoke(roomList);
                        break;
                    }

                    case RoomTags.GetOpenRoomsFailed:
                    {
                        Debug.Log("Player not logged in!");
                        SceneManager.LoadScene("Login");
                        break;
                    }

                    case RoomTags.StartGameSuccess:
                    {
                        SceneManager.LoadScene("Game");
                        break;
                    }

                    case RoomTags.StartGameFailed:
                    {
                        var content = "Failed to start game.";
                        using (var reader = message.GetReader())
                        {
                            if (reader.Length != 1)
                            {
                                Debug.LogWarning("Invalid StartGame Error data received.");
                                return;
                            }

                            switch (reader.ReadByte())
                            {
                                case 0:
                                    Debug.Log("Invalid CreateRoom data sent!");
                                    break;
                                case 1:
                                    Debug.Log("Player not logged in!");
                                    SceneManager.LoadScene("Login");
                                    break;
                                case 2:
                                    Debug.Log("You are not the host!");
                                    content = "Only the host can start a game!";
                                    break;
                                default:
                                    Debug.Log("Invalid errorId!");
                                    break;
                            }
                        }

                        ChatManager.ServerMessage(content, MessageType.Error);
                        break;
                    }
                }
            }
        }
    }
}
