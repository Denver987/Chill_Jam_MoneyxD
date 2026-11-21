using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardsSpawner : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    private List<int> indexesForCards = new List<int>();
    private List<int> spawnIndexes = new List<int>();
    public int first;
    public int second;
    public int third;
    public GameObject healingCard;
    public List<Transform> cardSlots; 
    public Vector3 targetScale = new Vector3(0.45f, 0.45f, 0.45f);
    private Dictionary<Transform, Vector3> cachedScales = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (cards.Count == 0)
        {
            foreach (var card in gameObject.GetComponentsInChildren<Cards>())
            {
                cards.Add(card.gameObject);
                Debug.Log("Card added: " + card.name);
            }
        }
        foreach (var card in cards)
            if (card != null && card.transform.localScale != Vector3.zero)
                cachedScales[card.transform] = card.transform.localScale;
        if (healingCard != null && healingCard.transform.localScale != Vector3.zero)
            cachedScales[healingCard.transform] = healingCard.transform.localScale;
    }

    private Vector3 GetCardScale(Transform t) =>
        t != null && cachedScales.TryGetValue(t, out var s) ? s : targetScale;

    public void IndexsForCards()
    {
        indexesForCards.Clear();
        spawnIndexes.Clear();

        var player = FindFirstObjectByType<Player>();
        int playerLevel = player != null ? player.level : 1;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i].GetComponent<Cards>();
            if (card != null && card.IsAvailable(playerLevel))
            {
                indexesForCards.Add(i);
            }
        }
        Debug.Log("Spawn indexes: " + indexesForCards.Count);

        var selector = player != null ? player.GetComponent<SpellSelector>() : null;
        if (playerLevel >= 5 && selector != null && !selector.magneticPullEnabled)
        {
            int pullIdx = indexesForCards.FindIndex(idx =>
            {
                var c = cards[idx].GetComponent<Cards>();
                return c != null && c.cardType == Cards.CardType.MagneticPull;
            });
            if (pullIdx != -1)
            {
                spawnIndexes.Add(indexesForCards[pullIdx]);
                indexesForCards.RemoveAt(pullIdx);
            }
        }

        int maxSlots = cardSlots != null && cardSlots.Count > 0 ? cardSlots.Count : 3;

        while (spawnIndexes.Count < maxSlots && indexesForCards.Count > 0)
        {
            int pick = Random.Range(0, indexesForCards.Count);
            spawnIndexes.Add(indexesForCards[pick]);
            indexesForCards.RemoveAt(pick);
        }

        if (spawnIndexes.Count == 0 && healingCard != null)
        {
            spawnIndexes.Add(-1);
        }
        else if (spawnIndexes.Count < maxSlots && healingCard != null)
        {
            spawnIndexes.Add(-1);
        }
    }

    public void spawnCards()
    {
        foreach(var slot in cardSlots)
        {
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var child = slot.GetChild(i);
                child.DOKill();
                child.localScale = GetCardScale(child);
                child.SetParent(transform); 
                child.gameObject.SetActive(false);
            }
        }

        IndexsForCards();
        int spawnCount = Mathf.Min(spawnIndexes.Count, cardSlots.Count);

        if (spawnCount == 0)
        {
            // Nothing could be drawn; never leave the game paused forever.
            var player = FindFirstObjectByType<Player>();
            if (player != null) player.OnUpgradeChosen();
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject cardToSpawn;

            // -1 means use healing card instead of regular card
            if (spawnIndexes[i] == -1)
            {
                if (healingCard == null)
                {
                    Debug.LogError("Healing card is null but needed for slot " + i);
                    break;
                }
                cardToSpawn = healingCard;
            }
            else
            {
                cardToSpawn = cards[spawnIndexes[i]];
            }

            // Place card in slot
            cardToSpawn.transform.SetParent(cardSlots[i]);
            cardToSpawn.transform.localPosition = Vector3.zero;
            cardToSpawn.transform.localRotation = Quaternion.identity;
            cardToSpawn.transform.DOKill();
            cardToSpawn.transform.localScale = Vector3.zero;
            cardToSpawn.SetActive(true);

            // Pop-in animation: scale 0 -> native scale with Ease.OutBack over 0.25s, staggered by 0.08s
            // Uses SetUpdate(true) because Time.timeScale is 0 during card selection
            float delay = i * 0.08f;
            cardToSpawn.transform.DOScale(GetCardScale(cardToSpawn.transform), 0.25f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }
}
