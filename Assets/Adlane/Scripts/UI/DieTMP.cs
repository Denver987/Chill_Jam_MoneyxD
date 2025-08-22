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
        foreach (var obj in CanvasSingleton.Instance.GetComponentsInChildren<Transform>())
        {
            obj.gameObject.SetActive(false);
        }
    }
    public void ShowText(int level, float experience)
    {
        dieText.text = $"You Died at level {level}, with {experience} XP";
        SecondDieText.text = $"Your Score : " + GameManager.Instance.score;
    }
}
