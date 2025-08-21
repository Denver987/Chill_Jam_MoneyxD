using System.Collections;
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

    [Header("Shell Settings")]
    public GameObject bouncingShellPrefab;
    public List<Transform> shellSpawnZones = new List<Transform>(); // different spawn zones
    public int bouncingShellPoolSize = 5;
    public float spawnInterval = 5f; // how often to try spawning
    [Range(0f, 1f)] public float spawnChance = 0.3f; // 30% chance
    private ObjectPool bouncingShellPool;

    [Header("Pooling Settings")]
    public int initialPoolSize = 20;

    private void Awake()
    {
        // create object pools for each enemy prefab
        for (int i = 0; i < EnemyPrefabs.Count; i++)
        {
            EnemyObjectPools.Add(i, ObjectPool.CreateInstance(EnemyPrefabs[i],initialPoolSize, this.transform));
        }

        if (bouncingShellPrefab != null)
        {
            bouncingShellPool = ObjectPool.CreateInstance(
                bouncingShellPrefab.GetComponent<PoolableObject>(),
                bouncingShellPoolSize,
                this.transform
            );
        }
    }

    private void Start()
    {
        // Start automatic shell spawning
        StartCoroutine(ShellSpawnRoutine());
    }

    private IEnumerator ShellSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (Random.value <= spawnChance) // roll chance
            {
                SpawnBouncingShell();
            }
        }
    }
    private void SpawnBouncingShell()
    {
        if (bouncingShellPool == null || shellSpawnZones.Count == 0) return;

        PoolableObject pooledShell = bouncingShellPool.GetObject();
        if (pooledShell == null) return; // pool empty

        // Pick random spawn point
        Transform spawnPoint = shellSpawnZones[Random.Range(0, shellSpawnZones.Count)];

        GameObject shellObj = pooledShell.gameObject;
        shellObj.transform.position = spawnPoint.position;
        shellObj.transform.rotation = Quaternion.identity; // reset rotation
        shellObj.SetActive(true);

        // Reset movement
        var shell = shellObj.GetComponent<BouncingShell>();
        if (shell != null)
        {
            var rb = shellObj.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            shell.enabled = true;   // restart wandering
            shell.RestartWandering(); // <-- ensure it always starts wandering fresh
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
