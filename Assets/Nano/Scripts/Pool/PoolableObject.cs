using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    public ObjectPool Parent;

    public virtual void OnDisable()
    {
        // Objects that live in the scene (not spawned by a pool) have no parent.
        if (Parent == null) return;

        Parent.ReturnObjectToPool(this);
    }
}
