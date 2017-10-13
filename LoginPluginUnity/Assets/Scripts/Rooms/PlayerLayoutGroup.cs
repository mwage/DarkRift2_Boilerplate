using System.Collections.Generic;
using UnityEngine;

namespace Rooms
{
    public class PlayerLayoutGroup : MonoBehaviour
    {
        [SerializeField] private GameObject _playerListingPrefab;

        private List<PlayerListing> _playerListings = new List<PlayerListing>();

        private void Awake()
        {
            RoomManager.onPlayerJoined += PlayerJoinedRoom;
            RoomManager.onPlayerLeft += PlayerLeftRoom;
        }

        private void OnDestroy()
        {
            RoomManager.onPlayerJoined -= PlayerJoinedRoom;
            RoomManager.onPlayerLeft -= PlayerLeftRoom;
        }

        public void JoinedRoom(List<Player> playerList)
        {
            foreach (var player in playerList)
            {
                PlayerJoinedRoom(player);
            }
        }

        private void PlayerJoinedRoom(Player player)
        {
            var playerListing = Instantiate(_playerListingPrefab, transform, false).GetComponent<PlayerListing>();
            playerListing.Initialize(player);
            _playerListings.Add(playerListing);
        }

        private void PlayerLeftRoom(uint leftId, uint newHostId)
        {
            // Delete old player
            var index = _playerListings.FindIndex(pL => pL.Player.Id == leftId);
            if (index != -1)
            {
                Destroy(_playerListings[index].gameObject);
                _playerListings.RemoveAt(index);
            }

            // Set new host
            var newHost = _playerListings.Find(pL => pL.Player.Id == newHostId);
            newHost.Player.IsHost = true;
            newHost.Initialize(newHost.Player);
        }

        public void RemovePlayers()
        {
            foreach (var listing in _playerListings)
            {
                Destroy(listing.gameObject);
                _playerListings = new List<PlayerListing>();
            }
        }
    }
}
