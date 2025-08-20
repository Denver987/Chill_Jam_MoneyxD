using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private PoolableObject Prefab;
    private int Size;
    public List<PoolableObject> AvailableObjectsPool;

    private ObjectPool(PoolableObject Prefab, int Size)
    {
        this.Prefab = Prefab;
        this.Size = Size;
        AvailableObjectsPool = new List<PoolableObject>(Size);
    }

    public static ObjectPool CreateInstance(PoolableObject Prefab, int Size, Transform parent = null)
    {
        ObjectPool pool = new ObjectPool(Prefab, Size);

        GameObject poolGameObject = new GameObject(Prefab.name + " Pool");

        if (parent != null)
        {
            poolGameObject.transform.SetParent(parent, false);
            poolGameObject.transform.position = parent.position;   // ✅ match parent position
            poolGameObject.transform.rotation = parent.rotation;   // ✅ optional: match parent rotation
        }

        pool.CreateObjects(poolGameObject);

        return pool;
    }


    private void CreateObjects(GameObject parent)
    {
        for (int i = 0; i < Size; i++)
        {
            PoolableObject poolableObject = GameObject.Instantiate(Prefab, Vector3.zero, Quaternion.identity, parent.transform);
            poolableObject.Parent = this;
            poolableObject.gameObject.SetActive(false); // PoolableObject handles re-adding the object to the AvailableObjects
        }
    }

   
    public PoolableObject GetObject()
    {
        PoolableObject instance = AvailableObjectsPool[0];

        AvailableObjectsPool.RemoveAt(0);

        instance.gameObject.SetActive(true);

        return instance;
    }

    public void ReturnObjectToPool(PoolableObject Object)
    {
        Debug.Log($"Returning {Object.name} to pool {this.Prefab.name}");
        AvailableObjectsPool.Add(Object);
        Debug.Log($"Available objects in pool: {AvailableObjectsPool.Count}");
    }
}