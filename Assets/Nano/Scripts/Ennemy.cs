using UnityEngine;
using UnityEngine.AI;

public class Enemy : PoolableObject
{
    public EnnemyMovement Movement;
    public NavMeshAgent Agent;

    public override void OnDisable()
    {
        base.OnDisable();

        Agent.enabled = false;
    }
    public void ResetEnemy(Vector3 position, Transform target)
    {
       

        // Reactivate navmesh agent
        Agent.enabled = true;
        Agent.Warp(position);

        // Reset movement target
        Movement.target = target;

        // Reset knockback / physics
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reactivate the object
        gameObject.SetActive(true);
    }
}