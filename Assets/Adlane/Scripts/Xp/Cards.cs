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
    private bool unlockedAtStart;

    void Awake()
    {
        if (cardType == CardType.MagneticPull)
        {
            maxCardCaps = 1;
            if (minLevelToUnlock <= 0) minLevelToUnlock = 5;
        }
        else if (cardType == CardType.IncreasePullRange && maxCardCaps > 100)
        {
            cardType = CardType.Heal;
        }
        unlockedAtStart = canSpawn;
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
        if(EnemySpawner == null)
        {
            EnemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    // A card is offerable when it is unlocked (either from the start or by
    // reaching minLevelToUnlock) and its pick cap has not been reached.
    public bool IsAvailable(int playerLevel)
    {
        if (cardType == CardType.MagneticPull)
        {
            maxCardCaps = 1;
        }
        else if (cardType == CardType.IncreasePullRange && maxCardCaps > 100)
        {
            cardType = CardType.Heal;
        }

        if (CardCap || cardCapCounter >= maxCardCaps)
        {
            CardCap = true;
            return false;
        }

        if (player == null) player = FindFirstObjectByType<Player>();
        var selector = player != null ? player.GetComponent<SpellSelector>() : null;

        if (cardType == CardType.DecreasePullCd || cardType == CardType.IncreasePullRange)
        {
            if (selector == null || !selector.magneticPullEnabled)
                return false;
        }

        if (cardType == CardType.MagneticPull)
        {
            if (selector != null && selector.magneticPullEnabled)
            {
                CardCap = true;
                return false;
            }
            int req = minLevelToUnlock > 0 ? minLevelToUnlock : 5;
            return playerLevel >= req;
        }

        if (unlockedAtStart) return true;

        return minLevelToUnlock > 0 && playerLevel >= minLevelToUnlock;
    }

    public void CardChoose()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
        if (player == null)
        {
            // Nothing to apply the card to: make sure the game is not left
            // paused forever.
            if (CanvasSingleton.Instance != null && CanvasSingleton.Instance.CardsSelection != null)
            {
                CanvasSingleton.Instance.CardsSelection.SetActive(false);
            }
            Time.timeScale = 1f;
            return;
        }

        if (player.knockback != null)
        {
            var currentSelector = player.GetComponent<SpellSelector>();
            // Level up disables knockback; restore it only if the shockwave is
            // the spell the player actually has selected.
            player.knockback.enabled = currentSelector == null
                || currentSelector.currentSpell == SpellSelector.SpellType.Shockwave;
        }
        switch (cardType)
        {
            case CardType.MagneticPull:
                var selector = player.GetComponent<SpellSelector>();
                if (selector != null) selector.magneticPullEnabled = true;
                cardCapCounter++;
                break;
            case CardType.MaxHealth:
                player.maxHealth += 10;
                cardCapCounter++;
                break;
            case CardType.KnockbackForce:
                var forceKnockback = player.GetComponent<Knockback>();
                if (forceKnockback != null)
                {
                    forceKnockback.initialKnockbackStrength += 5;
                    forceKnockback.maxKnockbackStrength += 5;
                }
                cardCapCounter++;
                break;
            case CardType.SpeedBoost:
                var input = player.GetComponent<InputSystem>();
                if (input != null) input.moveSpeed += 2;
                cardCapCounter++;

                break;
            case CardType.HealthRegeneration:
                player.IncreaseHealing(2);
                cardCapCounter++;

                break;
            case CardType.SpawnRateCapsule:
                if (EnemySpawner != null) EnemySpawner.spawnChance += 0.1f;
                cardCapCounter++;
                break;
            case CardType.DropRatePotion:
                if (EnemySpawner != null)
                {
                    foreach (var enemy in EnemySpawner.EnemyPrefabs)
                    {
                        if (enemy == null) continue;
                        var movement = enemy.GetComponent<EnnemyMovement>();
                        if (movement != null) movement.HealthPotionDropChance += 0.05f;
                    }
                }
                cardCapCounter++;
                break;
            case CardType.IncreaseKnockbackRange:
                var rangeKnockback = player.GetComponent<Knockback>();
                if (rangeKnockback != null) rangeKnockback.maxDistance += 2;
                cardCapCounter++;

                break;
            case CardType.DecreasePullCd:
                var cdPull = player.GetComponent<MagneticPull>();
                if (cdPull != null) cdPull.pullCooldownTimer -= 1f;
                cardCapCounter++;

                break;
            case CardType.IncreasePullRange:
                var rangePull = player.GetComponent<MagneticPull>();
                if (rangePull != null) rangePull.pullRange += 2f;
                cardCapCounter++;

                break;
            case CardType.Heal:
                player.health = Mathf.Min(player.health + 15, player.maxHealth);
                cardCapCounter++;
                break;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (CanvasSingleton.Instance != null && CanvasSingleton.Instance.CardsSelection != null)
        {
            var selection = CanvasSingleton.Instance.CardsSelection.GetComponent<CardSelectionUI>();
            if (selection != null) selection.choosingCards = false;
        }

        // Let the player resume the game (or draw the next card set if several
        // levels were gained at once).
        player.OnUpgradeChosen();
    }
    public void Update()
    {
        if (cardCapCounter >= maxCardCaps)
        {
            CardCap = true;
        }
    }



}