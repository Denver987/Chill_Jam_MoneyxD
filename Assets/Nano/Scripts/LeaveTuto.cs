using UnityEngine;

public class LeaveTuto : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Assuming you have a GameManager script to handle game states
            GameManager.Instance.LoadScene(2);
        }
    }
}
