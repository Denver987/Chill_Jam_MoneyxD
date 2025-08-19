using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    public ObjectPool Parent;

    public virtual void OnDisable()
    {
        Debug.Log($"Returning {this.name} to pool ");
        Parent.ReturnObjectToPool(this);
    }
}
