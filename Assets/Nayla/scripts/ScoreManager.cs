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
    [SerializeField] private List<TextMeshProUGUI> names;
    [SerializeField] private List<TextMeshProUGUI> scores;
    public string publicLeaderBoardKey = "6e27e986969cc70102cc8ff7652de566b198f6bdc84c988655621c995bb498ff";
    private int maxScore;

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

        SetLeaderBoardEntry(inputName.text, maxScore);
    }
    public void ClickOnButton()
    {
        inputScore = GameManager.Instance.score;

    }
    public void Update()
    {
        SubmitScore();
    }
    public void SetLeaderBoardEntry(string username, int score)
    {

        // foreach (string badWord in badWords)
        // {
        //     if (username.ToLower().Contains(badWord.ToLower()))
        //     {
        //         return;
        //     }
        // } 

        LeaderboardCreator.UploadNewEntry(publicLeaderBoardKey, username, score, (_) =>
        {
            GetLeaderboard();
            LeaderboardCreator.ResetPlayer(); // reset after upload so new entries don’t override
        });
    }
        public void GetLeaderboard()
    {
        LeaderboardCreator.GetLeaderboard(publicLeaderBoardKey, (msg) =>
        {
            int loopLentgh = (msg.Length < names.Count) ? msg.Length : names.Count;
            for (int i = 0; i < loopLentgh; ++i)
            {
                names[i].text = msg[i].Username;
                scores[i].text = msg[i].Score.ToString();
            }
        });
    }
}
