using System.Collections;
using System.Collections.Generic;
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
        Debug.Log($"Spawning enemy of type {SpawnIndex} from pool {poolableObject.name}");
        Debug.Log($"Available objects in pool: {EnemyObjectPools[SpawnIndex].AvailableObjectsPool.Count}");
        if (poolableObject != null)
        {
            Enemy enemy = poolableObject.GetComponent<Enemy>();

            int VertexIndex = Random.Range(0, Triangulation.vertices.Length);

            NavMeshHit Hit;
            if (NavMesh.SamplePosition(Triangulation.vertices[VertexIndex], out Hit, 2f, -1))
            {
                enemy.ResetEnemy(Hit.position + Vector3.up, target);

                if (!ActiveEnemies.Contains(enemy))
                    ActiveEnemies.Add(enemy);
            }
            else
            {
                Debug.LogError($"Unable to place NavMeshAgent on NavMesh. Tried to use {Triangulation.vertices[VertexIndex]}");
            }
        }
        else
        {
            Debug.LogError($"Unable to fetch enemy of type {SpawnIndex} from object pool. Out of objects?");
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