using UnityEngine;

public class Xpballs : MonoBehaviour
{
    public enum ballType
    {
        green,
        red,
        purple,
        diamond
    }
    public ballType type;
    public float rotatingSpeed = 100f;
    public float floatingSpeed = 1f;
    public float pullStrength = 10f;
    public float radius = 15f;
    public float height = 0.1f;

    private Vector3 startPosition;
    private Transform playerTransform;
    private Vector3 newPosition;

    void Start()
    {
        startPosition = transform.position;
        newPosition = startPosition;
    }

    public void Update()
    {
        // rotate around Y axis
        transform.Rotate(0f, rotatingSpeed * Time.deltaTime, 0f, Space.World);

        float yOffset = startPosition.y + Mathf.PingPong(Time.time * floatingSpeed, height) - (height * 0.5f);

        newPosition = transform.position;
        newPosition.y = yOffset;
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<Player>(out Player player))
            {
                playerTransform = player.transform;
                break;
            }
        }

        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = yOffset;
            float distanceToPlayer = toPlayer.magnitude;
            if (distanceToPlayer <= radius)
            {
                Vector3 moveDirection = toPlayer.normalized;
                newPosition += moveDirection * pullStrength * Time.deltaTime;
            }
        }
        newPosition.y = yOffset;
        transform.position = newPosition;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            switch (type)
            {
                case ballType.green:
                    player.experience += 10;
                    Destroy(gameObject);
                    break;
                case ballType.red:
                    player.experience += 20;
                    Destroy(gameObject);
                    break;
                case ballType.purple:
                    player.experience += 30;
                    Destroy(gameObject);
                    break;
                case ballType.diamond:
                    player.experience += 40;
                    Destroy(gameObject);
                    break;
            }
        }
    }
}
