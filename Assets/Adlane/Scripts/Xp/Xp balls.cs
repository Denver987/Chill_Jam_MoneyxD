using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using SmallHedge.SoundManager;

public class Xpballs : MonoBehaviour
{
    // static registry to track all active XP ball instances
    private static readonly List<Xpballs> allXpBalls = new List<Xpballs>();

    private Collider myCollider;
    private Vector3 startPosition;
    private Transform playerTransform;
    private Vector3 newPosition;

    [Header("Movement")]
    public float rotatingSpeed = 100f;
    public float pullStrength = 10f;
    public float radius = 15f;

    [Header("Separation (avoid stacking)")]
    public float separationRadius = 0.5f;       // min distance before repulsion kicks in
    public float separationStrength = 5f;       // how strong the repulsion is

    public enum ballType { green, red, purple, diamond }
    public ballType type;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        startPosition = transform.position;
        newPosition = startPosition;
    }

    void OnEnable()
    {
        if (!allXpBalls.Contains(this)) allXpBalls.Add(this);
    }

    void OnDisable()
    {
        allXpBalls.Remove(this);
    }

    void OnDestroy()
    {
        allXpBalls.Remove(this);
    }

    public void Update()
    {
        // rotate
        transform.Rotate(0f, rotatingSpeed * Time.deltaTime, 0f, Space.World);

        // find player transform (existing logic)
        newPosition = transform.position;
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        playerTransform = null;
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<Player>(out Player player))
            {
                playerTransform = player.transform;
                break;
            }
        }

        // pull to player if in range (existing logic)
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            float distanceToPlayer = toPlayer.magnitude;
            if (distanceToPlayer <= radius)
            {
                Vector3 moveDirection = toPlayer.normalized;
                newPosition += moveDirection * pullStrength * Time.deltaTime;
            }
        }

        // Separation pass: push away from other xp balls to avoid stacking
        // cheap O(n) pass over active balls; keep it light by only doing short-range checks
        for (int i = 0; i < allXpBalls.Count; i++)
        {
            var other = allXpBalls[i];
            if (other == null || other == this) continue;

            Vector3 diff = transform.position - other.transform.position;
            float dist = diff.magnitude;
            if (dist > 0f && dist < separationRadius)
            {
                // direction away from other ball
                Vector3 away = diff.normalized;
                // amount to push this frame
                float pushAmount = (separationRadius - dist) * separationStrength * Time.deltaTime;
                newPosition += away * pushAmount;
            }
        }

        transform.position = newPosition;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            SoundManager.PlaySound(SoundType.stone, player.gameObject.GetComponent<AudioSource>(), 0.5f);
            switch (type)
            {
                case ballType.green:
                    player.addExperience(10);
                    this.gameObject.SetActive(false);
                    break;
                case ballType.red:
                    player.addExperience(15);
                    this.gameObject.SetActive(false);
                    break;
                case ballType.purple:
                    player.addExperience(20);
                    this.gameObject.SetActive(false);
                    break;
                case ballType.diamond:
                    player.addExperience(25);
                    this.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
