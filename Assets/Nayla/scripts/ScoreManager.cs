using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Dan.Main;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour {
    

    [SerializeField] private TextMeshProUGUI inputScore;
    [SerializeField] private TMP_InputField inputName;
    private List<(string, int)> names = new List<(string, int)>();

    public void Start()
    { 
        DontDestroyOnLoad(this.gameObject);
    }

    public UnityEvent<string, int> submitScoreEvent;
    public void SubmitScore()
    {
        LeaderboardCreator.GetLeaderboard("c27757b80fa469562d1ffd09d2ca68f2bc83b4b09b7ae661cf7d3e4485014e2e", (msg) =>
        {
            foreach (var entry in msg)
            {
                names.Add((entry.Username, entry.Score));
            }
        });
        if (!names.Contains((inputName.text, int.Parse(inputScore.text))))
        {
            if (int.Parse(inputScore.text) < GameManager.Instance.score)
            {
                submitScoreEvent.Invoke(inputName.text, GameManager.Instance.score);
            }
        }
    }
}
