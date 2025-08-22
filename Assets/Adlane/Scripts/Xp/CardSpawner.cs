using System.Collections.Generic;
using UnityEngine;




public class CardsSpawner : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    private List<int> indexesForCards = new List<int>();
    private int nextNumber = 1;
    public int first;
    public int second;
    public int third;
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
        for (int i = 0; i < nextNumber; i++)
        {
            first = Random.Range(0, cards.Count);
            if (cards[first].GetComponent<Cards>().canSpawn && !cards[first].GetComponent<Cards>().CardCap)
            {
                indexesForCards.Add(first);
                break;
            }
            nextNumber++;
        }
        for (int i = 0; i < nextNumber; i++)
        {
            second = Random.Range(0, cards.Count);
            if (second != first && cards[second].GetComponent<Cards>().canSpawn && !cards[second].GetComponent<Cards>().CardCap)
            {
                indexesForCards.Add(second);
                break;
            }
            nextNumber++;
        }
        for (int i = 0; i < nextNumber; i++)
        {
            third = Random.Range(0, cards.Count);
            if (third != first && third != second && cards[third].GetComponent<Cards>().canSpawn && !cards[third].GetComponent<Cards>().CardCap)
            {
                indexesForCards.Add(third);
                nextNumber = 1;
                break;
            }
            nextNumber++;
        }
    }
    public void spawnCards()
    {
        IndexsForCards();
        cards[indexesForCards[0]].transform.SetParent(cardSlots[0]);
        cards[indexesForCards[0]].transform.localPosition = Vector3.zero; // Reset position to avoid overlap
        cards[indexesForCards[0]].gameObject.SetActive(true);
        cards[indexesForCards[1]].transform.SetParent(cardSlots[1]);
        cards[indexesForCards[1]].transform.localPosition = Vector3.zero; // Reset position to avoid overlap
        cards[indexesForCards[1]].gameObject.SetActive(true);
        cards[indexesForCards[2]].transform.SetParent(cardSlots[2]);
        cards[indexesForCards[2]].transform.localPosition = Vector3.zero; // Reset position to avoid overlap
        cards[indexesForCards[2]].gameObject.SetActive(true);
    }
}