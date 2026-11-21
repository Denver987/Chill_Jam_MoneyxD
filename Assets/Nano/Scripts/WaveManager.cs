using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawning")]
    public EnemySpawner spawner;
    public float baseSpawnInterval = 2f;
    public int baseSpawnCount = 3;
    public int maxAliveEnemies = 25;
    [SerializeField] private float openingSpawnRateMultiplier = 1.45f;
    [SerializeField] private float openingBoostSeconds = 60f;

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
        UpdateStage();
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

    public enum EnemyTier
    {
        Basic,
        Intermediate,
        Heavy
    }

    public static EnemyTier GetEnemyTier(Enemy enemy)
    {
        if (enemy == null) return EnemyTier.Basic;
        if (enemy.Movement != null && enemy.Movement.isGolem) return EnemyTier.Heavy;
        if (enemy.Config != null)
        {
            string configName = enemy.Config.name;
            if (configName.IndexOf("Golem", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return EnemyTier.Heavy;
            if (configName.IndexOf("Goblin", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                configName.IndexOf("Imp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return EnemyTier.Intermediate;
        }
        string enemyName = enemy.name;
        if (enemyName.IndexOf("Golem", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return EnemyTier.Heavy;
        if (enemyName.IndexOf("Imp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            enemyName.IndexOf("Goblin", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return EnemyTier.Intermediate;

        return EnemyTier.Basic;
    }

    private int GetCurrentPlayerLevel()
    {
        if (spawner != null && spawner.target != null)
        {
            var player = spawner.target.GetComponent<Player>();
            if (player != null) return player.level;
        }
        return 1;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentStage != null && currentStage.enemyTypes.Length > 0)
            {
                // Cap concurrent alive enemies so late game doesn't turn into an unplayable flood
                if (spawner != null && spawner.activeEnemiesCount >= maxAliveEnemies)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                float minutes = elapsedTime / 60f;

                int enemiesToSpawn = baseSpawnCount + Mathf.RoundToInt(spawnCountCurve.Evaluate(minutes));
                float interval = baseSpawnInterval / Mathf.Max(0.1f, spawnRateCurve.Evaluate(minutes));
                // A livelier opening without increasing the late-game enemy cap.
                if (elapsedTime < openingBoostSeconds) interval /= openingSpawnRateMultiplier;

                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    if (spawner != null && spawner.activeEnemiesCount >= maxAliveEnemies)
                        break;

                    Enemy enemy = PickEnemy(currentStage.enemyTypes);
                    if (enemy != null)
                    {
                        spawner.SpawnSpecificEnemy(enemy);
                    }

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
        int playerLevel = GetCurrentPlayerLevel();
        bool isTutorial = GameManager.Instance != null && GameManager.Instance.isTutorial;

        // Level-gated enemy spawns:
        // Level 1-4: Only Basic enemies
        // Level 5-6: Intermediate enemies (Goblins / Ghouls) begin spawning (coinciding with Magnetic Pull unlock)
        // Level 7+: Heavy Golems begin spawning
        List<EnemySpawnData> eligible = new List<EnemySpawnData>();
        foreach (var e in enemyTypes)
        {
            if (e.enemyPrefab == null) continue;
            if (!isTutorial)
            {
                EnemyTier tier = GetEnemyTier(e.enemyPrefab);
                if (tier == EnemyTier.Heavy && playerLevel < 7) continue;
                if (tier == EnemyTier.Intermediate && playerLevel < 5) continue;
            }
            eligible.Add(e);
        }

        if (!isTutorial && playerLevel >= 7)
        {
            bool hasHeavy = false;
            for (int i = 0; i < eligible.Count; i++)
            {
                if (GetEnemyTier(eligible[i].enemyPrefab) == EnemyTier.Heavy)
                {
                    hasHeavy = true;
                    break;
                }
            }

            if (!hasHeavy)
            {
                Enemy heavyPrefab = null;
                if (spawner != null && spawner.EnemyPrefabs != null)
                    heavyPrefab = spawner.EnemyPrefabs.Find(p => p != null && GetEnemyTier(p) == EnemyTier.Heavy);

                if (heavyPrefab == null && stages != null)
                {
                    foreach (var s in stages)
                    {
                        if (s.enemyTypes == null) continue;
                        foreach (var d in s.enemyTypes)
                        {
                            if (d.enemyPrefab != null && GetEnemyTier(d.enemyPrefab) == EnemyTier.Heavy)
                            {
                                heavyPrefab = d.enemyPrefab;
                                break;
                            }
                        }
                        if (heavyPrefab != null) break;
                    }
                }

                if (heavyPrefab != null)
                    eligible.Add(new EnemySpawnData { enemyPrefab = heavyPrefab, weight = 0.5f });
            }
        }

        // Fallback to basic enemy if all current stage types are level-locked
        if (eligible.Count == 0)
        {
            foreach (var e in enemyTypes)
            {
                if (e.enemyPrefab != null && GetEnemyTier(e.enemyPrefab) == EnemyTier.Basic)
                {
                    eligible.Add(e);
                    break;
                }
            }
        }

        if (eligible.Count == 0)
        {
            if (spawner != null && spawner.EnemyPrefabs.Count > 0)
                return spawner.EnemyPrefabs[0];
            return enemyTypes.Length > 0 ? enemyTypes[0].enemyPrefab : null;
        }

        float totalWeight = 0f;
        foreach (var e in eligible) totalWeight += e.weight;

        float randomValue = Random.value * totalWeight;
        foreach (var e in eligible)
        {
            if (randomValue < e.weight)
                return e.enemyPrefab;
            randomValue -= e.weight;
        }
        return eligible[0].enemyPrefab; // fallback
    }

    public float GetEnemyStatMultiplier()
    {
        float minutes = elapsedTime / 60f;
        return enemyStatCurve.Evaluate(minutes);
    }
}
