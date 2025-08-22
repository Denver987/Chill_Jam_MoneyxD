using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    private GameObject cardSpawners;

    void Awake()
    {
        if (cardSpawners == null) cardSpawners = gameObject.GetComponentInChildren<CardsSpawner>().gameObject;
    }
    public void SpawnCards() {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cardSpawners.GetComponent<CardsSpawner>().spawnCards();
    }

}