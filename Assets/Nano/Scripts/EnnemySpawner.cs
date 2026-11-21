using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public Transform target;
    public List<Enemy> EnemyPrefabs = new List<Enemy>();
    public List<Transform> spawnzones = new List<Transform>();

    public Dictionary<int, ObjectPool> EnemyObjectPools = new Dictionary<int, ObjectPool>();
    public List<Enemy> ActiveEnemies = new List<Enemy>();
    [Header("Spawn Limits")]
    public int maxAliveEnemies = 25;
    public int activeEnemiesCount
    {
        get
        {
            ActiveEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy || (e.Movement != null && e.Movement.isDead));
            return ActiveEnemies.Count;
        }
    }




    [Header("Shell Settings")]
    public GameObject bouncingShellPrefab;
    public List<Transform> shellSpawnZones = new List<Transform>(); // different spawn zones
    public int bouncingShellPoolSize = 5;
    public float spawnInterval = 5f; // how often to try spawning
    [Range(0f, 1f)] public float spawnChance = 0.3f; // 30% chance
    private ObjectPool bouncingShellPool;


    [Header("Potions Settings")]
    public GameObject healthPotionPrefab;
    public int healthPotionPoolSize = 20;
    [Range(0f, 1f)] public float healthPotionDropChance = 0.2f; // 20% chance on enemy death
    private ObjectPool healthPotionPool;


    [Header("Experience Settings")]
    public List<GameObject> xpOrbPrefabs = new List<GameObject>(); // assign 1 prefab per type in inspector
    public int xpOrbPoolSizePerType = 20;
    public float xpOrbSpawnRadius = 1.5f;

    private List<ObjectPool> xpOrbPools = new List<ObjectPool>();


    [Header("Pooling Settings")]
    public int initialPoolSize = 20;

    private void Awake()
    {
        for (int i = 0; i < EnemyPrefabs.Count; i++)
        {
            if (EnemyPrefabs[i] != null) EnsureEnemyPool(i);
        }
     

        if (bouncingShellPrefab != null)
        {
            bouncingShellPool = ObjectPool.CreateInstance(
                bouncingShellPrefab.GetComponent<PoolableObject>(),
                bouncingShellPoolSize,
                this.transform
            );
        }
        if(healthPotionPrefab != null)
        {
            healthPotionPool = ObjectPool.CreateInstance(
                healthPotionPrefab.GetComponent<PoolableObject>(),
                healthPotionPoolSize,
                this.transform
            );
        }

        // XP orbs: one pool per orb prefab. The pool index is used as the orb
        // type index, so keep the same order as xpOrbPrefabs.
        xpOrbPools.Clear();
        for (int i = 0; i < xpOrbPrefabs.Count; i++)
        {
            var orbPrefab = xpOrbPrefabs[i];
            var poolable = orbPrefab != null ? orbPrefab.GetComponent<PoolableObject>() : null;
            if (poolable == null)
            {
                Debug.LogError($"XP orb prefab at index {i} is missing a PoolableObject component.");
                xpOrbPools.Add(null);
                continue;
            }

            xpOrbPools.Add(ObjectPool.CreateInstance(poolable, xpOrbPoolSizePerType, this.transform));
        }
        
     

    }

    
    private bool once = false;

    private bool EnsureEnemyPool(int index)
    {
        if (EnemyObjectPools.ContainsKey(index)) return true;
        if (index < 0 || index >= EnemyPrefabs.Count || EnemyPrefabs[index] == null) return false;
        EnemyObjectPools[index] = ObjectPool.CreateInstance(EnemyPrefabs[index], initialPoolSize, transform);
        return true;
    }

    private void Update()
    { 
        if (once || target == null) return;

        var player = target.GetComponent<Player>();
        int playerLevel = player != null ? player.level : 1;
        if (playerLevel >= 7 && !once
            && GameManager.Instance != null && !GameManager.Instance.isTutorial
            && EnemyPrefabs.Count > 2 && !EnemyObjectPools.ContainsKey(2))
        {
            EnsureEnemyPool(2);
            once = true; // ensure this runs only once
        }
    }

    public void SpawnXpOrbs(Vector3 enemyPosition, int count, int orbTypeIndex = 0)
    {
        if (xpOrbPools.Count == 0) return;
        if (orbTypeIndex < 0 || orbTypeIndex >= xpOrbPools.Count) orbTypeIndex = 0;

        ObjectPool pool = xpOrbPools[orbTypeIndex];
        if (pool == null) return;

        for (int i = 0; i < count; i++)
        {
            PoolableObject pooledOrb = pool.GetObject();
            if (pooledOrb == null) return; // pool empty

            Vector3 randomOffset = Random.insideUnitSphere * xpOrbSpawnRadius;
            randomOffset.y = 0f; // keep on ground plane

            GameObject orbObj = pooledOrb.gameObject;
            orbObj.transform.position = enemyPosition + randomOffset;
            orbObj.transform.rotation = Quaternion.identity;
            orbObj.SetActive(true);
        }
    }


    private void Start()
    {

        if (GameManager.Instance != null && GameManager.Instance.isTutorial)
        {
            EnsureEnemyPool(2);
            once = true; // ensure this runs only once
        }
            // Start automatic shell spawning
            StartCoroutine(ShellSpawnRoutine());

        // These are arena spawn points, not the tutorial's local training points.
        if (GameManager.Instance != null && GameManager.Instance.isTutorial) return;
        foreach (var zone in spawnzones)
        {
            if (!NavMesh.SamplePosition(zone.position, out _, 5f, NavMesh.AllAreas))
            {
                Debug.LogError($"❌ Spawn zone '{zone.name}' is NOT on the NavMesh! Fix it in the scene.");
            }
        }
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
        if (prefab == null) return;
        int index = EnemyPrefabs.IndexOf(prefab);
        if (index < 0)
        {
            EnemyPrefabs.Add(prefab);
            index = EnemyPrefabs.Count - 1;
            EnsureEnemyPool(index);
        }
        DoSpawnEnemy(index);
    }

    private void DoSpawnEnemy(int spawnIndex)
    {
        if (!isActiveAndEnabled || spawnzones.Count == 0) return;
        if (activeEnemiesCount >= maxAliveEnemies) return;

        Enemy prefab = EnemyPrefabs[spawnIndex];
        int playerLevel = target != null && target.TryGetComponent<Player>(out var p) ? p.level : 1;
        bool isTutorial = GameManager.Instance != null && GameManager.Instance.isTutorial;
        if (!isTutorial)
        {
            var tier = WaveManager.GetEnemyTier(prefab);
            if (tier == WaveManager.EnemyTier.Intermediate && playerLevel < 5) return;
            if (tier == WaveManager.EnemyTier.Heavy && playerLevel < 7) return;
        }

        if (!EnsureEnemyPool(spawnIndex)) return;

        PoolableObject poolableObject = EnemyObjectPools[spawnIndex].GetObject();
        if (poolableObject == null)
        {
            Debug.LogWarning($"Pool for {EnemyPrefabs[spawnIndex].name} is exhausted!");
            return;
        }

        Enemy enemy = poolableObject.GetComponent<Enemy>();

        // Pick one spawn point
        Transform spawnPoint = spawnzones[Random.Range(0, spawnzones.Count)];

        // Force snap to NavMesh only
        if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            // Found a valid NavMesh position near spawnPoint
            StartCoroutine(enemy.ResetEnemy(hit.position, target, 2f));

            if (!ActiveEnemies.Contains(enemy))
                ActiveEnemies.Add(enemy);
        }
        else
        {
            // No valid NavMesh found → skip this spawn
            Debug.Log($"⚠ Spawn zone '{spawnPoint.name}' is not on NavMesh! Enemy skipped.");
            poolableObject.gameObject.SetActive(false); // return to pool
        }
    }

    public void TrySpawnHealthPotion(Vector3 position, float dropChance)
    {
        if (healthPotionPool == null) return;

        // Roll the dice
        if (Random.value <= dropChance)
        {
            PoolableObject pooledPotion = healthPotionPool.GetObject();
            if (pooledPotion == null) return; // pool empty

            GameObject potionObj = pooledPotion.gameObject;
            potionObj.transform.position = position;
            potionObj.transform.rotation = Quaternion.identity;
            potionObj.SetActive(true);
        }
    }

    public Enemy SpawnEnemyAt(Enemy prefab, Transform spawnPoint)
    {
        int index = EnemyPrefabs.IndexOf(prefab);
        if (index < 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} not found in EnemyPrefabs list!");
            return null;
        }

        // TutorialSpawner.Start can run before this component's Start.
        if (!EnsureEnemyPool(index)) return null;
        if (!isActiveAndEnabled) return null;

        PoolableObject poolableObject = EnemyObjectPools[index].GetObject();
        if (poolableObject == null)
        {
            Debug.LogWarning($"Pool for {EnemyPrefabs[index].name} is exhausted!");
            return null;
        }

        Enemy enemy = poolableObject.GetComponent<Enemy>();

        // Snap to NavMesh if possible
        if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            StartCoroutine(enemy.ResetEnemy(hit.position, target, 2f));

            if (!ActiveEnemies.Contains(enemy))
                ActiveEnemies.Add(enemy);

            return enemy;
        }
        else
        {
            Debug.Log($"⚠ Tutorial spawn zone '{spawnPoint.name}' is not on NavMesh! Enemy skipped.");
            poolableObject.gameObject.SetActive(false); // return to pool
            return null;
        }
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (ActiveEnemies.Contains(enemy))
            ActiveEnemies.Remove(enemy);
    }
}
