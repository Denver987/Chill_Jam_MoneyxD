using UnityEngine;

public class Bouncer : PoolableObject
{
    private BouncingShell shell;

    private void Awake()
    {
        shell = GetComponent<BouncingShell>();
    }

    public virtual void OnEnable()
    {
        shell?.SendMessage("StartWandering", SendMessageOptions.DontRequireReceiver);
        // resets into wander mode every time it's enabled
    }

    public override void OnDisable()
    {
        base.OnDisable();
        // stop movement cleanly
        if (shell != null)
        {
            var rb = shell.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
