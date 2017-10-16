using UnityEngine;
using UnityEngine.UI;

namespace Rooms
{
    public class PlayerListing : MonoBehaviour
    {
        [SerializeField] private Text _playerName;
        public Player Player { get; private set; }

        public void Initialize(Player player)
        {
            Player = player;
            _playerName.text = player.IsHost ? player.Name + " (Host)" : player.Name;
        }
    }
}