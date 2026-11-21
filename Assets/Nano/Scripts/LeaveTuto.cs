using UnityEngine;

public class LeaveTuto : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LoadScene(2);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }
    }
}
