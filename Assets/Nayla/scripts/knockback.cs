using SmallHedge.SoundManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class Knockback : MonoBehaviour
{
    public Camera cam;

    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float initialKnockbackStrength = 10f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float maxKnockbackStrength = 20f;
    [SerializeField] private float groundMultiplier = 2f;
    [SerializeField] private AnimationCurve chargeCurve;

    [Header("Screen Shake")]
    [SerializeField] private KnockbackScreenshake screenShake;

    public float maxDistance = 100f; // maximum distance to hit something   
    public float upwardForce = 0.5f; // how much upward force to apply when hitting the ground
    private float nextAllowedHitTime = 0f;
    private float chargeStartTime = -1f;
    private bool isCharging = false;
    private Rigidbody rb;
    public Animator animator;
    public float ChargeNormalized { get; private set; } = 0f;

    private float maxChargeReachedTime = -1f;
    private bool hasAutoReleased = false;

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
        
        SoundManager.PlaySound(SoundType.spell1, this.GetComponent<AudioSource>(),0.25f);
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
                    
                    hitable.Execute(hit.transform, finalKnockbackStrengthEnemy);
                    nextAllowedHitTime = Time.time + hitCooldown;

                }
                else
                {
                    hitable.ExecuteWithUpwardBias(transform, finalKnockbackStrengthEnemy, upwardForce);
                    nextAllowedHitTime = Time.time + hitCooldown;
                }
               
                

              
            }
            else
            {
                if (hit.normal.y >= 0.95f)
                {
                    if (rb != null)
                    {
                        rb.AddForce(Vector3.up * finalKnockbackStrengthPlayer, ForceMode.VelocityChange);
                        nextAllowedHitTime = Time.time + hitCooldown;
                    }
                }
            }
        }

        // Reset everything
        ChargeNormalized = 0f;
        animator.SetBool("ShockWave", false);
        hasAutoReleased = false;
        maxChargeReachedTime = -1f;
    }
    
    
}

