using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IHitable
{
    private Rigidbody rb;
    float knockbackStrength = 10f; 
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Execute(Transform executionSource)
    {
        KnockBackEntity(executionSource);
    }

    private void KnockBackEntity(Transform executionSource)
    {

        if (executionSource == null)
        {
            return;
        }

        if (rb == null)
        {
            return;
        }

        Vector3 direction = (transform.position - executionSource.position).normalized;
        rb.AddForce(direction * knockbackStrength, ForceMode.Impulse);
    }
}
