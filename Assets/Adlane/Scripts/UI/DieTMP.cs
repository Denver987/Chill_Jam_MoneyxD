using UnityEditor.SearchService;
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
        SceneManager.LoadScene(0);
        gameObject.SetActive(false);
    }
    public void ShowText(int level, float experience, int count)
    {
        dieText.text = $"You Died at level {level}, with {experience} XP, and {count} Cards.";
        SecondDieText.text = $"Your Score : (TODO)";
    }
}
