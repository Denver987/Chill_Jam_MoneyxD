using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    // Existing UI references
    public GameObject optionsMenu;
    public GameObject gameMenu;
    public GameObject leaderboardMenu;
    public GameObject textField;
    public TMP_InputField nameInputField;
    
    // Add UI for player selection
    public GameObject playerSelectionPanel;
    public GameObject playerButtonPrefab;
    public Transform playerButtonContainer;
    
    // Keys for player data
    private const string PLAYERS_COUNT_KEY = "PlayersCount";
    private const string PLAYER_NAME_PREFIX = "PlayerName_";
    private const string CURRENT_PLAYER_KEY = "CurrentPlayer";
    
    // Current list of players
    private List<string> playerNames = new List<string>();
    private string currentPlayerName;
    
    public void Start()
    {
        LoadPlayerNames();
        
        if (playerNames.Count > 0)
        {
            // We have existing players, show selection screen
            ShowPlayerSelection();
        }
        else
        {
            // First time use, show name entry
            textField.SetActive(true);
            gameMenu.SetActive(false);
            playerSelectionPanel.SetActive(false);
        }
        
        // Hide options menu
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(false);
        }
    }
    
    private void LoadPlayerNames()
    {
        playerNames.Clear();
        int playerCount = PlayerPrefs.GetInt(PLAYERS_COUNT_KEY, 0);
        
        for (int i = 0; i < playerCount; i++)
        {
            string name = PlayerPrefs.GetString(PLAYER_NAME_PREFIX + i);
            if (!string.IsNullOrEmpty(name))
            {
                playerNames.Add(name);
            }
        }
        
        // Get current player if set
        currentPlayerName = PlayerPrefs.GetString(CURRENT_PLAYER_KEY, "");
    }
    
    private void ShowPlayerSelection()
    {
        // Clear existing buttons
        foreach (Transform child in playerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create a button for each player
        foreach (string name in playerNames)
        {
            GameObject buttonObj = Instantiate(playerButtonPrefab, playerButtonContainer);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = name;
            
            // Set up button click
            buttonObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                SelectPlayer(name);
            });
        }
        
        // Show the selection panel
        playerSelectionPanel.SetActive(true);
        textField.SetActive(false);
        gameMenu.SetActive(false);
    }
    
    public void SelectPlayer(string name)
    {
        currentPlayerName = name;
        PlayerPrefs.SetString(CURRENT_PLAYER_KEY, name);
        PlayerPrefs.Save();
        
        // Show main menu
        playerSelectionPanel.SetActive(false);
        gameMenu.SetActive(true);
    }
    
    public void ShowNewPlayerInput()
    {
        // Show name entry screen for new player
        playerSelectionPanel.SetActive(false);
        textField.SetActive(true);
    }
    
    public void SubmitName()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            // Add new player
            string newName = nameInputField.text;
            if (!playerNames.Contains(newName))
            {
                playerNames.Add(newName);
                
                // Save updated player list
                PlayerPrefs.SetInt(PLAYERS_COUNT_KEY, playerNames.Count);
                for (int i = 0; i < playerNames.Count; i++)
                {
                    PlayerPrefs.SetString(PLAYER_NAME_PREFIX + i, playerNames[i]);
                }
            }
            
            // Set as current player
            currentPlayerName = newName;
            PlayerPrefs.SetString(CURRENT_PLAYER_KEY, newName);
            PlayerPrefs.Save();
            
            // Clear input field
            nameInputField.text = "";
            
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
    
    // Get current player name
    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(CURRENT_PLAYER_KEY, "Player");
    }
    
    // Your existing methods
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
    
    // Add a button to switch players from main menu
    public void SwitchPlayer()
    {
        ShowPlayerSelection();
    }
    
    // Other menu navigation methods remain the same
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
