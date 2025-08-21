using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float score = 0f;
    public  EnemySpawner EnemySpawner; // Reference to the EnemySpawner script
    public WaveManager WaveManager; // Reference to the WaveManager script

    public void AddScore(float amount)
    {
        score += amount;
        Debug.Log("Score added: " + amount + ". Total score: " + score);
    }

    public void ResetScore()
    {
        score = 0f;
        Debug.Log("Score reset. Total score: " + score);
    }

    public void StopGame()
    {
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
