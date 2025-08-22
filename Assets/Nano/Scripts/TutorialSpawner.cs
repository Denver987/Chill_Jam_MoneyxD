using UnityEngine;

public class TutorialSpawner : MonoBehaviour
{
    private EnemySpawner enemySpawner;
    public Enemy enemyPrefab;
    public int ennemy;
    private void Awake()
    {
        enemySpawner = GetComponentInParent<EnemySpawner>();
    }
    private void Start()
    {
        SpawnEnnemy();
    }
    public void SpawnEnnemy()
    {
        switch (ennemy)
        {
            case 0:
               enemyPrefab =  enemySpawner.SpawnEnemyAt(enemySpawner.EnemyPrefabs[0], transform);
                enemyPrefab.gameObject.GetComponent<EnnemyMovement>().tutorialSpawner = this;
                break;

            case 1:
                enemyPrefab = enemySpawner.SpawnEnemyAt(enemySpawner.EnemyPrefabs[2], transform);
                enemyPrefab.gameObject.GetComponent<EnnemyMovement>().tutorialSpawner = this;
                break;
            default:
                break;
        }
           
        }
    }
