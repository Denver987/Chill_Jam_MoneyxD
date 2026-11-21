using UnityEngine;
using UnityEngine.SceneManagement;

public class DieTMP : MonoBehaviour
{
    public TMPro.TextMeshProUGUI dieText;
    public TMPro.TextMeshProUGUI SecondDieText;


    void Start()
    {
        dieText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void exitGame()
    {
        Application.Quit();
    }
    public void BackToMenu()
    {
        // Leaving a paused/level-up/game-over state must not freeze the menu.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
    public void ShowText(int level, float experience)
    {
        if (dieText != null)
            dieText.text = $"You Died at level {level}, with {experience} XP";
        if (SecondDieText != null && GameManager.Instance != null)
            SecondDieText.text = $"Your Score : " + GameManager.Instance.score;
    }
}
