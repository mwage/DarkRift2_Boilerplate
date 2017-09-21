using DarkRift.Client.Unity;
using UnityEngine.SceneManagement;

public class GameControl : Singleton<GameControl>
{
    protected GameControl()
    {
    }

    public static UnityClient Client;

    //Keep Game Manager active and destroy any additional copys
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Client = GetComponent<UnityClient>();
    }

    //Start Game
    private void Start()
    {
        SceneManager.LoadScene("Login");
    }
}