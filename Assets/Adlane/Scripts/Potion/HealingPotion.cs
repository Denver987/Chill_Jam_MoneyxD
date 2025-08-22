using SmallHedge.SoundManager;
using System.Collections;
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
        StartCoroutine(Heal(other));
    }

    private IEnumerator Heal(Collider collider)
    {
        if (collider.gameObject.TryGetComponent<Player>(out Player player))
        {
            if (player.health == player.maxHealth)  yield break;
            if (player.health + healAmount > player.maxHealth)
            {
                player.health = player.maxHealth;
            }
            else
            {
                player.health += healAmount;
            }
            SoundManager.PlaySound(SoundType.healing, player.gameObject.GetComponent<AudioSource>(), 0.5f);
            StartCoroutine(player.FlashGreen());

        }
        yield return new WaitForSeconds(1.1f);
        this.gameObject.SetActive(false);
    }
}
