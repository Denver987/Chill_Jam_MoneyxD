using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionUI : MonoBehaviour
{
    private GameObject cardSpawners;
    public bool choosingCards;
    public float gracePeriod = 0.4f;
    private CanvasGroup canvasGroup;
    private Coroutine graceCoroutine;

    void Awake()
    {
        InitComponents();
    }

    private void InitComponents()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (cardSpawners == null)
        {
            // Include inactive children: the spawner can be disabled while the
            // panel is closed.
            var spawner = GetComponentInChildren<CardsSpawner>(true);
            if (spawner != null) cardSpawners = spawner.gameObject;
        }
    }

    public void SpawnCards() {
        InitComponents();
        if (cardSpawners == null)
        {
            Debug.LogError("CardSelectionUI has no CardsSpawner to spawn cards from.");
            return;
        }

        choosingCards = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ignore clicks during the 0.4s grace period to stop accidental attack-click spam
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        var spawner = cardSpawners.GetComponent<CardsSpawner>();
        if (spawner != null)
        {
            spawner.spawnCards();
            SetCardsInteractable(spawner, false);
        }

        if (graceCoroutine != null) StopCoroutine(graceCoroutine);
        graceCoroutine = StartCoroutine(GracePeriodRoutine(spawner));
    }

    private IEnumerator GracePeriodRoutine(CardsSpawner spawner)
    {
        yield return new WaitForSecondsRealtime(gracePeriod);
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (spawner != null) SetCardsInteractable(spawner, true);
        graceCoroutine = null;
    }

    private void SetCardsInteractable(CardsSpawner spawner, bool interactable)
    {
        if (spawner == null || spawner.cardSlots == null) return;
        foreach (var slot in spawner.cardSlots)
        {
            if (slot == null) continue;
            var buttons = slot.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                btn.interactable = interactable;
            }
        }
    }

    void OnDisable()
    {
        if (graceCoroutine != null)
        {
            StopCoroutine(graceCoroutine);
            graceCoroutine = null;
        }
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }
}
