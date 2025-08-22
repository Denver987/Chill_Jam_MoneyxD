using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject gameMenu;
    public void Start()
    {
        if (gameMenu != null || gameMenu.activeSelf == false)
        {
            gameMenu.SetActive(true);
        }
        if (optionsMenu != null && optionsMenu.activeSelf == true)
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
    public void BackToMenu()
    {
        optionsMenu.SetActive(false);
        gameMenu.SetActive(true);
    }

}
