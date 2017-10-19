using DarkRift.Client.Unity;
using UnityEngine.SceneManagement;

namespace Launcher
{
    public class GameControl : Singleton<GameControl>
    {
        protected GameControl()
        {
        }

        private UnityClient _client;

        public static UnityClient Client => Instance?._client;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _client = GetComponent<UnityClient>();
        }

        private void Start()
        {
            SceneManager.LoadScene("Login");
        }
    }
}