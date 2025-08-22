using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CanvasSingleton : MonoBehaviour
{
    public static CanvasSingleton Instance { get; private set; }
    public Knockback playerKnockback;
    public Slider chargeSlider;
    public SpellSelector spellSelector;
    public Player player;
    public Slider healthSlider; // Reference to the health slider
    private RectTransform healthBarRect;
    public GameObject die;
    public GameObject PausePanel;
    public GameObject Hand;
    public GameObject CardsSelection;
    public Transform xpBar;

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        if (playerKnockback == null) playerKnockback = FindFirstObjectByType<Knockback>();
        if (chargeSlider == null) chargeSlider = GameObject.Find("ChargeSlider").GetComponent<Slider>();

        if (spellSelector == null) spellSelector = FindFirstObjectByType<SpellSelector>();
        if (player == null) player = FindFirstObjectByType<Player>();
        if (healthBarRect == null) healthBarRect = GameObject.Find("HealthBar").GetComponent<RectTransform>();
        if (healthSlider == null) healthSlider = healthBarRect.gameObject.GetComponent<Slider>();
    }
    void Start()
    {
        if (player != null)
        {
            UpdateHealthBarSize();
        }
    }
    public IEnumerator CardsUI() {
        Instance.CardsSelection.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Wait a frame to ensure the UI is active
        CardsSelection.GetComponent<CardSelectionUI>().SpawnCards();
    }
    
    public float GetPlayerCharge()
    {
        if (spellSelector.currentSpell != SpellSelector.SpellType.Shockwave && spellSelector != null)
        {
            return 0f;
        }
        return playerKnockback != null ? playerKnockback.ChargeNormalized : 0f;
    }
    public float GetPlayerHealth()
    {
        return player.health; 
    }

    // Update is called once per frame
    void Update()
    {
        if (chargeSlider != null)
        {
            chargeSlider.value = GetPlayerCharge();
        }
        if (healthSlider != null && player != null)
        {
            UpdateHealthBarSize();
            healthSlider.value = player.health;
        }
        if (xpBar != null && player != null)
        {
            UpdateXpBarSize();
        }
    }
    void UpdateHealthBarSize()
    {
        if (healthBarRect == null || player == null) return;
        
        float newWidth = player.maxHealth * 2f; 
        
        Vector2 sizeDelta = healthBarRect.sizeDelta;
        sizeDelta.x = newWidth;
        healthBarRect.sizeDelta = sizeDelta;

        healthSlider.maxValue = player.maxHealth;
    }
    void UpdateXpBarSize()
    {
        if (xpBar == null || player == null) return;
        xpBar.GetComponent<Slider>().maxValue = player.experienceToNextLevel;
        xpBar.GetComponent<Slider>().value = player.experience;
    }
    public void showPausePanel()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(true);
            chargeSlider.gameObject.SetActive(false);
            healthBarRect.gameObject.SetActive(false);
            Hand.SetActive(false);
        }
    }
    public void HidePausePanel()
    {
        if (PausePanel != null)
        {
            PausePanel.SetActive(false);
            chargeSlider.gameObject.SetActive(true);
            healthBarRect.gameObject.SetActive(true);
            Hand.SetActive(true);
        }
    }

    void OnDestroy()
    {
        // Clean up singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
