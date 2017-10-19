using System.Collections.Generic;
using Chat;
using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
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
            var writer = new DarkRiftWriter();
            writer.Write(roomname);
            writer.Write(isVisible);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Room, RoomSubjects.Create, writer), SendMode.Reliable);
        }

        public static void JoinRoom(ushort roomId)
        {
            var writer = new DarkRiftWriter();
            writer.Write(roomId);

            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Room, RoomSubjects.Join, writer), SendMode.Reliable);
        }

        public static void LeaveRoom()
        {
            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Room, RoomSubjects.Leave, new DarkRiftWriter()), SendMode.Reliable);
        }

        public static void GetOpenRooms()
        {
            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Room, RoomSubjects.GetOpenRooms, new DarkRiftWriter()), SendMode.Reliable);
        }

        public static void StartGame()
        {
            var writer = new DarkRiftWriter();
            writer.Write(CurrentRoom.Id);
            GameControl.Client.SendMessage(new TagSubjectMessage(Tags.Room, RoomSubjects.StartGame, writer), SendMode.Reliable);
        }
        #endregion

        private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
        {
            var message = e.Message as TagSubjectMessage;

            if (message == null || message.Tag != Tags.Room)
                return;

            // Successfully created Room
            if (message.Subject == RoomSubjects.CreateSuccess)
            {
                var reader = message.GetReader();
                var room = reader.ReadSerializable<Room>();
                var player = reader.ReadSerializable<Player>();

                IsHost = player.IsHost;
                CurrentRoom = room;
                ChatManager.ServerMessage("Created room " + room.Name + "!", MessageType.Room);

                onSuccessfulJoinRoom?.Invoke(new List<Player> { player });
            }

            // Failed to create Room
            else if (message.Subject == RoomSubjects.CreateFailed)
            {
                ChatManager.ServerMessage("Failed to create room.", MessageType.Error);
                var reader = message.GetReader();
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

            // Successfully joined Room
            else if (message.Subject == RoomSubjects.JoinSuccess)
            {
                var reader = message.GetReader();
                var playerList = new List<Player>();

                var room = reader.ReadSerializable<Room>();
                while (reader.Position < reader.Length)
                {
                    var player = reader.ReadSerializable<Player>();
                    playerList.Add(player);
                    ChatManager.ServerMessage(player.Name + " joined the room.", MessageType.Room);
                }

                IsHost = playerList.Find(p => p.Id == GameControl.Client.ID).IsHost;
                CurrentRoom = room;
                onSuccessfulJoinRoom?.Invoke(playerList);
            }

            // Failed to join Room
            else if (message.Subject == RoomSubjects.JoinFailed)
            {
                var content = "Failed to join room.";
                var reader = message.GetReader();
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
                ChatManager.ServerMessage(content, MessageType.Error);
            }

            // Successfully left Room
            else if (message.Subject == RoomSubjects.LeaveSuccess)
            {
                ChatManager.ServerMessage("You left the room.", MessageType.Room);
                CurrentRoom = null;
                onSuccessfulLeaveRoom?.Invoke();
            }

            // Another player joined the Room
            else if (message.Subject == RoomSubjects.PlayerJoined)
            {
                var reader = message.GetReader();
                var player = reader.ReadSerializable<Player>();
                ChatManager.ServerMessage(player.Name + " joined the room.", MessageType.Room);

                onPlayerJoined?.Invoke(player);
            }

            // Another player left the Room
            else if (message.Subject == RoomSubjects.PlayerLeft)
            {
                var reader = message.GetReader();
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

            // Received all available Rooms
            else if (message.Subject == RoomSubjects.GetOpenRooms)
            {
                var reader = message.GetReader();
                var roomList = new List<Room>();

                while (reader.Position < reader.Length)
                {
                    roomList.Add(reader.ReadSerializable<Room>());
                }
                onReceivedOpenRooms?.Invoke(roomList);
            }

            // Failed to receive all available Rooms
            else if (message.Subject == RoomSubjects.GetOpenRoomsFailed)
            {
                Debug.Log("Player not logged in!");
                SceneManager.LoadScene("Login");
            }

            // Successfully started Game
            else if (message.Subject == RoomSubjects.StartGameSuccess)
            {
                SceneManager.LoadScene("Game");
            }

            // Failed to start Game
            else if (message.Subject == RoomSubjects.StartGameFailed)
            {
                var content = "Failed to start game.";
                var reader = message.GetReader();
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
                ChatManager.ServerMessage(content, MessageType.Error);
            }
        }
    }
}
