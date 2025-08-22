using System.Security.Cryptography.X509Certificates;
using SmallHedge.SoundManager;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;
using System.Collections;

public class Knockback : MonoBehaviour
{
    public Camera cam;

    public float hitCooldown = 0.5f;
    public float initialKnockbackStrength = 10f;
    public float maxChargeTime = 2f;
    public float maxKnockbackStrength = 20f;
    public float groundMultiplier = 2f;
    public AnimationCurve chargeCurve;

    [Header("Screen Shake")]
    public KnockbackScreenshake screenShake;

    public float maxDistance = 100f; // maximum distance to hit something   
    public float upwardForce = 0.5f; // how much upward force to apply when hitting the ground
    private float nextAllowedHitTime = 0f;
    private float chargeStartTime = -1f;
    public bool isCharging = false;
    private Rigidbody rb;
    public Animator animator;
    public GameObject vfx;
    public float ChargeNormalized { get; private set; } = 0f;

    private float maxChargeReachedTime = -1f;
    private bool hasAutoReleased = false;
    private bool shockwaveAnimationComplete = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (screenShake == null) screenShake = GetComponentInChildren<KnockbackScreenshake>();
    }

    void Update()
    {
        if (isCharging)
        {
            ChargeNormalized = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.0001f, maxChargeTime));

            // Start shaking when reaching threshold
            if (!screenShake.IsShaking && ChargeNormalized >= screenShake.shakeThreshold)
            {
                // Calculate total shake duration from threshold to auto-release
                float shakeStartProgress = screenShake.shakeThreshold;
                float totalShakeDuration = (maxChargeTime * (1f - shakeStartProgress)) + screenShake.autoReleaseDelay;
                screenShake.StartProgressiveShake(totalShakeDuration);
            }

            // Track when max charge is reached
            if (ChargeNormalized >= 1f && maxChargeReachedTime < 0f)
            {
                maxChargeReachedTime = Time.time;
            }

            // Auto-release after holding at max charge for the delay time
            if (!hasAutoReleased && maxChargeReachedTime > 0f && (Time.time - maxChargeReachedTime) >= screenShake.autoReleaseDelay)
            {
                hasAutoReleased = true;
                ReleaseCharge();
            }
        }
        // --- Crosshair + Outline (aim ray) ---
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        bool targetable = false;
        Outline outlineTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // �in range & hittable�
            var hitable = hit.transform.GetComponent<IHitable>();
            if (hitable != null)
            {
                targetable = true;

                // Try to find an Outline on the hit object, its parent, or child (disabled by default on prefab)
                outlineTarget = hit.transform.GetComponent<Outline>()
                               ?? hit.transform.GetComponentInParent<Outline>()
                               ?? hit.transform.GetComponentInChildren<Outline>();
                // (We only toggle .enabled; we don't AddComponent here per QuickOutline guidance.)
            }
        }

        CameraSettings.Instance?.SetCrosshairTargetable(targetable, outlineTarget);

    }

    public void knockback(InputAction.CallbackContext context)
    {
        if (Time.time < nextAllowedHitTime) return;
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.Shockwave) return;

        if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            hasAutoReleased = false;
            maxChargeReachedTime = -1f;
            animator.SetBool("ShockWave", true);
            return;
        }

        if (context.phase == InputActionPhase.Canceled && isCharging && !hasAutoReleased)
        {
            ReleaseCharge();
        }
    }

    private void ReleaseCharge()
    {
        if (!isCharging) return;

        SoundManager.PlaySound(SoundType.spell1, this.GetComponent<AudioSource>(), 0.25f);
        isCharging = false;

        // Stop the progressive shake
        screenShake.StopShake();

        float held = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.0001f, maxChargeTime));
        float t = chargeCurve.Evaluate(held);
        float finalKnockbackStrengthEnemy = Mathf.Lerp(initialKnockbackStrength, maxKnockbackStrength, t);
        float finalKnockbackStrengthPlayer = finalKnockbackStrengthEnemy * groundMultiplier;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            IHitable hitable = hit.transform.GetComponent<IHitable>();
            if (hitable != null)
            {
                SoundManager.PlaySound(SoundType.magicianspells, this.GetComponent<AudioSource>(), 0.25f);
                if (hit.transform.gameObject.GetComponent<BouncingShell>() != null)
                {
                    hitable.Execute(transform, finalKnockbackStrengthEnemy);
                    nextAllowedHitTime = Time.time + hitCooldown;

                }
                else
                {
                    hitable.ExecuteWithUpwardBias(transform, finalKnockbackStrengthEnemy, upwardForce);
                    nextAllowedHitTime = Time.time + hitCooldown;
                }




            }
            
        }

        StartCoroutine(ReleaseShockwave());
        // Reset everything
        ChargeNormalized = 0f;
        animator.SetBool("ShockWave", false);
        hasAutoReleased = false;
        maxChargeReachedTime = -1f;
    }
    private IEnumerator ReleaseShockwave()
    {
        shockwaveAnimationComplete = false;
        vfx.SetActive(true);
        yield return new WaitUntil(() => shockwaveAnimationComplete);
        vfx.SetActive(false);
    }
    public void ShockwaveAnimationComplete()
    {
        shockwaveAnimationComplete = true;
    }
    
    
}

