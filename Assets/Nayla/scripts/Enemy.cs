using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour, IHitable
{
    private Rigidbody rb;

    public void Execute(Transform executionSource)
    {
        KnockBackEntity(executionSource);
    }
    private void KnockBackEntity(Transform executionSource)
    {
        Vector3 dir = (transform.position = executionSource.transform.position).normalized;
        rb.AddForce(dir, ForceMode.Impulse);
    }
}
