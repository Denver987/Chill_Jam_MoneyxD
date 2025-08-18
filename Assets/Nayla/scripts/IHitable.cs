using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHitable
{
    public void Execute(Transform executionSource, float knockbackStrength);

    public void ExecuteWithUpwardBias(Transform executionSource, float knockbackStrength, float upwardBias = 0.5f);
}