using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Dan.Main;

public class LeaderBoardUI : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> names;
    [SerializeField] private List<TextMeshProUGUI> scores;

    public string publicLeaderBoardKey = "c27757b80fa469562d1ffd09d2ca68f2bc83b4b09b7ae661cf7d3e4485014e2e";

    // private string[] badWords = { "" }; idk if this is necessary


    private void Start()
    {
        GetLeaderboard();
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
}
