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
        IncreasePullRange
    }
    public CardType cardType;
    public bool canSpawn;
    public int minLevelToUnlock;
    public bool CardCap;
    public int cardCapCounter;
    public int maxCardCaps = 3;
    public Player player;
    void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
    }

    public void CardChoose()
    {

        switch (cardType)
        {
            case CardType.MagneticPull:
                player.GetComponent<SpellSelector>().magneticPullEnabled = true;
                cardCapCounter++;
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
                player.health += 2;
                cardCapCounter++;
                break;
            case CardType.SpawnRateCapsule:
                // TODO
                cardCapCounter++;
                break;
            case CardType.DropRatePotion:
                // TODO
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
        }
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