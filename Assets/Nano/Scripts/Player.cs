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
    [Header ("Exp things")]
    public float experience = 0;
    public float experienceToNextLevel = 100f;
    public int level = 1;
    public float xpGainMultiplier = 1.2f;
    public System.Collections.Generic.List<DarkmoonCards> darkmoonCards = new System.Collections.Generic.List<DarkmoonCards>();
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
    [Header("other")]
    public GameObject canvas;

    private void Awake()
    {
        inputSystem = GetComponent<InputSystem>();
    }
    void Start()
    {
        postProcessing = GameObject.FindGameObjectWithTag("PP")?.GetComponent<Volume>();
        if (postProcessing == null || postProcessing.profile == null) return;

        if (postProcessing.profile.TryGet<Vignette>(out var vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteColor = vignette.color.value;
            originalVignetteSmoothness = vignette.smoothness.value;
            originalRounded = vignette.rounded.value;
            originalVignetteCenter = vignette.center.value;
        }
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
        if (experience >= experienceToNextLevel)
        {
            LevelUp();
        }

    }
    private void LevelUp()
    {
        level++;
        experience = 0;
        experienceToNextLevel *= xpGainMultiplier;
        DarkmoonCards newMoon = new DarkmoonCards();
        newMoon.ChooseCard();
        darkmoonCards.Add(newMoon);
    }

    public void TakeDamage(int damage, Vector3 hitDirection, float knockbackForce)
    {
        if (invulnerable || health <= 0) return;

        health -= damage;
        if (health < 0) health = 0;
        StartCoroutine(FlashRed());
        StartCoroutine(TakeKnockback(hitDirection, knockbackForce, 0.5f)); // Adjust knockback duration as needed
        SoundManager.PlaySound(SoundType.PlayerDamage, this.GetComponent<AudioSource>(), 0.5f); // Play damage sound with volume 0.5

        if (health <= 0)
        {
            Die();
        }
    }
    private IEnumerator FlashRed()
    {
        if (postProcessing == null || postProcessing.profile == null) yield break;

        if (postProcessing.profile.TryGet<Vignette>(out var vignette))
        {
            float half = Mathf.Max(0.0001f, hitFlashDuration * 0.5f);
            float t = 0f;
            vignette.color.Override(HitColor);

            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                vignette.intensity.value = Mathf.Lerp(0f, intensity, k);
                yield return null;
            }
            vignette.smoothness.Override(1f);
            vignette.rounded.Override(true);
            vignette.center.Override(new Vector2(0.5f, 0.5f));
        }

        yield return new WaitForSeconds(hitFlashDuration);

        if (postProcessing.profile.TryGet<Vignette>(out var vignetteRestore))
        {
            float half = Mathf.Max(0.0001f, hitFlashDuration * 0.5f);
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                vignetteRestore.intensity.value = Mathf.Lerp(hitFlashDuration, originalVignetteIntensity, k);
                yield return null;
            }
            vignetteRestore.color.Override(originalVignetteColor);
            vignetteRestore.smoothness.Override(originalVignetteSmoothness);
            vignetteRestore.rounded.Override(originalRounded);
            vignetteRestore.center.Override(originalVignetteCenter);
        }
    }

    private IEnumerator TakeKnockback(Vector3 direction, float force, float duration)
    {
        yield return new WaitForSeconds(duration / 2);
        inputSystem.TakeKnockback(direction, force, 0.05f);
        CameraSettings.Instance.StartShake(0.2f, 0.1f); // Adjust shake duration and intensity as needed
        invulnerable = true;

    }

    private void Die()
    {
        CanvasSingleton.Instance.die.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        CanvasSingleton.Instance.die.GetComponent<DieTMP>().ShowText(level, experience, darkmoonCards.Count);
        Time.timeScale = 0f; 

    }
}
