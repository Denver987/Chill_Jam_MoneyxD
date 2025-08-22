using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SmallHedge.SoundManager;
using UnityEngine;
using UnityEngine.AI;

public class EnnemyMovement : MonoBehaviour,IHitable
{

    [Header("Settings")]    
    public float minSpeed = 0.5f; // minimum speed to avoid being stuck
    public float maxSpeed = 2.0f; // maximum speed for the enemy
    public float moveSpeed = 1.0f;
    public float spreadRadius = 1.5f; // how much the enemies spread out
    public float separationRadius = 2.0f; // distance to keep from other enemies
    public float avoidanceForce = 2.0f;
    public float separationUpdateRate = 0.2f;
    public float knockbackRecoveryTime = 0.5f; // how long it takes to recover from knockback
    // recalc separation every 0.2s// how strong separation pushes them

    public float minRotationSpeed = 1f; // minimum rotation speed
    public float maxRotationSpeed = 10f; // maximum rotation speed
    public float rotationSpeed = 5f; // how fast they rotate to face the player

    public float surroundDistance = 2.0f; // how far they try to stay from the player
    public float ennemyId = 0f; // unique ID for the enemy, can be used for debugging or tracking
    private SkinnedMeshRenderer[] meshRenderers;


    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float attackKnockbackForce = 5f;
    public int attackDamage = 10;

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    public EnemySpawner spawner; // reference to the spawner for accessing all enemies
    public Transform target;
    private Vector3 personalOffset;


    public GameObject dustExplosion; // reference to the dust explosion prefab
    private Vector3 cachedSeparation = Vector3.zero;
    private float separationTimer = 0f;

    private const string isMoving = "IsMoving";
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float riseGravityMultiplier = 1.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    private bool grounded;
    public bool isDead;
    private Animator animator;
    private Enemy enemy;
    public bool isKnockedBack = false;
    private bool isAttacking = false;
    public bool canattack = true; // can attack after cooldown
    private float knockbackTimer = 0f;
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        navMeshAgent.updatePosition = false;

