using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IHitable
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Execute(Transform executionSource, float knockbackStrength)
    {
        KnockBackEntity(executionSource, knockbackStrength);
    }

    private void KnockBackEntity(Transform executionSource, float knockbackStrength)
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
