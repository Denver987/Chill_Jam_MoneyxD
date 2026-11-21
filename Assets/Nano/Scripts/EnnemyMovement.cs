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

    public TutorialSpawner tutorialSpawner; // reference to the tutorial spawner if in tutorial mode

    public bool isGolem = false; // is this enemy a golem?


    public int scoreAdder = 20;
    public float HealthPotionDropChance = 0.3f; // chance to drop a health potion on death  



    [Header("Attack Settings")]
    public float attackRange = 1.8f;
    public float attackCooldown = 1.0f;
    public float attackKnockbackForce = 4f;
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
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float riseGravityMultiplier = 1.25f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    private bool grounded;
    public bool isDead;
    private Animator animator;
    private Enemy enemy;
    public bool isKnockedBack = false;
    private bool isAttacking = false;
    public bool isSpellSuspended;
    private Coroutine attackRoutine;
    private float nextAttackTime;
    public bool canattack = true; // can attack after cooldown
    private float knockbackTimer = 0f;
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        if (navMeshAgent != null)
        {
            navMeshAgent.updatePosition = false;
        }
        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        if (groundLayer.value == 0)
        {
            groundLayer = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << gameObject.layer));
        }

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
        if (isSpellSuspended || isDead) return;
        GroundCheck();
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

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            Die();
            return;
        }

        if (isGolem) return; // Golems don't take damage from regular enemy/wall collisions
        EnnemyMovement otherEnemy = collision.gameObject.GetComponent<EnnemyMovement>();
        if (otherEnemy != null)
        {
            if (isKnockedBack)
            {

                Die();
                if (otherEnemy.isGolem) return; // Golems don't die from collisions
                otherEnemy.Die(); // if knocked back, both die
            }
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {

            Debug.Log("Hit wall");
            if (isKnockedBack)
            {
                Die();
            }
        }
        else if (collision.gameObject.CompareTag("Water")) {

            Die();
        }
    }
    public  void Die()
    {
        if (isDead) return;

        isDead = true;
        canattack = false;
        isAttacking = false;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }
        
        this.transform.GetChild(0).gameObject.SetActive(true); // show the dust explosion
        this.transform.GetChild(2).gameObject.SetActive(false);
        
        //CameraSettings.Instance.StartZoom(55f, 1f);
        SoundManager.PlaySound(SoundType.enemyDeath, GetComponent<AudioSource>(), 0.5f); // Play death sound with volume 0.5

        if (CameraSettings.Instance != null)
        {
            CameraSettings.Instance.StartShake(0.2f, 0.05f);
        }
       
       
        // hide the enemy model
        StartCoroutine(DieAfterDelay());
        
    }
    public IEnumerator DieAfterDelay()
    {
       
        yield return new WaitForSeconds(1f);

        this.transform.GetChild(0).gameObject.SetActive(false);

        // Start() may not have run yet if the enemy died during its spawn
        // effect, so resolve the spawner lazily here as well.
        if (spawner == null) spawner = GetComponentInParent<EnemySpawner>();

        if(GameManager.Instance != null && GameManager.Instance.isTutorial ==false && spawner != null)
        {
            int orbType = isGolem ? 3 : Random.Range(0, 4);
            int orbCount = isGolem ? 3 : 1;
            spawner.SpawnXpOrbs(transform.position, orbCount, orbType); // spawn XP orbs
            spawner.TrySpawnHealthPotion(transform.position + Vector3.up, HealthPotionDropChance);
            GameManager.Instance.AddScore(scoreAdder);
        }
        gameObject.SetActive(false);
        if (spawner != null) spawner.RemoveEnemy(enemy);
        if(GameManager.Instance != null && GameManager.Instance.isTutorial && tutorialSpawner != null)
        {
            tutorialSpawner.SpawnEnnemy();
        }
    }
    private void FollowTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.isTutorial)
            return;
        if (target == null) return;
        if (isKnockedBack ||isDead)
        {
            animator.SetBool(isMoving, false);
            return;
        }

        // Desired position (player + offset)
        float angle = (ennemyId * 137) % 360; // "spread" enemies in a circle using golden angle
        Vector3 offsetDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

        Vector3 destination = target.position + offsetDir * surroundDistance;

        Vector3 direction = Vector3.zero;
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(destination);
            if (navMeshAgent.hasPath && navMeshAgent.desiredVelocity.sqrMagnitude > 0.04f)
            {
                direction = navMeshAgent.desiredVelocity;
            }
        }

        if (direction.sqrMagnitude <= 0.04f)
        {
            Vector3 direct = destination - rb.position;
            direct.y = 0f;
            if (direct.sqrMagnitude > 0.01f) direction = direct.normalized * moveSpeed;
        }

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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            animator.SetBool(isMoving, false);
            TryAttack();
            return;
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            animator.SetBool(isMoving, true);

            int mask = groundLayer.value == 0 ? ~0 : groundLayer.value;
            mask &= ~(1 << gameObject.layer);

            // --- Slope handling ---
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2.5f, mask, QueryTriggerInteraction.Ignore))
            {
                Vector3 slopeNormal = hit.normal;

                // Align movement to slope
                Vector3 slopeDir = Vector3.ProjectOnPlane(direction, slopeNormal).normalized;

                // Apply velocity including upward slope climbing
                Vector3 move = slopeDir * moveSpeed;
                float yVel = (slopeDir.y > 0.02f) ? move.y : Mathf.Min(rb.linearVelocity.y, 0f);
                rb.linearVelocity = new Vector3(move.x, yVel, move.z);

                // Gentle ground-stick downforce only when not actively climbing uphill
                if (slopeDir.y <= 0.05f)
                {
                    rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);
                }

                // Face direction of movement
                Vector3 lookDir = new Vector3(slopeDir.x, 0f, slopeDir.z);
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
                }
            }
            else
            {
                // Airborne or ledge fallback: maintain horizontal movement towards destination
                Vector3 dirNorm = direction.normalized;
                rb.linearVelocity = new Vector3(dirNorm.x * moveSpeed, rb.linearVelocity.y, dirNorm.z * moveSpeed);

                Vector3 lookDir = new Vector3(dirNorm.x, 0f, dirNorm.z);
                if (lookDir.sqrMagnitude > 0.01f)
                {
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

        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.nextPosition = rb.position;
        }
    }



    private void TryAttack()
    {
        if (GameManager.Instance != null && GameManager.Instance.isTutorial)
            return;
        if (isKnockedBack || isAttacking || isSpellSuspended || isDead || !canattack || Time.time < nextAttackTime) return;

        // Check if player is still in range before starting
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange) return;

        // Start attack
        attackRoutine = StartCoroutine(AttackRoutine());
    }
    private void GroundCheck()
    {
        int mask = groundLayer.value == 0 ? ~0 : groundLayer.value;
        mask &= ~(1 << gameObject.layer);
        grounded = Physics.SphereCast(transform.position + Vector3.up * 0.3f, 0.25f, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.3f, mask, QueryTriggerInteraction.Ignore);

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
        nextAttackTime = Time.time + attackCooldown;
        animator.SetTrigger("Attack");

        // Time damage to land near the end of the swing (~65-70% of animation delay)
        float animationDuration = isGolem ? 0.9f : 0.6f;
        float strikeDelay = animationDuration * 0.68f;
        float elapsed = 0f;

        while (elapsed < strikeDelay)
        {
            if (target == null || isKnockedBack || isSpellSuspended || isDead)
            {
                isAttacking = false;
                if (animator != null) animator.ResetTrigger("Attack");
                attackRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // At the EXACT instant damage is applied, re-check distance; if player backed away/dodged, attack misses
        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange + 0.3f)
        {
            Vector3 attackDirection = (target.position - transform.position).normalized;
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(attackDamage, attackDirection, attackKnockbackForce);
            }
        }

        // Complete remaining swing animation before returning to idle/walk
        float remainingAnimation = Mathf.Max(0f, animationDuration - strikeDelay);
        float elapsedRecovery = 0f;
        while (elapsedRecovery < remainingAnimation)
        {
            if (isKnockedBack || isSpellSuspended || isDead)
            {
                isAttacking = false;
                attackRoutine = null;
                yield break;
            }
            elapsedRecovery += Time.deltaTime;
            yield return null;
        }

        // Wait for remaining cooldown if any
        float remainingCooldown = nextAttackTime - Time.time;
        if (remainingCooldown > 0f)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        isAttacking = false;
        attackRoutine = null;
    }

    private void ApplyHitFlinch(float flinchDuration = 0.2f)
    {
        // Interrupt attacks and add brief recovery grace period so attacks are responsive to player hits
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        isAttacking = false;
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
        }

        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + flinchDuration);
        isKnockedBack = true;
        knockbackTimer = Mathf.Max(knockbackTimer, flinchDuration);
    }

    public void SetSpellSuspended(bool suspended)
    {
        if (isSpellSuspended == suspended) return;
        isSpellSuspended = suspended;
        if (suspended)
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            isAttacking = false;
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
                animator.SetBool(isMoving, false);
            }
        }
        else if (!isDead)
        {
            ApplyHitFlinch(0.2f);
        }
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
        float duration = Mathf.Lerp(0.35f, 0.65f, Mathf.InverseLerp(3f, 15f, knockbackStrength));
        ApplyHitFlinch(duration);

        rb.linearVelocity = direction * knockbackStrength;
    }
    


    private void KnockBackEntityUpward(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f)
    {
        if (executionSource == null || rb == null) return;

        Vector3 direction = (transform.position - executionSource.position).normalized;
        direction.y = 0f;
        Vector3 knockDir = (direction.normalized + Vector3.up * upwardBias).normalized;

        float duration = Mathf.Lerp(0.35f, 0.65f, Mathf.InverseLerp(3f, 15f, knockbackStrength));
        ApplyHitFlinch(duration);

        // Rigidbody velocity
        rb.linearVelocity = knockDir * knockbackStrength;

        // Visual feedback
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        if (meshRenderers != null)
        {
            foreach (var r in meshRenderers)
            {
                if (r != null && r.material != null)
                    r.material.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo);
            }
        }
        if (animator != null) animator.SetTrigger("Hit");

        // Camera shake
        CameraSettings.Instance?.StartShake(0.15f, 0.05f);

        SoundManager.PlaySound(SoundType.ennemyDamage, GetComponent<AudioSource>(), 1f);
    }


    private void RecoverFromKnockback()
    {
        rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
        rb.angularVelocity = Vector3.zero;
        isKnockedBack = false;
    }
    // --- Steering Behavior: Separation ---
    private Vector3 ComputeSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        int neighborCount = 0;

        // Find all enemies in the scene (you can optimize this with a manager/pooling)
        if (spawner == null || spawner.ActiveEnemies == null) return Vector3.zero;
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
