using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawning")]
    public EnemySpawner spawner;
    public float baseSpawnInterval = 2f;
    public int baseSpawnCount = 3;

    [Header("Scaling")]
    public AnimationCurve spawnCountCurve;
    public AnimationCurve spawnRateCurve;
    public AnimationCurve enemyStatCurve;

    [Header("Enemy Stages")]
    public List<EnemyStage> stages = new List<EnemyStage>();

    private float elapsedTime = 0f;
    private EnemyStage currentStage;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateStage();
    }

    private void UpdateStage()
    {
        float minutes = elapsedTime / 60f;

        // Pick the latest stage that’s unlocked
        foreach (var stage in stages)
        {
            if (minutes >= stage.startMinute)
                currentStage = stage;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentStage != null && currentStage.enemyTypes.Length > 0)
            {
                float minutes = elapsedTime / 60f;

                int enemiesToSpawn = baseSpawnCount + Mathf.RoundToInt(spawnCountCurve.Evaluate(minutes));
                float interval = baseSpawnInterval / Mathf.Max(0.1f, spawnRateCurve.Evaluate(minutes));

                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    Enemy enemy = PickEnemy(currentStage.enemyTypes);
                    spawner.SpawnSpecificEnemy(enemy);

                    // 🔹 wait between individual enemy spawns
                    yield return new WaitForSeconds(0.2f);
                }

                // 🔹 wait before next wave cycle
                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return null;
            }
        }
    }


    private Enemy PickEnemy(EnemySpawnData[] enemyTypes)
    {
        float totalWeight = 0f;
        foreach (var e in enemyTypes) totalWeight += e.weight;

        float randomValue = Random.value * totalWeight;
        foreach (var e in enemyTypes)
        {
            if (randomValue < e.weight)
                return e.enemyPrefab;
            randomValue -= e.weight;
        }
        return enemyTypes[0].enemyPrefab; // fallback
    }

    public float GetEnemyStatMultiplier()
    {
        float minutes = elapsedTime / 60f;
        return enemyStatCurve.Evaluate(minutes);
    }
}
