using UnityEngine;

[System.Serializable]
public class EnemySpawnData
{
    public Enemy enemyPrefab;
    public float weight = 1f; // higher = more likely
}

[System.Serializable]
public class EnemyStage
{
    public string name;                  // e.g. "Early Zombies"
    public EnemySpawnData[] enemyTypes;  // which enemies + weights
    public float startMinute;            // when this stage unlocks
}
