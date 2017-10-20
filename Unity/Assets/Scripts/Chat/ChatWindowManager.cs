using System;
using Friends;
using Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChatWindowManager : MonoBehaviour
    {
        public GameObject ChatPanels;
        public GameObject FriendPanels;
        [SerializeField] private InputField _inputField;
        [SerializeField] private Text _channelText;
        [SerializeField] private Text _messageText;
        [SerializeField] private GameObject _placeHolderText;
        [SerializeField] private ChatLayoutGroup _chatLayoutGroup;
        [SerializeField] private ChannelLayoutGroup _channelLayoutGroup;
        [SerializeField] private FriendLayoutGroup _friendLayoutGroup;

        private RectTransform _inputFieldTransform;
        private MessageType _outputMessageType;
        private string _outputChannelName;
        private bool _checkInput = true;
        private int _index;

        private void Awake()
        {
            _inputFieldTransform = _inputField.GetComponent<RectTransform>();
            _chatLayoutGroup.Initialize();
            _channelLayoutGroup.Initialize(this);
            _friendLayoutGroup.Initialize();
            SetOutputChannel();

            ChatManager.onActivateChat += ActivateInputField;
        }

        private void OnDestroy()
        {
            _chatLayoutGroup.Destroy();
            _channelLayoutGroup.Destroy();
            _friendLayoutGroup.Destroy();

            ChatManager.onActivateChat -= ActivateInputField;
        }

        private void Update()
        {
            _inputFieldTransform.sizeDelta = new Vector2(395 - _channelText.rectTransform.sizeDelta.x, 30);
        }

        private void OnGUI()
        {
            // Navigate with Enter
            if (Input.GetKeyDown(KeyCode.Return) && _checkInput)
            {
                if (_inputField.isFocused)
                {
                    if (_inputField.text != "")
                    {
                        SendMessage();
                    }
                    _checkInput = false;
                }
                else
                {
                    if (!FriendPanels.activeSelf)
                    {
                        ActivateInputField(_outputMessageType, _outputChannelName);
                    }
                    else
                    {
                        ChatPanels.SetActive(true);
                        _placeHolderText.SetActive(false);
                        _inputField.ActivateInputField();
                    }
                    _checkInput = false;
                }
            }

            if (Input.GetKeyUp(KeyCode.Return))
            {
                _checkInput = true;
            }

            // Tab to switch between channels
            if (Input.GetKeyDown(KeyCode.Tab) && _checkInput)
            {
                if (_inputField.isFocused)
                {
                    NextOutput();
                }
                else
                {
                    ActivateInputField(_outputMessageType, _outputChannelName);
                }
                _checkInput = false;
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                _checkInput = true;
            }
        }

        // Sets the output channel to selected channel
        public void SelectChannel(ChannelListing channelListing)
        {
            var idx = _channelLayoutGroup.ActiveChannels.IndexOf(channelListing);
            if (idx == -1)
                return;

            _index = idx;
            _outputMessageType = channelListing.MessageType;
            _outputChannelName = channelListing.Name;
            ConfigureOutput();
        }

        // Selects the next active channel
        public void NextOutput()
        {
            if (_channelLayoutGroup.ActiveChannels.Count != 0)
            {
                _index++;
                _index = _index % _channelLayoutGroup.ActiveChannels.Count;
            }
            SetOutputChannel();
            _inputField.ActivateInputField();
        }

        // Sets the output channel to the currently selected active channel or default
        private void SetOutputChannel()
        {
            if (_channelLayoutGroup.ActiveChannels.Count == 0)
            {
                _outputMessageType = MessageType.All;
                _outputChannelName = "";

                // Otherwise the layoutgroup wont scale the channeltext size back to 0. Don't ask me why...
                _channelText.text = ".";
            }
            else
            {
                _outputMessageType = _channelLayoutGroup.ActiveChannels[_index].MessageType;
                _outputChannelName = _channelLayoutGroup.ActiveChannels[_index].Name;
            }

            ConfigureOutput();
        }

        private void ConfigureOutput()
        {
            _channelText.text = _outputChannelName != "" ? "[" + _outputChannelName + "]" : "";
            _channelText.color = ChatManager.ChatColors[_outputMessageType];
            _messageText.color = ChatManager.ChatColors[_outputMessageType];
        }

        // Sets output to selection and activates the input field
        public void ActivateInputField(MessageType messageType, string channel)
        {
            var idx = _channelLayoutGroup.ActiveChannels.FindIndex(c => c.MessageType == messageType && c.Name == channel);
            if (idx == -1)
            {
                if (messageType == MessageType.Private)
                {
                    _channelLayoutGroup.AddPrivateChannel(channel);
                    idx = _channelLayoutGroup.ActiveChannels.Count - 1;
                }
                else
                {
                    return;
                }
            }

            _index = idx;
            _outputChannelName = channel;
            _outputMessageType = messageType;
            ConfigureOutput();
            ActivateChatPanel();
        }

        // Opens the Chat Panel and closes Friend Panel
        public void ActivateChatPanel()
        {
            ChatPanels.SetActive(true);
            FriendPanels.SetActive(false);
            _inputField.ActivateInputField();
            _placeHolderText.SetActive(false);
        }

        // Closes the Chat and Friends Panel
        public void DeactivatePanels()
        {
            ChatPanels.SetActive(false);
            FriendPanels.SetActive(false);
            _placeHolderText.SetActive(true);
        }

        private void SendMessage()
        {
            if (_inputField.text[0] == '/')
            {
                ChatManager.Command(_inputField.text);
            }
            else
            {
                switch (_outputMessageType)
                {
                    case MessageType.Private:
                        ChatManager.SendPrivateMessage(_outputChannelName, _inputField.text);
                        break;

                    case MessageType.Room:
                        if (RoomManager.CurrentRoom == null)
                        {
                            Debug.Log("You're currently not in a Room.");
                            ChatManager.ServerMessage("You're currently not in a Room.", MessageType.Error);
                        }
                        else
                        {
                            ChatManager.SendRoomMessage(_inputField.text);
                        }
                        break;

                    case MessageType.ChatGroup:
                        ChatManager.SendGroupMessage(_outputChannelName, _inputField.text);
                        break;

                    case MessageType.All:
                        ChatManager.ServerMessage("Not in a channel. Use \"/join channelname\" to join one or \"/list\" to list all active channels!", MessageType.All);
                        break;
                    default:
                        Debug.Log("Invalid MessageType.");
                        break;
                }
            }

            _inputField.text = "";
            ChatManager.ActivateChatInput(_outputMessageType, _outputChannelName);
        }
    }
}
