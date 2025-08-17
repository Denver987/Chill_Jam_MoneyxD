using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnnemyMovement : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float spreadRadius = 1.5f; // how much the enemies spread out
    public float separationRadius = 2.0f; // distance to keep from other enemies
    public float avoidanceForce = 2.0f;
    public float separationUpdateRate = 0.2f; // recalc separation every 0.2s// how strong separation pushes them

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    public EnemySpawner spawner; // reference to the spawner for accessing all enemies
    public Transform target;
    private Vector3 personalOffset;

    private Vector3 cachedSeparation = Vector3.zero;
    private float separationTimer = 0f;
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

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
        FollowTarget();
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
