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
    public float initialKnockbackStrength = 3f;
    public float maxChargeTime = 2f;
    public float maxKnockbackStrength = 15f;
    public float groundMultiplier = 1.4f;
    public AnimationCurve chargeCurve;

    [Header("Screen Shake")]
    public KnockbackScreenshake screenShake;

    public float maxDistance = 10f; // maximum distance to hit something   
    public float minReach = 5.5f;
    public float maxReach = 9.5f;
    public float maxGroundReach = 3.5f;
    public float minRadius = 0.5f;
    public float maxRadius = 1.5f;
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
        if (Time.timeScale == 0f) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
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
        Ray ray = GetAimRay();
        bool targetable = false;
        Outline outlineTarget = null;
        float aimReach = isCharging ? Mathf.Lerp(minReach, maxReach, ChargeNormalized) : minReach;
        float aimRadius = isCharging ? Mathf.Lerp(minRadius, maxRadius, ChargeNormalized) : minRadius;

        IHitable hitable = null;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, aimReach, ~0, QueryTriggerInteraction.Ignore))
            hitable = hit.collider.GetComponentInParent<IHitable>() ?? hit.collider.GetComponentInChildren<IHitable>();

        if (hitable == null)
        {
            foreach (var sHit in Physics.SphereCastAll(ray, aimRadius, aimReach, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = sHit.collider.GetComponentInParent<IHitable>() ?? sHit.collider.GetComponentInChildren<IHitable>();
                if (h != null) { hitable = h; hit = sHit; break; }
            }
        }

        if (hitable != null)
        {
            targetable = true;
            outlineTarget = hit.transform.GetComponent<Outline>()
                           ?? hit.transform.GetComponentInParent<Outline>()
                           ?? hit.transform.GetComponentInChildren<Outline>();
        }

        CameraSettings.Instance?.SetCrosshairTargetable(targetable, outlineTarget);

    }

    public void knockback(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || Time.timeScale == 0f)
        {
            if (context.canceled) CancelCharge();
            return;
        }
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

        isCharging = false;

        // Stop progressive charge shake
        screenShake.StopShake();

        float held = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.0001f, maxChargeTime));
        float t = (chargeCurve != null && chargeCurve.length > 0) ? chargeCurve.Evaluate(held) : held;

        // Differentiated power scaling between tap and full charge
        float finalKnockbackStrengthEnemy = Mathf.Lerp(initialKnockbackStrength, maxKnockbackStrength, t);
        float finalKnockbackStrengthPlayer = finalKnockbackStrengthEnemy * groundMultiplier;
        float currentReach = Mathf.Lerp(minReach, maxReach, t);
        float currentRadius = Mathf.Lerp(minRadius, maxRadius, t);
        float currentShake = Mathf.Lerp(0.05f, 0.25f, t);
        float currentPitch = Mathf.Lerp(1.0f, 0.75f, t);
        float currentVolume = Mathf.Lerp(0.25f, 0.55f, t);

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.pitch = currentPitch;
        SoundManager.PlaySound(SoundType.spell1, audioSource, currentVolume);

        // Immediate blast shake feedback
        CameraSettings.Instance?.StartShake(Mathf.Lerp(0.1f, 0.25f, t), currentShake);

        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Ray ray = GetAimRay();
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, currentReach, ~0, QueryTriggerInteraction.Ignore);
        IHitable hitable = hasHit ? (hit.collider.GetComponentInParent<IHitable>() ?? hit.collider.GetComponentInChildren<IHitable>()) : null;

        if (hitable == null)
        {
            float closestDist = float.MaxValue;
            RaycastHit bestEnemyHit = default;
            IHitable bestHitable = null;
            foreach (var sHit in Physics.SphereCastAll(ray, currentRadius, currentReach, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = sHit.collider.GetComponentInParent<IHitable>() ?? sHit.collider.GetComponentInChildren<IHitable>();
                if (h != null && sHit.distance < closestDist)
                {
                    closestDist = sHit.distance;
                    bestEnemyHit = sHit;
                    bestHitable = h;
                }
            }
            if (bestHitable != null)
            {
                hit = bestEnemyHit;
                hitable = bestHitable;
                hasHit = true;
            }
            else if (!hasHit && Physics.SphereCast(ray, currentRadius, out hit, currentReach, ~0, QueryTriggerInteraction.Ignore))
            {
                hasHit = true;
            }
        }

        if (hasHit)
        {
            if (hitable != null)
            {
                CameraSettings.Instance?.TriggerHitFeedback();
                SoundManager.PlaySound(SoundType.magicianspells, audioSource, currentVolume);
                if (hitable is BouncingShell)
                {
                    hitable.Execute(transform, finalKnockbackStrengthEnemy);
                }
                else
                {
                    hitable.ExecuteWithUpwardBias(transform, finalKnockbackStrengthEnemy, upwardForce);
                }
                nextAllowedHitTime = Time.time + hitCooldown;
            }
            else if (hit.normal.y >= 0.5f && (hit.distance <= maxGroundReach || Vector3.Distance(transform.position, hit.point) <= maxGroundReach))
            {
                var playerMovement = GetComponent<InputSystem>();
                Vector3 launch = Vector3.ProjectOnPlane(-ray.direction, Vector3.up).normalized;
                launch.y = Mathf.Lerp(0.65f, 0.85f, t);

                float recovery = Mathf.Lerp(0.2f, 0.4f, t);
                if (playerMovement != null)
                    playerMovement.ApplyExternalImpulse(launch.normalized * finalKnockbackStrengthPlayer, recovery);
                else if (rb != null)
                    rb.AddForce(launch.normalized * finalKnockbackStrengthPlayer, ForceMode.VelocityChange);
                nextAllowedHitTime = Time.time + hitCooldown;
            }
        }

        StartCoroutine(ReleaseShockwave());
        // Reset everything
        ChargeNormalized = 0f;
        animator.SetBool("ShockWave", false);
        hasAutoReleased = false;
        maxChargeReachedTime = -1f;
    }
    private Ray GetAimRay()
    {
        // The world camera renders to a low-resolution texture. Screen pixels
        // (and browser CSS pixels) are not coordinates in that render target.
        return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
    private IEnumerator ReleaseShockwave()
    {
        shockwaveAnimationComplete = false;
        vfx.SetActive(true);
        yield return new WaitUntil(() => shockwaveAnimationComplete);
        vfx.SetActive(false);
        var src = GetComponent<AudioSource>();
        if (src != null) src.pitch = 1f;
    }
    public void ShockwaveAnimationComplete()
    {
        shockwaveAnimationComplete = true;
    }

    /// <summary>
    /// Abort a charge without firing. Used when the game freezes mid-charge
    /// (level-up card screen, pause menu): the mouse-release event is missed
    /// while frozen, which would otherwise leave the shockwave stuck charging.
    /// </summary>
    public void CancelCharge()
    {
        if (!isCharging && maxChargeReachedTime < 0f && !hasAutoReleased) return;

        isCharging = false;
        ChargeNormalized = 0f;
        maxChargeReachedTime = -1f;
        hasAutoReleased = false;

        var src = GetComponent<AudioSource>();
        if (src != null) src.pitch = 1f;

        if (screenShake != null && screenShake.IsShaking) screenShake.StopShake();
        if (animator != null) animator.SetBool("ShockWave", false);
        if (vfx != null && vfx.activeSelf) vfx.SetActive(false);
    }
    
    
}

