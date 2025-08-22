using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject gameMenu;
    public GameObject leaderboardMenu;

    public void Start()
    {   if (gameMenu != null)
        {
            gameMenu.SetActive(true);
        }
        if (leaderboardMenu != null)
        {
            leaderboardMenu.SetActive(false);
        }
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(false);
        }

    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    public void Options()
    {
        optionsMenu.SetActive(true);
        gameMenu.SetActive(false);
    }
    public void exitGame()
    {
        Application.Quit();
    }
    public void BackToMenuFromOptions()
    {
        optionsMenu.SetActive(false);
        gameMenu.SetActive(true);
    }
    public void Leaderboard()
    {
        leaderboardMenu.SetActive(true);
        gameMenu.SetActive(false);
    }
    public void BackToMenuFromLeaderboard()
    {
        leaderboardMenu.SetActive(false);
        gameMenu.SetActive(true);
    }

}
