using UnityEngine;

public class TutorialSpawner : MonoBehaviour
{
    private EnemySpawner enemySpawner;
    public Enemy enemyPrefab;
    public int ennemy;
    private void Awake()
    {
        enemySpawner = GetComponentInParent<EnemySpawner>() ?? FindFirstObjectByType<EnemySpawner>();
    }
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        for (int retry = 0; retry < 10 && enemyPrefab == null; retry++)
        {
            SpawnEnnemy();
            if (enemyPrefab == null) yield return new WaitForSecondsRealtime(0.1f);
        }
    }
    public void SpawnEnnemy()
    {
        if (enemyPrefab != null && enemyPrefab.gameObject.activeInHierarchy) return;
        if (enemySpawner == null) enemySpawner = GetComponentInParent<EnemySpawner>() ?? FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner == null || enemySpawner.EnemyPrefabs.Count == 0) return;

        switch (ennemy)
        {
            case 0:
                if (enemySpawner.EnemyPrefabs.Count > 0)
                {
                    enemyPrefab = enemySpawner.SpawnEnemyAt(enemySpawner.EnemyPrefabs[0], transform);
                    if (enemyPrefab != null)
                    {
                        var movement = enemyPrefab.gameObject.GetComponent<EnnemyMovement>();
                        if (movement != null) movement.tutorialSpawner = this;
                    }
                }
                break;

            case 1:
                int golemIndex = enemySpawner.EnemyPrefabs.Count > 2 ? 2 : 0;
                enemyPrefab = enemySpawner.SpawnEnemyAt(enemySpawner.EnemyPrefabs[golemIndex], transform);
                if (enemyPrefab != null)
                {
                    var movement = enemyPrefab.gameObject.GetComponent<EnnemyMovement>();
                    if (movement != null) movement.tutorialSpawner = this;
                }
                break;
            default:
                break;
        }
    }
}
