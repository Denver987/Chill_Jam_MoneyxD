using UnityEngine;

public class Potion : PoolableObject
{
 public virtual void OnEnable()
    {
    }
    public override void OnDisable()
    {
        base.OnDisable();
    }
}
