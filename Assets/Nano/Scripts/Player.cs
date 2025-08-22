using DG.Tweening;
using SmallHedge.SoundManager;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int health = 100;
    [Header("Exp things")]
    public float experience = 0;
    public float experienceToNextLevel = 100f;
    public int level = 1;
    public float xpGainMultiplier = 1.2f;

    [Header("Invulnerability")]
    public bool invulnerable = false;
    public float invulnerabilityTime = 1f;
    private float timer = 0f;

    public InputSystem inputSystem;

    [Header("Vignette Settings")]
    public Volume postProcessing;
    public Color HitColor;
    public float intensity = 0.4f;
    public float hitFlashDuration = 0.1f;
    private float originalVignetteIntensity;
    private Color originalVignetteColor;
    private float originalVignetteSmoothness;
    private bool originalRounded;
    private Vector2 originalVignetteCenter;

    // Add these fields to your Player class
    [Header("Health Regeneration")]
    public float healingRegen = 0;        // Amount to heal per tick
    public float regenTickInterval = 1f;  // Heal every 1 second
    private Coroutine regenCoroutine;     // Reference to active regen coroutine
    private bool isRegenerating = false;  // Track if regeneration is active

    private void Awake()
    {
        inputSystem = GetComponent<InputSystem>();
    }
    void Start()
    {


        if (postProcessing.profile.TryGet<Vignette>(out var vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteColor = vignette.color.value;
            originalVignetteSmoothness = vignette.smoothness.value;
            originalRounded = vignette.rounded.value;
            originalVignetteCenter = vignette.center.value;
        }


        UpdateRegeneration();
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

    }

    public void UpdateLevel()
    {
        if (health < maxHealth)
        {
            health = maxHealth;
        }
        Time.timeScale = 0f;
        CanvasSingleton.Instance.CardsUI();
    }

    public void TakeDamage(int damage, Vector3 hitDirection, float knockbackForce)
    {
        if (invulnerable || health <= 0) return;

        health -= damage;
        if (health < 0) health = 0;
        StartCoroutine(FlashRed());
        StartCoroutine(TakeKnockback(hitDirection, knockbackForce, 0.5f)); // Adjust knockback duration as needed
        SoundManager.PlaySound(SoundType.PlayerDamage, this.GetComponent<AudioSource>(), 0.5f); // Play damage sound with volume 0.5

        // if (health <= 0)
        // {
        //     Die();
        // }
    }
    private IEnumerator FlashRed()
    {
        if (postProcessing == null || postProcessing.profile == null) yield break;

        if (postProcessing.profile.TryGet<Vignette>(out var vignette))
        {
            // Backup current state (in case another flash already modified it)
            float startIntensity = vignette.intensity.value;
            Color startColor = vignette.color.value;

            // Phase 1: Flash in (fade to red)
            float t = 0f;
            while (t < hitFlashDuration * 0.5f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / (hitFlashDuration * 0.5f));

                vignette.color.Override(Color.Lerp(startColor, HitColor, k));
                vignette.intensity.Override(Mathf.Lerp(startIntensity, intensity, k));
                yield return null;
            }

            // Stay red for a short moment
            yield return new WaitForSeconds(0.4f);

            // Phase 2: Flash out (fade back)
            t = 0f;
            while (t < hitFlashDuration * 0.5f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / (hitFlashDuration * 0.5f));

                vignette.color.Override(Color.Lerp(HitColor, originalVignetteColor, k));
                vignette.intensity.Override(Mathf.Lerp(intensity, originalVignetteIntensity, k));
                yield return null;
            }

            // Ensure restore
            vignette.color.Override(originalVignetteColor);
            vignette.intensity.Override(originalVignetteIntensity);
            vignette.smoothness.Override(originalVignetteSmoothness);
            vignette.rounded.Override(originalRounded);
            vignette.center.Override(originalVignetteCenter);
        }
    }


    private IEnumerator TakeKnockback(Vector3 direction, float force, float duration)
    {
        yield return new WaitForSeconds(duration / 2);
        inputSystem.TakeKnockback(direction, force, 0.05f);
        CameraSettings.Instance.StartShake(0.2f, 0.1f); // Adjust shake duration and intensity as needed
        invulnerable = true;

    }
    public void addExperience(int amount)
    {
        experience += amount;
        checkLevelUp();
    }
    private void checkLevelUp()
    {
        while (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel;
            level++;
            experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * xpGainMultiplier);
            UpdateLevel();
        }
    }

    // Add this method to start/update the regeneration
    private void UpdateRegeneration()
    {
        // Only run regen if we have some healing power
        if (healingRegen > 0)
        {
            // If regen is already running, stop it so we can restart with new values
            if (isRegenerating && regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
            }

            // Start the regeneration coroutine
            regenCoroutine = StartCoroutine(RegenerateHealth());
        }
        else if (isRegenerating && regenCoroutine != null)
        {
            // Stop regeneration if healing power becomes zero
            StopCoroutine(regenCoroutine);
            isRegenerating = false;
        }
    }

    // Coroutine that handles the actual healing
    private IEnumerator RegenerateHealth()
    {
        isRegenerating = true;

        while (true) // Run until stopped
        {
            // Wait for the tick interval
            yield return new WaitForSeconds(regenTickInterval);

            // Only heal if not at max health and game isn't paused
            if (health < maxHealth && Time.timeScale > 0)
            {
                // Calculate the target health (capped at max health)
                int amountToHeal = Mathf.RoundToInt(healingRegen);
                int targetHealth = Mathf.Min(health + amountToHeal, maxHealth);
                
                if (targetHealth > health)
                {
                    // Store the starting health for the tween
                    int startHealth = health;

                    // Tween the health value over 0.25 seconds
                    DOTween.To(
                        () => health,
                        x => health = x,
                        targetHealth,
                        1
                    ).SetEase(Ease.OutQuad);

                
                    // Wait for the tween to complete
                    yield return new WaitForSeconds(1);
                }
            }

            // If at max health and game is paused, wait until conditions change
            if (health >= maxHealth || Time.timeScale == 0)
            {
                yield return new WaitUntil(() => Time.timeScale > 0 && health < maxHealth);
            }
        }
    }


    public void IncreaseHealing(float amount)
    {
        healingRegen += amount;
        UpdateRegeneration();
    }




    // private void Die()
    // {
    //     CanvasSingleton.Instance.die.SetActive(true);
    //     Cursor.lockState = CursorLockMode.None;
    //     CanvasSingleton.Instance.die.GetComponent<DieTMP>().ShowText(level, experience, darkmoonCards.Count);
    //     Time.timeScale = 0f; 
    //     Debug.Log("Player has died. Level: " + level + ", Experience: " + experience + ", Cards: " + darkmoonCards.Count);


    //     Debug.Log("Player died!");
    //     CameraSettings.Instance.OnPlayerDeath();
    //     if (CameraSettings.Instance == null)
    //     {
    //         Debug.LogError("CameraSettings.Instance is NULL. Is the script on the Main Camera and enabled?");
    //         return;
    //     }
    // }
}
