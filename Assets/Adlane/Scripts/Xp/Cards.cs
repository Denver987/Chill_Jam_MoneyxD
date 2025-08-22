using System;
using Unity.VisualScripting;
using UnityEngine;



public class Cards : MonoBehaviour
{
    public enum CardType
    {
        MagneticPull,
        MaxHealth,
        KnockbackForce,
        SpeedBoost,
        HealthRegeneration,
        SpawnRateCapsule,
        DropRatePotion,
        IncreaseKnockbackRange,
        DecreasePullCd,
        IncreasePullRange,
        Heal
    }
    public CardType cardType;
    public bool canSpawn;
    public int minLevelToUnlock;
    public bool CardCap;
    public int cardCapCounter;
    public int maxCardCaps = 3;
    public Player player;
    public EnemySpawner EnemySpawner;
    void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
        if(EnemySpawner == null)
        {
            EnemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    public void CardChoose()
    {
        player.gameObject.GetComponent<Knockback>().enabled = true; 
        switch (cardType)
        {
            case CardType.MagneticPull:
                player.GetComponent<SpellSelector>().magneticPullEnabled = true;
                break;
            case CardType.MaxHealth:
                player.maxHealth += 10;
                cardCapCounter++;
                break;
            case CardType.KnockbackForce:
                player.GetComponent<Knockback>().initialKnockbackStrength += 5;
                player.GetComponent<Knockback>().maxKnockbackStrength += 5;
                cardCapCounter++;
                break;
            case CardType.SpeedBoost:
                player.GetComponent<InputSystem>().moveSpeed += 2;
                cardCapCounter++;

                break;
            case CardType.HealthRegeneration:
                player.IncreaseHealing(2);
                cardCapCounter++;

                break;
            case CardType.SpawnRateCapsule:
                EnemySpawner.spawnChance +=0.1f;
                cardCapCounter++;
                break;
            case CardType.DropRatePotion:
                // TODO
                foreach (var enemy in EnemySpawner.EnemyPrefabs)
                {
                    enemy.GetComponent<Enemy>().gameObject.GetComponent<EnnemyMovement>().HealthPotionDropChance += 0.05f;
                }
                cardCapCounter++;
                break;
            case CardType.IncreaseKnockbackRange:
                player.GetComponent<Knockback>().maxDistance += 2;
                cardCapCounter++;

                break;
            case CardType.DecreasePullCd:
                player.GetComponent<MagneticPull>().pullCooldownTimer -= 1f;
                cardCapCounter++;

                break;
            case CardType.IncreasePullRange:
                player.GetComponent<MagneticPull>().pullRange += 2f;
                cardCapCounter++;

                break;
            case CardType.Heal:
                player.health += 15;
                cardCapCounter++;
                break;
        }
        Time.timeScale = 1f;
        transform.parent.parent.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CanvasSingleton.Instance.CardsSelection.GetComponent<CardSelectionUI>().choosingCards = false;
    }
    public void Update()
    {
        if (cardCapCounter >= maxCardCaps)
        {
            CardCap = true;
            canSpawn = false;
        }
        if (player.level >= minLevelToUnlock)
        {
            canSpawn = true;
        }
    }



}