        // give each enemy a random offset around the player
        personalOffset = Random.insideUnitSphere * spreadRadius;
        personalOffset.y = 0; // keep it flat on the ground
    }

    private void Start()
    {
        ennemyId = Random.Range(0f, 180f); 
        surroundDistance = Random.Range(0f,2f); // random surround distance between 1.5 and 2.5

        // assign a random ID for debugging
        spawner = GetComponentInParent<EnemySpawner>();
        if(spawner == null)
        {
           
            return;
        }
    }

    private void FixedUpdate()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                RecoverFromKnockback();
            }
            return; // Skip AI movement while knocked back
        }

        FollowTarget();
        GroundCheck();

    }

    private void OnCollisionEnter(Collision collision)
    {
        EnnemyMovement otherEnemy = collision.gameObject.GetComponent<EnnemyMovement>();
        if(otherEnemy != null)
        {
            if (isKnockedBack)
            {

                Die();
                otherEnemy.Die(); // if knocked back, both die
            }
        }else if (collision.gameObject.CompareTag("Obstacle"))
        {
           
            Debug.Log("Hit wall");
            if (isKnockedBack)
            {
                Die();
            }
        }
    }
    public  void Die()
    {
        isDead = true;
        canattack = false;
        this.transform.GetChild(0).gameObject.SetActive(true); // show the dust explosion
        this.transform.GetChild(2).gameObject.SetActive(false);
        
        //CameraSettings.Instance.StartZoom(55f, 1f);
        SoundManager.PlaySound(SoundType.enemyDeath, GetComponent<AudioSource>(), 0.5f); // Play death sound with volume 0.5

        CameraSettings.Instance.StartShake(0.2f, 0.05f);
        CameraSettings.Instance.StartSlowMotion(0.5f, 0.5f);
       
       
        // hide the enemy model
        StartCoroutine(DieAfterDelay());
        
    }
    public IEnumerator DieAfterDelay()
    {
       
        yield return new WaitForSeconds(1f);

        this.transform.GetChild(0).gameObject.SetActive(false); // hide the dust explosion
        gameObject.SetActive(false);
        spawner.RemoveEnemy(enemy);
    }
    private void FollowTarget()
    {
        if (target == null) return;
        if (isKnockedBack || isAttacking ||isDead)
        {
            animator.SetBool(isMoving, false);
            return;
        }

        // Desired position (player + offset)
        float angle = (ennemyId * 137) % 360; // "spread" enemies in a circle using golden angle
        Vector3 offsetDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

        Vector3 destination = target.position + offsetDir * surroundDistance;
        navMeshAgent.SetDestination(destination);

        Vector3 direction = navMeshAgent.desiredVelocity;

        // Add separation force
        separationTimer -= Time.fixedDeltaTime;
        if (separationTimer <= 0f)
        {
            cachedSeparation = ComputeSeparation();
            separationTimer = separationUpdateRate;
        }
        direction += cachedSeparation * avoidanceForce;

        float distance = Vector3.Distance(rb.position, target.position);
        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector3.zero;
            TryAttack();
            return;
        }

        if (direction.magnitude > 0.1f)
        {
            animator.SetBool(isMoving, true);

            // --- Slope handling ---
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            {
                Vector3 slopeNormal = hit.normal;

                // Align movement to slope
                Vector3 slopeDir = Vector3.ProjectOnPlane(direction, slopeNormal).normalized;

                // Apply velocity instead of MovePosition
                Vector3 move = slopeDir * moveSpeed;
                rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

                // Add a "stick to ground" force so they don’t hover
                rb.AddForce(Vector3.down * 30f, ForceMode.Acceleration);

                // Face direction of movement
                if (slopeDir.sqrMagnitude > 0.01f)
                {
                    // Add some "inaccuracy" so they don’t all rotate the same
                    Vector3 lookDir = slopeDir + new Vector3(
                        Random.Range(-0.05f, 0.05f),
                        0f,
                        Random.Range(-0.05f, 0.05f)
                    );

                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
                }

            }
        }
        else
        {
            animator.SetBool(isMoving, false);
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // keep gravity
        }

        navMeshAgent.nextPosition = rb.position;
    }



    private void TryAttack()
    {
        if (isKnockedBack || isAttacking || !canattack) return;

        // Check if player is still in range before starting
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange) return;

        // Start attack
        StartCoroutine(AttackRoutine());
    }
    private void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);

        if (!grounded)
        {
            if (rb.linearVelocity.y < 0) // falling
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (fallMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
            else if (rb.linearVelocity.y > 0) // rising (knockback / jump)
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (riseGravityMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        // Wait half of the attack animation before applying damage (adjust to match your animation)
        float animationDelay = 0.3f;
        float elapsed = 0f;

        while (elapsed < animationDelay)
        {
            // If player moves out of range, cancel attack
            if (Vector3.Distance(transform.position, target.position) > attackRange)
            {
                isAttacking = false;
                animator.ResetTrigger("Attack"); // stop attack animation
                yield break; // cancel coroutine
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Apply damage if still in range
        if (Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            Vector3 attackDirection = (target.position - transform.position).normalized;
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, attackDirection, attackKnockbackForce);
                Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
            }
        }

        // Cooldown
        yield return new WaitForSeconds(attackCooldown / 2f);
        isAttacking = false;
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

    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5F)
    {
        KnockBackEntityUpward(executionSource, knockbackStrength, upwardBias);
    }
    public void Execute(Transform executionSource, float knockbackStrength)
    {
        KnockBackEntity(executionSource, knockbackStrength);
    }
    private void KnockBackEntity(Transform executionSource, float knockbackStrength)
    {
        if (executionSource == null || rb == null) return;

        Vector3 direction = (transform.position - executionSource.position).normalized;
        rb.AddForce(direction * knockbackStrength, ForceMode.Impulse);

        // Enter knockback state
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;
    }
    


    private void KnockBackEntityUpward(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
        if (executionSource == null || rb == null) return;

        
        Vector3 direction = (transform.position - executionSource.position).normalized;
        direction.y = 0f;
        Vector3 knockDir = direction + Vector3.up * upwardBias;

        // Enter knockback state
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime; // <-- keep your recovery timer

        // Rigidbody impulse
        rb.AddForce(knockDir * knockbackStrength, ForceMode.Impulse);

        // DOTween knockback for smooth visual movement
        Vector3 targetPos = transform.position + knockDir * (knockbackStrength * 0.1f);
        transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad);

        // Visual feedback
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        foreach (var r in meshRenderers)
            r.material.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo);
        animator.SetTrigger("Hit");

        // Camera shake
        CameraSettings.Instance?.StartShake(0.15f, 0.05f);

        // Hit pause
        StartCoroutine(HitPause(0.08f));
        SoundManager.PlaySound(SoundType.ennemyDamage, GetComponent<AudioSource>(), 1f); // Play hit sound with volume 0.5
    }


    private void RecoverFromKnockback()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isKnockedBack = false;
    }
    // --- Steering Behavior: Separation ---
    private Vector3 ComputeSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        int neighborCount = 0;

        // Find all enemies in the scene (you can optimize this with a manager/pooling)
        List<Enemy> allEnemies = spawner.ActiveEnemies; // Assuming this is a list of all active enemies

        foreach (var other in allEnemies)
        {
            if (other == this) continue; // skip self

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < separationRadius && dist > 0.01f)
            {
                // Push away from neighbor
                Vector3 away = (transform.position - other.transform.position).normalized;
                separationForce += away / dist; // closer enemies push harder
                neighborCount++;
            }
        }

        if (neighborCount > 0)
            separationForce /= neighborCount;

        return separationForce;
    }

    
}
