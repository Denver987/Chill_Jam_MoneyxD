using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public Transform target;
    public List<Enemy> EnemyPrefabs = new List<Enemy>();
    public List<Transform> spawnzones = new List<Transform>();

    public Dictionary<int, ObjectPool> EnemyObjectPools = new Dictionary<int, ObjectPool>();
    public List<Enemy> ActiveEnemies = new List<Enemy>();
    public int activeEnemiesCount => ActiveEnemies.Count;

    [Header("Pooling Settings")]
    public int initialPoolSize = 20;

    private void Awake()
    {
        // create object pools for each enemy prefab
        for (int i = 0; i < EnemyPrefabs.Count; i++)
        {
            EnemyObjectPools.Add(i, ObjectPool.CreateInstance(EnemyPrefabs[i],initialPoolSize, this.transform));
        }
    }

    /// <summary>Spawn an enemy prefab directly (used by WaveManager)</summary>
    public void SpawnSpecificEnemy(Enemy prefab)
    {
        int index = EnemyPrefabs.IndexOf(prefab);
        if (index >= 0)
        {
            DoSpawnEnemy(index);
        }
        else
        {
            Debug.LogWarning($"Prefab {prefab.name} not found in EnemyPrefabs list!");
        }
    }

    private void DoSpawnEnemy(int spawnIndex)
    {
        if (!EnemyObjectPools.ContainsKey(spawnIndex)) return;

        PoolableObject poolableObject = EnemyObjectPools[spawnIndex].GetObject();

        if (poolableObject == null)
        {
            // Pool is empty, no more enemies of this type can be spawned
            Debug.LogWarning($"Pool for {EnemyPrefabs[spawnIndex].name} is exhausted!");
            return;
        }

        Enemy enemy = poolableObject.GetComponent<Enemy>();

        // Pick one spawn point
        Transform spawnPoint = spawnzones[Random.Range(0, spawnzones.Count)];

        if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            enemy.ResetEnemy(hit.position, target);

            if (!ActiveEnemies.Contains(enemy))
                ActiveEnemies.Add(enemy);
        }
    }


    public void RemoveEnemy(Enemy enemy)
    {
        if (ActiveEnemies.Contains(enemy))
            ActiveEnemies.Remove(enemy);
    }
}
