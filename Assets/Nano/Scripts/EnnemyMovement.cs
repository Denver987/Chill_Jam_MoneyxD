using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnnemyMovement : MonoBehaviour,IHitable
{

    [Header("Settings")]    
    public float moveSpeed = 1.0f;
    public float spreadRadius = 1.5f; // how much the enemies spread out
    public float separationRadius = 2.0f; // distance to keep from other enemies
    public float avoidanceForce = 2.0f;
    public float separationUpdateRate = 0.2f;
    public float knockbackRecoveryTime = 0.5f; // how long it takes to recover from knockback
    // recalc separation every 0.2s// how strong separation pushes them

    private SkinnedMeshRenderer[] meshRenderers;


    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float attackKnockbackForce = 5f;
    public int attackDamage = 10;

    private float lastAttackTime = 0f;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    public EnemySpawner spawner; // reference to the spawner for accessing all enemies
    public Transform target;
    private Vector3 personalOffset;


    public GameObject dustExplosion; // reference to the dust explosion prefab
    private Vector3 cachedSeparation = Vector3.zero;
    private float separationTimer = 0f;

    private const string isMoving = "IsMoving";

    private Animator animator;
    private Enemy enemy;
    private bool isKnockedBack = false;
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
       spawner = GetComponentInParent<EnemySpawner>();
        if(spawner == null)
        {
            Debug.LogError("EnemySpawner not found in parent hierarchy!");
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
        }else if(collision.gameObject.CompareTag("Obstacle"))
        {
            if (isKnockedBack)
            {
                Die();
            }
        }   
    }

    public void Attack()
    {
        
       //handle attacking 
    }

    public  void Die()
    {
      
        this.transform.GetChild(0).gameObject.SetActive(true); // show the dust explosion
        this.transform.GetChild(1).gameObject.SetActive(false); // hide the enemy model
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
        if (isKnockedBack)
        {
            animator.SetBool(isMoving, false);
            return;
        }

        // Desired position (player + some offset)
        Vector3 destination = target.position + personalOffset;
        navMeshAgent.SetDestination(destination);

        // Base direction from NavMeshAgent
        Vector3 direction = navMeshAgent.desiredVelocity;

        // Add separation from nearby enemies
        separationTimer -= Time.fixedDeltaTime;
        if (separationTimer <= 0f)
        {
            cachedSeparation = ComputeSeparation();
            separationTimer = separationUpdateRate;
        }

        // Add cached separation force
        direction += cachedSeparation * avoidanceForce;


        // Stop if very close to player
        float distance = Vector3.Distance(rb.position, target.position);
        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            TryAttack();
            return;
        }
        if (distance <= navMeshAgent.stoppingDistance + 0.5f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // Apply movement
        if (direction.magnitude > 0.1f)
        {
            animator.SetBool(isMoving, true);
            rb.MovePosition(rb.position + direction.normalized * moveSpeed * Time.fixedDeltaTime);

            //Quaternion targetRotation = Quaternion.LookRotation(direction);
            //rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f));
        }else
        {
            animator.SetBool(isMoving, false);
        }

        navMeshAgent.nextPosition = rb.position;
    }

    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            // Apply knockback to player if they have Rigidbody
            Rigidbody playerRb = target.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 knockDir = (target.position - transform.position).normalized;
                playerRb.AddForce(knockDir * attackKnockbackForce, ForceMode.Impulse);
                Debug.Log($"{gameObject.name} attacked player!");
            }
            else
            {
                Debug.LogWarning("Target does not have a Rigidbody for knockback!");
            }

            

           
        }
    }
    IEnumerator LockInHitFrame(Animator animator, string stateName, float normalizedTime, float duration)
    {
        animator.Play(stateName, 0, normalizedTime);
        animator.Update(0f);   // force immediate pose update
        animator.speed = 0f;   // freeze here

        yield return new WaitForSeconds(duration);

        animator.speed = 1f;   // resume
    }

    IEnumerator FlashColor(SkinnedMeshRenderer[] renderers, Color flashColor, float duration)
    {
        if (renderers == null || renderers.Length == 0) yield break;

        // Save original colors
        List<Color> originalColors = new List<Color>();
        foreach (var r in renderers)
        {
            if (r != null)
                originalColors.Add(r.material.color);
        }

        // Apply flash color
        foreach (var r in renderers)
        {
            if (r != null)
                r.material.color = flashColor;
        }

        yield return new WaitForSeconds(duration);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }

    IEnumerator FlashColorLerp(SkinnedMeshRenderer[] renderers, Color startColor, Color endColor, float duration)
    {
        if (renderers == null || renderers.Length == 0) yield break;

        // Store original colors so we can restore at the end
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                originalColors[i] = renderers[i].material.color;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Ping-pong for back-and-forth flash (0 → 1 → 0)
            float lerpT = Mathf.PingPong(t * 2f, 1f);

            // Lerp between start and end colors
            Color currentColor = Color.Lerp(startColor, endColor, lerpT);

            foreach (var r in renderers)
            {
                if (r != null)
                    r.material.color = currentColor;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }

    IEnumerator HitPause(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }
    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5F)
    {
        KnockBackEntityUpward(executionSource, knockbackStrength, upwardBias);
    }
    public void Execute(Transform executionSource, float knockbackStrength)
    {
        KnockBackEntity(executionSource, knockbackStrength);
    }
    IEnumerator SmoothKnockback(Rigidbody rb, Vector3 direction, float distance, float duration)
    {
        Vector3 startPos = rb.position;
        Vector3 targetPos = startPos + direction.normalized * distance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = 1 - Mathf.Pow(1 - t, 3); // cubic ease-out

            rb.MovePosition(Vector3.Lerp(startPos, targetPos, easedT));

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
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
        StartCoroutine(FlashColorLerp(meshRenderers, Color.red, Color.white, 0.3f));
        StartCoroutine(LockInHitFrame(animator, "Hit", 0.4f, 0.2f));
        StartCoroutine(HitPause(0.2f));
        Vector3 direction = (transform.position - executionSource.position).normalized;
        Vector3 upwardForce = Vector3.up * upwardBias * knockbackStrength;
        rb.AddForce(direction * knockbackStrength + upwardForce, ForceMode.Impulse);
        // Enter knockback state
        isKnockedBack = true;
        knockbackTimer = knockbackRecoveryTime;
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
