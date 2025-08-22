using UnityEngine;

public class HealingPotion : MonoBehaviour
{
    [SerializeField] private int healAmount = 20;
    void Start()
    {

    }

    void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Player>(out Player player))
        {
            if (player.health == player.maxHealth) return; 
            if (player.health + healAmount > player.maxHealth)
            {
                player.health = player.maxHealth;
            }
            else
            {
                player.health += healAmount;
            }
            Destroy(gameObject);
        }
    }
}
