using DarkRift;
using DarkRift.Client;
using DarkRiftTags;
using Launcher;
using Login;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        GameControl.Client.MessageReceived += OnDataHandler;
    }

    private void OnDestroy()
    {
        GameControl.Client.MessageReceived -= OnDataHandler;
    }

    private static void OnDataHandler(object sender, MessageReceivedEventArgs e)
    {
        var message = e.Message as TagSubjectMessage;

        if (message != null && message.Tag == Tags.Login && message.Subject == LoginSubjects.LogoutSuccess)
        {
            SceneManager.LoadScene("Login");
        }
    }

    public void Logout()
    {
        LoginManager.Logout();
    }
}
