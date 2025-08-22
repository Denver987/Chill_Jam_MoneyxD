using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Dan.Main;

public class LeaderBoardUI : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> names;
    [SerializeField] private List<TextMeshProUGUI> scores;

    public string publicLeaderBoardKey = "6e27e986969cc70102cc8ff7652de566b198f6bdc84c988655621c995bb498ff";

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

}
