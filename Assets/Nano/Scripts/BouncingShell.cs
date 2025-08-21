using DG.Tweening;
using SmallHedge.SoundManager;
using System.Collections;
using UnityEngine;

public class BouncingShell : MonoBehaviour, IHitable
{
    [Header("Wander")]
    public float wanderSpeed = 2f;
    public float wanderChangeInterval = 2f;


    [Header("Physics")]
    [SerializeField] private float fallMultiplier = 2f;       // faster falling
    [SerializeField] private float riseGravityMultiplier = 1.5f; // optional for upward knockback
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;


    private SkinnedMeshRenderer[] meshRenderers;

    private bool grounded;
    [Header("Bounce / Launch")]
    public float minLaunchSpeed = 8f;       // baseline speed when launched
    public float maxLaunchSpeed = 20f;      // clamp so it doesn't get insane
    public float speedPerStrength = 0.6f;   // converts knockbackStrength into speed
    [Range(0.7f, 1.0f)] public float keepSpeedOnBounce = 1f; // 1 keeps speed, <1 dampens
    public bool keepOnGround = true;        // flatten Y so it slides along ground

    [Header("Active life")]
    public float activeLifeTime = 5f;

    private Animator animator;
    private Rigidbody rb;
    private bool isActive = false;  // bouncing mode
    private Vector3 wanderDir;
    private float wanderTimer;
    private float activeTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

    }
    private void ApplyFallMultiplier()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);

        if (!grounded)
        {
            if (rb.linearVelocity.y < 0) // falling
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (fallMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
            else if (rb.linearVelocity.y > 0) // rising
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (riseGravityMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
        }
    }

    private void OnEnable()
    {
        RestartWandering();
    }

    public void RestartWandering()
    {
        isActive = false;
        activeTimer = 0f;
        wanderTimer = wanderChangeInterval;
        PickNewWanderDirection();
        rb.linearVelocity = Vector3.zero;
    }

    private void PickNewWanderDirection()
    {
        wanderDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        if (keepOnGround) wanderDir.y = 0f;
        if (wanderDir.sqrMagnitude < 0.001f) wanderDir = transform.forward;
        wanderDir.Normalize();
    }

    private void FixedUpdate()
    {
        animator.SetBool("IsMoving", !isActive);
        if (!isActive)
        {
            // --- WANDER MODE ---
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                PickNewWanderDirection();
                wanderTimer = wanderChangeInterval;
            }

            rb.linearVelocity = wanderDir * wanderSpeed;

            // Rotate to velocity
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                var look = rb.linearVelocity; if (keepOnGround) look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }
        }
        else
        {
            // --- ACTIVE BOUNCE MODE ---
            activeTimer += Time.deltaTime;
            if (activeTimer >= activeLifeTime)
            {
                Deactivate();
                return;
            }

            // Keep facing travel direction while active
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                var look = rb.linearVelocity; if (keepOnGround) look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
                }
            }

           
        }
        ApplyFallMultiplier();
    }

    // ===== Bounce handling =====
    private void OnCollisionEnter(Collision collision)
    {
        if (!isActive) return;

        float speed = rb.linearVelocity.magnitude;
        Vector3 n = collision.contacts[0].normal;
        Vector3 v = Vector3.Reflect(rb.linearVelocity, n);

        if (keepOnGround) v.y = 0f;
        v = v.sqrMagnitude > 0.0001f ? v.normalized : transform.forward;

        rb.linearVelocity = v * (speed * keepSpeedOnBounce);

        // Damage things it hits
        var enemy = collision.gameObject.GetComponent<EnnemyMovement>();
        if (enemy != null) enemy.Die();

        var player = collision.gameObject.GetComponent<Player>();
        if (player != null) player.TakeDamage(10, v, 5f);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
    private bool isHitPaused = false;
    IEnumerator HitPause(float duration)
    {
        if (isHitPaused) yield break;

        isHitPaused = true;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;

        isHitPaused = false;
    }
    // ===== IHitable =====
    public void Execute(Transform executionSource, float knockbackStrength)
    {
        if (executionSource == null || rb == null) return;

        // Direction AWAY from the attacker (hit source) → attacker -> shell
        Vector3 dir = transform.position - executionSource.position;

        // If you want strictly ground slide, project onto ground plane
        if (keepOnGround)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up);
        }
        else
        {
            // Prevent pushing downward; add a tiny pop so it doesn't grind into the ground
            if (dir.y <= 0f) dir.y = 0.15f;
        }

        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        // (Feedback kept as-is)
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        foreach (var r in meshRenderers)
            r.material.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo);
        CameraSettings.Instance?.StartShake(0.15f, 0.05f);
        StartCoroutine(HitPause(0.08f));
        SoundManager.PlaySound(SoundType.ennemyDamage, GetComponent<AudioSource>(), 1f);

        // Apply the knockback along that hit direction
        KnockBackShell(dir, knockbackStrength);
    }



    private void KnockBackShell(Vector3 direction, float strength)
    {
        // Enter bounce state
        isActive = true;
        animator.SetBool("IsKnocking", true);
        activeTimer = 0f;

        // Stop any DOTween position conflicts
        DOTween.Kill(transform);

        // Enable physics
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (keepOnGround) direction.y = 0f;   // enforce ground slide if wanted
        rb.linearVelocity = direction.normalized * strength;

        // Optional: small DOTween punch for visuals (doesn’t affect physics)
        Vector3 targetPos = transform.position + direction * (strength * 0.1f);
        transform.DOMove(targetPos, 0.15f).SetEase(Ease.OutQuad);
    }


    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
      
    }

   

}
