using System.Collections;
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


    public IEnumerator ResetEnemy(Vector3 position, Transform target, float spawnEffectDelay = 0.5f)
    {
        Movement.enabled = false; // Disable movement to prevent any unwanted actions during reset
        Movement.canattack = true;
        Movement.isDead = false;
        Movement.isKnockedBack = false;

        // Ensure the object is active so effects can play
        gameObject.SetActive(true);


        // Reset NavMesh agent and physics
        Agent.enabled = true;
       
        Agent.Warp(position);
        this.transform.position = position; // Move to the spawn position first
        Movement.target = target;

        // Show only the spawn effect child
        Transform spawnEffectChild = this.transform.GetChild(1);
        spawnEffectChild.gameObject.SetActive(true);

        // Hide other children temporarily
        for (int i = 0; i < this.transform.childCount; i++)
        {
            if (i != 1)
                this.transform.GetChild(i).gameObject.SetActive(false);
        }

        // Wait for the effect duration
        yield return new WaitForSeconds(spawnEffectDelay);

        this.transform.GetChild(1).gameObject.SetActive(false);

        // Reactivate full visuals
        for (int i = 2; i < this.transform.childCount; i++)
            this.transform.GetChild(i).gameObject.SetActive(true);


        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Movement.enabled = true;
    }


    public virtual void SetupAgentFromConfiguration()
    {
        Movement.moveSpeed = Random.Range(Config.minspeed,Config.maxspeed);
        Movement.spreadRadius = Config.spreadRadius; // how much the enemies spread out
        Movement.separationRadius = Config.separationRadius; // distance to keep from other enemies
        Movement.avoidanceForce = Config.avoidanceForce;
        Movement.separationUpdateRate = Config.separationUpdateRate;
        Movement.knockbackRecoveryTime = Config.knockbackRecoveryTime; // how long it takes to recover from knockback
        Movement.rotationSpeed = Random.Range(Config.minRotationSpeed, Config.maxRotationSpeed); // how fast the enemy rotates towards the target

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