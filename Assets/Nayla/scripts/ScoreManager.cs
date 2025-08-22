using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Dan.Main;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class ScoreManager : MonoBehaviour
{

    [SerializeField] private int inputScore;
    [SerializeField] private TMP_InputField inputName;
    private List<(string, int)> names = new List<(string, int)>();
    List<string> nom = new List<string>();
    public string publicLeaderBoardKey = "6e27e986969cc70102cc8ff7652de566b198f6bdc84c988655621c995bb498ff";
    private int maxScore;

    public void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public UnityEvent<string, int> submitScoreEvent;
    public void SubmitScore()
    {
        maxScore = GameManager.Instance.score;
        LeaderboardCreator.GetLeaderboard(publicLeaderBoardKey, (msg) =>
        {
            foreach (var entry in msg)
            {
                if (entry.Username.Contains(inputName.text))
                {
                    if (entry.Score < maxScore)
                    {
                        maxScore = entry.Score;
                    }
                }
            }
        });

        submitScoreEvent.Invoke(inputName.text, maxScore);
    }
    public void ClickOnButton()
    {
        inputScore = GameManager.Instance.score;

    }
    public void Update()
    {
        SubmitScore();
    }
}
