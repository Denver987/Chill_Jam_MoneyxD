using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject gameMenu;
    public GameObject leaderboardMenu;
    public GameObject textField;
    public TMP_InputField nameInputField;
    private const string HAS_ENTERED_NAME_KEY = "HasEnteredName";
    private const string PLAYER_NAME_KEY = "PlayerName";
    public void Start()
    {
        bool hasEnteredName = PlayerPrefs.GetInt(HAS_ENTERED_NAME_KEY, 0) == 1;
        if (hasEnteredName)
        {
            textField.SetActive(false);
            gameMenu.SetActive(true);
        }
        else
        {
            textField.SetActive(true);
            gameMenu.SetActive(false);
        }
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(false);
        }

    }
    public void SubmitName()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            // Store the name
            PlayerPrefs.SetString(PLAYER_NAME_KEY, nameInputField.text);
            // Mark that player has entered name
            PlayerPrefs.SetInt(HAS_ENTERED_NAME_KEY, 1);
            // Save immediately
            PlayerPrefs.Save();
            
            // Show main menu
            textField.SetActive(false);
            gameMenu.SetActive(true);
        }
        else
        {
            // Optional: show error if name is empty
            Debug.Log("Please enter a name");
        }
    }
    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
    }
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    public void Options()
    {
        optionsMenu.SetActive(true);
        gameMenu.SetActive(false);
    }
    public void exitGame()
    {
        Application.Quit();
    }
    public void BackToMenuFromOptions()
    {
        optionsMenu.SetActive(false);
        gameMenu.SetActive(true);
    }
    public void Leaderboard()
    {
        leaderboardMenu.SetActive(true);
        gameMenu.SetActive(false);
    }
    public void BackToMenuFromLeaderboard()
    {
        leaderboardMenu.SetActive(false);
        gameMenu.SetActive(true);
    }

}
