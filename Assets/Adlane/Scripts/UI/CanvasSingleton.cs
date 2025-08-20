using UnityEngine;
using UnityEngine.UI;

public class CanvasSingleton : MonoBehaviour
{
    public static CanvasSingleton Instance { get; private set; }
    public Knockback playerKnockback;
    public Slider chargeSlider;
    public SpellSelector spellSelector;
    public Player player;
    public Slider healthSlider; // Reference to the health slider
    public float healthAnimationSpeed = 5f; 
    private float targetHealth;
    private float currentDisplayedHealth;
    private RectTransform healthBarRect;
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keeps canvas across scenes
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
            targetHealth = player.health;
            currentDisplayedHealth = player.health;
            UpdateHealthBarSize();
        }
    }

    
    public float GetPlayerCharge()
    {
        if (spellSelector.currentSpell != SpellSelector.SpellType.Shockwave)
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

    void OnDestroy()
    {
        // Clean up singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
