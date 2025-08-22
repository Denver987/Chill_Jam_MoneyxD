using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public GameObject Options;
    public GameObject mainPausePanel;
    private void Start()
    {
    }

    public void ShowOptions()
    {
        Options.SetActive(true);
        mainPausePanel.SetActive(false);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void ResumeGame()
    {
        Time.timeScale = 1f; // Resume the game
        gameObject.SetActive(false); // Hide the pause panel
        Cursor.lockState = CursorLockMode.None; // Lock the cursor
        Cursor.visible = false;
        CanvasSingleton.Instance.HidePausePanel(); // Hide the pause panel from the singleton
    }
    public void Back()
    {
        Options.SetActive(false);
        mainPausePanel.SetActive(true);
    }

}
