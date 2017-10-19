using System.Collections.Generic;
using Chat;
using UnityEngine;
using UnityEngine.UI;

namespace Friends
{
    public class FriendLayoutGroup : MonoBehaviour
    {
        [SerializeField] private GameObject _onlineFriendPrefab;
        [SerializeField] private GameObject _offlineFriendPrefab;
        [SerializeField] private GameObject _friendRequestPrefab;
        [SerializeField] private InputField _addFriendInput;

        private readonly List<FriendListing> _onlineFriends = new List<FriendListing>();
        private readonly List<FriendListing> _offlineFriends = new List<FriendListing>();
        private readonly List<FriendListing> _friendRequests = new List<FriendListing>();

        public void Initialize()
        {
            FriendManager.onFriendLogin += FriendLogin;
            FriendManager.onFriendLogout += FriendLogout;
            FriendManager.onSuccessfulGetAllFriends += FriendListUpdate;
            FriendManager.onSuccessfulRemoveFriend += RemoveFriend;
            FriendManager.onSuccessfulDeclineRequest += RemoveFriend;
            FriendManager.onSuccessfulAcceptRequest += RequestAccepted;
            FriendManager.onSuccessfulFriendRequest += UnansweredRequest;
            FriendManager.onNewFriendRequest += NewRequest;

            FriendManager.GetAllFriends();
        }

        public void Destroy()
        {
            FriendManager.onFriendLogin -= FriendLogin;
            FriendManager.onFriendLogout -= FriendLogout;
            FriendManager.onSuccessfulGetAllFriends -= FriendListUpdate;
            FriendManager.onSuccessfulRemoveFriend -= RemoveFriend;
            FriendManager.onSuccessfulDeclineRequest -= RemoveFriend;
            FriendManager.onSuccessfulAcceptRequest -= RequestAccepted;
            FriendManager.onSuccessfulFriendRequest -= UnansweredRequest;
            FriendManager.onNewFriendRequest -= NewRequest;
        }

        private void FriendLogin(string friendName)
        {
            // Add new online listing
            AddListing(friendName, _onlineFriends, _onlineFriendPrefab);

            // Remove old offline Listing
            RemoveListing(friendName, _offlineFriends);

            SortListings();
        }

        private void FriendLogout(string friendName)
        {
            // Add new offline listing
            AddListing(friendName, _offlineFriends, _offlineFriendPrefab);

            // Remove old online Listing
            RemoveListing(friendName, _onlineFriends);

            SortListings();
        }

        private void RemoveFriend(string friendName)
        {
            if (_onlineFriends.Exists(l => l.FriendName == friendName))
            {
                RemoveListing(friendName, _onlineFriends);
            }
            else if (_offlineFriends.Exists(l => l.FriendName == friendName))
            {
                RemoveListing(friendName, _offlineFriends);
            }
            else if (_friendRequests.Exists(l => l.FriendName == friendName))
            {
                RemoveListing(friendName, _friendRequests);
            }
        }

        private void NewRequest(string friendName)
        {
            AddListing(friendName, _friendRequests, _friendRequestPrefab);
            SortListings();
        }

        private void UnansweredRequest(string friendName)
        {

            AddListing(friendName, _offlineFriends, _offlineFriendPrefab);
            SortListings();
        }

        private void RequestAccepted(string friendName, bool online)
        {
            RemoveListing(friendName, _friendRequests);
            RemoveListing(friendName, _offlineFriends);
            if (online)
            {
                AddListing(friendName, _onlineFriends, _onlineFriendPrefab);
            }
            else
            {
                AddListing(friendName, _offlineFriends, _offlineFriendPrefab);
            }


            SortListings();
        }

        private void FriendListUpdate(string[] onlineFriends, string[] offlineFriends, string[] friendRequests, string[] unansweredRequests)
        {
            foreach (var friend in onlineFriends)
            {
                AddListing(friend, _onlineFriends, _onlineFriendPrefab);
            }
            foreach (var friend in offlineFriends)
            {
                AddListing(friend, _offlineFriends, _offlineFriendPrefab);
            }
            foreach (var friend in friendRequests)
            {
                AddListing(friend, _friendRequests, _friendRequestPrefab);
            }
            foreach (var friend in unansweredRequests)
            {
                AddListing(friend, _offlineFriends, _offlineFriendPrefab);
            }
            SortListings();
        }

        private void AddListing(string friendName, List<FriendListing> list, GameObject prefab)
        {
            var friendListingObject = Instantiate(prefab, transform, false);
            var friendListing = friendListingObject.GetComponent<FriendListing>();
            friendListing.Initialize(friendName);
            list.Add(friendListing);
        }

        private void RemoveListing(string friendName, List<FriendListing> list)
        {
            var index = list.FindIndex(fL => fL.FriendName == friendName);
            if (index != -1)
            {
                Destroy(list[index].gameObject);
                list.RemoveAt(index);
            }
        }

        private void SortListings()
        {
            _onlineFriends.Sort((x, y) => string.CompareOrdinal(x.FriendName, y.FriendName));
            _offlineFriends.Sort((x, y) => string.CompareOrdinal(x.FriendName, y.FriendName));
            _friendRequests.Sort((x, y) => string.CompareOrdinal(x.FriendName, y.FriendName));

            for (var i = 0; i < _onlineFriends.Count; i++)
            {
                _onlineFriends[i].transform.SetSiblingIndex(i);
            }
            for (var i = 0; i < _offlineFriends.Count; i++)
            {
                _offlineFriends[i].transform.SetSiblingIndex(_onlineFriends.Count + i);
            }
            for (var i = 0; i < _friendRequests.Count; i++)
            {
                _friendRequests[i].transform.SetSiblingIndex(_onlineFriends.Count + _offlineFriends.Count + i);
            }
        }

        public void AddFriend()
        {
            if (string.IsNullOrWhiteSpace(_addFriendInput.text))
            {
                ChatManager.ServerMessage("Please enter a valid name.", MessageType.Error);
                return;
            }
            
            FriendManager.SendFriendRequest(_addFriendInput.text);
            _addFriendInput.text = "";
        }

        public void TogglePanel()
        {
            var panel = transform.parent.parent.parent.gameObject;
            panel.SetActive(!panel.activeSelf);
        }
    }
}
