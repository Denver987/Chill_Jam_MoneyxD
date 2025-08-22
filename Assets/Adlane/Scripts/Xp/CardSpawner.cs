using System.Collections.Generic;
using UnityEngine;




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
    }

    public void IndexsForCards()
    {

        indexesForCards.Clear();
        spawnIndexes.Clear();

        foreach (var card in cards)
        {
            if (card.GetComponent<Cards>().canSpawn && !card.GetComponent<Cards>().CardCap)
            {
                indexesForCards.Add(cards.IndexOf(card));
            }
        }
        if (indexesForCards.Count < 3)
        {
            spawnIndexes.Add(-1);
            return;
        }
        if (indexesForCards.Count == 1)
        {
            spawnIndexes.Add(indexesForCards[0]);
            spawnIndexes.Add(-1);
            return;
        }
        if (indexesForCards.Count == 2)
        {
            spawnIndexes.Add(indexesForCards[0]);
            spawnIndexes.Add(indexesForCards[1]);
            spawnIndexes.Add(-1);
            return;
        }
        int index1 = Random.Range(0, indexesForCards.Count);
        spawnIndexes.Add(indexesForCards[index1]); 
        indexesForCards.RemoveAt(index1);

        int index2 = Random.Range(0, indexesForCards.Count);
        spawnIndexes.Add(indexesForCards[index2]);
        indexesForCards.RemoveAt(index2);

        int index3 = Random.Range(0, indexesForCards.Count);
        spawnIndexes.Add(indexesForCards[index3]);
        indexesForCards.RemoveAt(index3);


        // for (int i = 0; i < nextNumber; i++)
        // {
        //     first = Random.Range(0, cards.Count);
        //     if (cards[first].GetComponent<Cards>().canSpawn && !cards[first].GetComponent<Cards>().CardCap)
        //     {
        //         indexesForCards.Add(first);
        //         break;
        //     }
        //     nextNumber++;
        // }
        // for (int i = 0; i < nextNumber; i++)
        // {
        //     second = Random.Range(0, cards.Count);
        //     if (second != first && cards[second].GetComponent<Cards>().canSpawn && !cards[second].GetComponent<Cards>().CardCap)
        //     {
        //         indexesForCards.Add(second);
        //         break;
        //     }
        //     nextNumber++;
        // }
        // for (int i = 0; i < nextNumber; i++)
        // {
        //     third = Random.Range(0, cards.Count);
        //     if (third != first && third != second && cards[third].GetComponent<Cards>().canSpawn && !cards[third].GetComponent<Cards>().CardCap)
        //     {
        //         indexesForCards.Add(third);
        //         nextNumber = 1;
        //         break;
        //     }
        //     nextNumber++;
        // }
    }
    public void spawnCards()
    {
        foreach(var slot in cardSlots)
        {
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var child = slot.GetChild(i);
                child.SetParent(transform); 
                child.gameObject.SetActive(false);
            }
        }

        IndexsForCards();
        for (int i = 0; i < spawnIndexes.Count; i++)
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
                // Regular card - check index is valid
                if (spawnIndexes[i] < 0 || spawnIndexes[i] >= cards.Count)
                {
                    Debug.LogError($"Invalid card index: {spawnIndexes[i]} at slot {i}");
                    continue;
                }
                cardToSpawn = cards[spawnIndexes[i]];
            }

            // Place card in slot
            cardToSpawn.transform.SetParent(cardSlots[i]);
            cardToSpawn.transform.localPosition = Vector3.zero;
            cardToSpawn.SetActive(true);
        }
    }
}