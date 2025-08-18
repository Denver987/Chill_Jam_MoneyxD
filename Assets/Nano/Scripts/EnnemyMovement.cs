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

    private Enemy enemy;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemy = GetComponent<Enemy>();

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;

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
        this.gameObject.GetComponent<MeshRenderer>().enabled = false; // hide the enemy mesh
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
            rb.MovePosition(rb.position + direction.normalized * moveSpeed * Time.fixedDeltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f));
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
