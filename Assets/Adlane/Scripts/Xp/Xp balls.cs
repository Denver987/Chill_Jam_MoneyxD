using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using SmallHedge.SoundManager;

public class Xpballs : PoolableObject
{
    // static registry to track all active XP ball instances
    private static readonly List<Xpballs> allXpBalls = new List<Xpballs>();
    private static Transform cachedPlayer;

    private Collider myCollider;
    private Vector3 startPosition;
    private Transform playerTransform;
    private Vector3 newPosition;
    private static readonly Collider[] separationNeighbours = new Collider[16];
    private Vector3 separationVelocity;
    private float nextSeparationUpdate;

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
        separationVelocity = Vector3.zero;
        nextSeparationUpdate = Time.time + Random.Range(0f, 0.12f);
    }

    public override void OnDisable()
    {
        allXpBalls.Remove(this);
        base.OnDisable();
    }

    void OnDestroy()
    {
        allXpBalls.Remove(this);
    }

    public void Update()
    {
        // rotate
        transform.Rotate(0f, rotatingSpeed * Time.deltaTime, 0f, Space.World);

        // Cache the player instead of running a physics overlap for every orb
        // on every frame (there can be hundreds of orbs alive at once).
        newPosition = transform.position;
        if (cachedPlayer == null)
        {
            var foundPlayer = FindFirstObjectByType<Player>();
            if (foundPlayer != null) cachedPlayer = foundPlayer.transform;
        }
        playerTransform = cachedPlayer;

        // pull to player if in range
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            if (toPlayer.sqrMagnitude <= radius * radius)
            {
                Vector3 moveDirection = toPlayer.normalized;
                newPosition += moveDirection * pullStrength * Time.deltaTime;
            }
        }

        // Previously every orb scanned every other orb on every frame: O(n²).
        // Use the physics broadphase at a staggered 8 Hz, with a fixed neighbour
        // cap and no allocations. Pull/rotation remain smooth every frame.
        if (Time.time >= nextSeparationUpdate)
        {
            nextSeparationUpdate = Time.time + 0.12f;
            separationVelocity = Vector3.zero;
            int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius,
                separationNeighbours, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var neighbour = separationNeighbours[i];
                if (neighbour == myCollider || !neighbour.TryGetComponent<Xpballs>(out var other)) continue;
                Vector3 diff = transform.position - other.transform.position;
                float distance = diff.magnitude;
                if (distance > 0.001f && distance < separationRadius)
                    separationVelocity += diff / distance * ((separationRadius - distance) * separationStrength);
            }
        }
        newPosition += Vector3.ClampMagnitude(separationVelocity, separationStrength) * Time.deltaTime;

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
