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
    private Vector3 launchDirection;
    private float launchSpeed;
    private PhysicsMaterial slidingMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        // Zero friction belongs to this rolling object, not the entire world.
        slidingMaterial = new PhysicsMaterial("Shell slide")
        {
            dynamicFriction = 0.02f, staticFriction = 0.02f,
            bounciness = 0.35f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.sharedMaterial = slidingMaterial;

    }
    private void ApplyFallMultiplier()
    {
        // Ground check
        LayerMask mask = groundLayer.value == 0 ? (LayerMask)(~0) : groundLayer;
        grounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, Mathf.Max(groundCheckDistance, 0.4f), mask, QueryTriggerInteraction.Ignore);

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

    public void StartWandering() => RestartWandering();

    public void RestartWandering()
    {
        isActive = false;
        activeTimer = 0f;
        wanderTimer = wanderChangeInterval;
        PickNewWanderDirection();
        rb.isKinematic = false;
        rb.useGravity = true;                          // make sure gravity is on
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

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

            var v = rb.linearVelocity;                     // keep current vertical velocity
            v.x = wanderDir.x * wanderSpeed;
            v.z = wanderDir.z * wanderSpeed;
            rb.linearVelocity = v;


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

            // Keep the promised launch speed rather than immediately losing it
            // to floor contact/damping. Gravity still controls the Y axis.
            var velocity = rb.linearVelocity;
            var planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (planar.sqrMagnitude > 0.01f) launchDirection = planar.normalized;
            rb.linearVelocity = new Vector3(launchDirection.x * launchSpeed, velocity.y, launchDirection.z * launchSpeed);

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

        Vector3 n = collision.contacts[0].normal;
        if (n.y > 0.5f) return; // A floor contact is not a wall bounce.

        Vector3 v = Vector3.Reflect(launchDirection, n);

        if (keepOnGround) v.y = 0f;
        v = v.sqrMagnitude > 0.0001f ? v.normalized : transform.forward;

        launchDirection = v;
        launchSpeed *= keepSpeedOnBounce;
        rb.linearVelocity = v * launchSpeed;

        // Damage things it hits
        var enemy = collision.gameObject.GetComponent<EnnemyMovement>() ?? collision.gameObject.GetComponentInParent<EnnemyMovement>();
        if (enemy != null)
        {
            CameraSettings.Instance?.TriggerHitFeedback();
            enemy.Die();
        }

        var player = collision.gameObject.GetComponent<Player>();
        if (player != null) player.TakeDamage(10, v, 5f);
    }

    private void Deactivate()
    {
        var bouncer = GetComponent<Bouncer>();
        if (bouncer != null && bouncer.Parent != null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            RestartWandering();
        }
    }
    // ===== IHitable =====
    public void Execute(Transform executionSource, float knockbackStrength)
    {
        if (executionSource == null) return;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        enabled = true;

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
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.WakeUp();
        launchDirection = direction.normalized;
        launchSpeed = Mathf.Clamp(strength * speedPerStrength, minLaunchSpeed, maxLaunchSpeed);
        rb.linearVelocity = launchDirection * launchSpeed;
    }


    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
        Execute(executionSource, knockbackStrength);
    }

    private void OnDestroy()
    {
        if (slidingMaterial != null) Destroy(slidingMaterial);
    }

   

}
