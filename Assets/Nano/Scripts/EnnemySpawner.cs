using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public Transform target;
    public int NumberOfEnemiesToSpawn = 5;
    public float SpawnDelay = 1f;
    public List<Enemy> EnemyPrefabs = new List<Enemy>();
    public SpawnMethod EnemySpawnMethod = SpawnMethod.RoundRobin;


    public bool infiniteSpawn = false; // if true, will spawn enemies infinitely until stopped

    private NavMeshTriangulation Triangulation;
    public Dictionary<int, ObjectPool> EnemyObjectPools = new Dictionary<int, ObjectPool>();

    public List<Transform> spawnzones = new List<Transform>();

    public List<Enemy> ActiveEnemies = new List<Enemy>();
    public int activeEnemiesCount => ActiveEnemies.Count;
    private void Awake()
    {
        for (int i = 0; i < EnemyPrefabs.Count; i++)
        {
            EnemyObjectPools.Add(i, ObjectPool.CreateInstance(EnemyPrefabs[i], NumberOfEnemiesToSpawn,this.transform));
        }
      
    }

    private void Start()
    {
        Triangulation = NavMesh.CalculateTriangulation();

        if(infiniteSpawn)
            StartCoroutine(SpawnInfinitEnnemies());
        else
            StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnInfinitEnnemies()
    {
        if (EnemyObjectPools.Count == 0)
            yield return null; // wait until there are no active enemies

        WaitForSeconds Wait = new WaitForSeconds(SpawnDelay);

        while (true) // infinite loop
        {
            // only spawn if below max active enemies
            if (ActiveEnemies.Count < NumberOfEnemiesToSpawn)
            {
                if (EnemySpawnMethod == SpawnMethod.RoundRobin)
                {
                    SpawnRoundRobinEnemy(ActiveEnemies.Count);
                }
                else if (EnemySpawnMethod == SpawnMethod.Random)
                {
                    SpawnRandomEnemy();
                }
            }

            yield return Wait;
        }
    }

    private IEnumerator SpawnEnemies()
    {
        WaitForSeconds Wait = new WaitForSeconds(SpawnDelay);

        int SpawnedEnemies = 0;

        while (SpawnedEnemies < NumberOfEnemiesToSpawn)
        {
            if (EnemySpawnMethod == SpawnMethod.RoundRobin)
            {
                SpawnRoundRobinEnemy(SpawnedEnemies);
            }
            else if (EnemySpawnMethod == SpawnMethod.Random)
            {
                SpawnRandomEnemy();
            }

            SpawnedEnemies++;

            yield return Wait;
        }
    }

    private void SpawnRoundRobinEnemy(int SpawnedEnemies)
    {
        int SpawnIndex = SpawnedEnemies % EnemyPrefabs.Count;

        DoSpawnEnemy(SpawnIndex);
    }

    private void SpawnRandomEnemy()
    {
        DoSpawnEnemy(Random.Range(0, EnemyPrefabs.Count));
    }

    private void DoSpawnEnemy(int SpawnIndex)
    {
        PoolableObject poolableObject = EnemyObjectPools[SpawnIndex].GetObject();
        if (poolableObject != null)
        {
            Enemy enemy = poolableObject.GetComponent<Enemy>();

            // Pick one spawn point
            Transform spawnPoint = spawnzones[Random.Range(0, spawnzones.Count)];

            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPoint.position, out hit, 2f, NavMesh.AllAreas))
            {
                enemy.ResetEnemy(hit.position, target);

                if (!ActiveEnemies.Contains(enemy))
                    ActiveEnemies.Add(enemy);
            }
        }
    }
    public void RemoveEnemy(Enemy enemy)
    {
        if (ActiveEnemies.Contains(enemy))
            ActiveEnemies.Remove(enemy);
    }

    public enum SpawnMethod
    {
        RoundRobin,
        Random
        // Other spawn methods can be added here
    }
}