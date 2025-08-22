using SmallHedge.SoundManager;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int score = 0;
    public  EnemySpawner EnemySpawner; // Reference to the EnemySpawner script
    public WaveManager WaveManager;
    public bool isTutorial = false; // Flag to indicate if the game is in tutorial mode
    public static GameManager Instance { get; private set; } // Singleton instance



    private void Awake()
    {


        EnemySpawner = GetComponentInChildren<EnemySpawner>();
        WaveManager = GetComponentInChildren<WaveManager>();
        Instance = this; // Set the singleton instance
        if(Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }
    public void LoadScene(int buildIndex)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(buildIndex);
    }
    // Reference to the WaveManager script

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score added: " + amount + ". Total score: " + score);
    }

    public void ResetScore()
    {
        score = 0;
        Debug.Log("Score reset. Total score: " + score);
    }

    public void StopGame()
    {
        Time.timeScale = 0; // Stop the game by setting time scale to 0
        foreach (var enemy in EnemySpawner.ActiveEnemies)
        {
            if (enemy != null)
            {
               enemy.gameObject.SetActive(false); // Deactivate each enemy
            }
        }
        EnemySpawner.ActiveEnemies.Clear();
        
        
        // Clear active enemies
        WaveManager.StopAllCoroutines(); // Stop all wave spawning coroutines
        EnemySpawner.enabled = false; // Disable the EnemySpawner
        WaveManager.enabled = false; // Disable the WaveManager


        Debug.Log("Game stopped. Final score: " + score);
        // Additional logic to stop the game can be added here
    }


    public void StartGame()
    {
        ResetScore();
        Debug.Log("Game started. Score reset to: " + score);
        // Additional logic to start the game can be added here
    }

    public void MainMenu()
    {

    }
}
