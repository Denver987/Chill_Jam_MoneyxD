using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnnemyMovement : MonoBehaviour
{

    public float updateSpeed = 1.0f; // Speed of the enemy movement

    public float moveSpeed = 1.0f;

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb; // Rigidbody component for physics interactions

    public Transform target; // Target to follow

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
    }

    private void FixedUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null) return;

        // Continuously update pathfinding destination
        navMeshAgent.SetDestination(target.position);

        float distance = Vector3.Distance(rb.position, target.position);

        // Stop if close enough
        if (distance <= navMeshAgent.stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // Get desired movement direction from NavMesh
        Vector3 direction = navMeshAgent.desiredVelocity;

        // Move Rigidbody instead of transform
        if (direction.magnitude > 0.1f)
        {
            rb.MovePosition(rb.position + direction.normalized * moveSpeed * Time.fixedDeltaTime);

            // Rotate smoothly towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f));
        }

        navMeshAgent.nextPosition = rb.position;

       
    }
}
