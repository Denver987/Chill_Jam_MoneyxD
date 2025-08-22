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
        // Use InputSystem's resume helper so internal state (paused, action map, locks) stays consistent
        var input = CanvasSingleton.Instance?.player?.GetComponent<InputSystem>();
        if (input != null)
        {
            input.ResumeFromUI();
        }
        else
        {
            // fallback if input component not found
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CanvasSingleton.Instance.HidePausePanel();
        }

        gameObject.SetActive(false); // Hide the pause panel
    }
    public void Back()
    {
        Options.SetActive(false);
        mainPausePanel.SetActive(true);
    }

}
