using UnityEngine;
using UnityEngine.AI;

public class Enemy : PoolableObject
{
    public EnnemyMovement Movement;
    public NavMeshAgent Agent;
    public EnemyScriptableObject Config;


    public virtual void OnEnable()
    {
        SetupAgentFromConfiguration();
    }
    public override void OnDisable()
    {
        base.OnDisable();

        Agent.enabled = false;
    }
    public void ResetEnemy(Vector3 position, Transform target)
    {
       
        Movement.canattack = true; // Reset attack state
        Movement.isDead = false; // Reset death state
        Movement.isKnockedBack = false; // Reset knockback state
        this.transform.GetChild(1).gameObject.SetActive(true); // hide the dust explosion  
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

    public virtual void SetupAgentFromConfiguration()
    {
        Movement.moveSpeed = Config.moveSpeed;
        Movement.spreadRadius = Config.spreadRadius; // how much the enemies spread out
        Movement.separationRadius = Config.separationRadius; // distance to keep from other enemies
        Movement.avoidanceForce = Config.avoidanceForce;
        Movement.separationUpdateRate = Config.separationUpdateRate;
        Movement.knockbackRecoveryTime = Config.knockbackRecoveryTime; // how long it takes to recover from knockback

        Movement.attackCooldown = Config.attackCooldown;
        Movement.attackRange = Config.attackRange;
        Movement.attackKnockbackForce = Config.attackKnockbackForce;
        Movement.attackDamage = Config.attackDamage;

        Agent.avoidancePriority = Config.AvoidancePriority;
        Agent.baseOffset = Config.BaseOffset;
        Agent.height = Config.Height;
        Agent.obstacleAvoidanceType = Config.ObstacleAvoidanceType;
        Agent.radius = Config.Radius;
        Agent.stoppingDistance = Config.StoppingDistance;


    }
}