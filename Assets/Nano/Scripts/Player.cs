using SmallHedge.SoundManager;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int health = 100;

    [Header("Invulnerability")]
    public bool invulnerable = false;
    public float invulnerabilityTime = 1f;
    private float timer = 0f;

    public InputSystem inputSystem;

    private void Awake()
    {
        inputSystem = GetComponent<InputSystem>();
    }

    private void Update()
    {
        if (invulnerable)
        {
            timer += Time.deltaTime;
            if (timer >= invulnerabilityTime)
            {
                invulnerable = false;
                timer = 0f;
            }
        }

        /*if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Testing knockback!");
            Vector3 fakeDirection = -transform.forward; // push backward
            inputSystem.TakeKnockback(fakeDirection, 15f, 0.6f);
        }*/

    }

    public void TakeDamage(int damage, Vector3 hitDirection, float knockbackForce)
    {
        if (invulnerable || health <= 0) return;

        health -= damage;
        if (health < 0) health = 0;

        StartCoroutine(TakeKnockback(hitDirection, knockbackForce, 0.5f)); // Adjust knockback duration as needed
        SoundManager.PlaySound(SoundType.PlayerDamage,this.GetComponent<AudioSource>(),0.5f); // Play damage sound with volume 0.5

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator TakeKnockback(Vector3 direction, float force, float duration)
    {
        yield return new WaitForSeconds(duration /2);
        inputSystem.TakeKnockback(direction, force, 0.05f);
        CameraSettings.Instance.StartShake(0.2f, 0.1f); // Adjust shake duration and intensity as needed
        invulnerable = true;

    }

    private void Die()
    {
        Debug.Log("Player died!");
        // TODO: death screen, respawn, etc.
    }
}
