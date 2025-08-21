using UnityEngine;
using DG.Tweening; // ✅ for DOTween

public class BouncingShell : MonoBehaviour, IHitable
{
    public float wanderSpeed = 2f;   // speed while wandering
    public float moveSpeed = 6f;     // speed while bouncing
    public float lifeTime = 10f;
    public float wanderChangeInterval = 2f; // how often to pick a new random direction
    public float knockbackRecoveryTime = 0.5f;

    private Animator animator;
    private Rigidbody rb;
    private bool isActive = false;   // bouncing mode
    private Vector3 wanderDir;
    private float wanderTimer;
    private bool isMoving = false;

    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartWandering();
    }

    private void StartWandering()
    {
        isActive = false;
        wanderTimer = wanderChangeInterval;
        PickNewWanderDirection();
    }

    private void PickNewWanderDirection()
    {
        // random horizontal direction
        wanderDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
    }

    private void Update()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f) RecoverFromKnockback();
            return;
        }

        if (!isActive)
        {
            animator.SetBool("IsMoving", !isActive);

            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                PickNewWanderDirection();
                wanderTimer = wanderChangeInterval;
            }

            rb.linearVelocity = wanderDir * wanderSpeed;
        }
    }

    public void Activate(Vector3 knockbackDir)
    {
        animator.SetBool("IsKnocking",true);
        isActive = true;
        rb.linearVelocity = knockbackDir.normalized * moveSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isActive) return;

        // Reflect bounce
        Vector3 reflectDir = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
        rb.linearVelocity = reflectDir * moveSpeed;

        // Hit enemy
        var enemy = collision.gameObject.GetComponent<EnnemyMovement>();
        if (enemy != null)
            enemy.Die();

        // Hit player
        var player = collision.gameObject.GetComponent<Player>();
        if (player != null)
            player.TakeDamage(10, reflectDir, 5f);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    // --- IHitable Implementation ---
    public void Execute(Transform executionSource, float knockbackStrength)
    {
        KnockBackEntity(executionSource, knockbackStrength);
    }

    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
        KnockBackEntityUpward(executionSource, knockbackStrength, upwardBias);
    }

    private void KnockBackEntity(Transform executionSource, float knockbackStrength)
    {
        if (executionSource == null || rb == null) return;

        Vector3 direction = (transform.position - executionSource.position).normalized;
        direction.y = 0f;

        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;

        rb.AddForce(direction * knockbackStrength, ForceMode.Impulse);

        // DOTween knockback
        Vector3 targetPos = transform.position + direction * (knockbackStrength * 0.1f);
        transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad);

        // Visual feedback (scale punch)
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        animator?.SetTrigger("Hit");

        // Camera shake
        CameraSettings.Instance?.StartShake(0.15f, 0.05f);
    }

    private void KnockBackEntityUpward(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
        if (executionSource == null || rb == null) return;

        Vector3 direction = (transform.position - executionSource.position).normalized;
        direction.y = 0f;
        Vector3 knockDir = direction + Vector3.up * upwardBias;

        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;

        rb.AddForce(knockDir * knockbackStrength, ForceMode.Impulse);

        Vector3 targetPos = transform.position + knockDir * (knockbackStrength * 0.1f);
        transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad);

        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        animator?.SetTrigger("Hit");

        CameraSettings.Instance?.StartShake(0.15f, 0.05f);
    }

    private void RecoverFromKnockback()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isKnockedBack = false;
    }
}
