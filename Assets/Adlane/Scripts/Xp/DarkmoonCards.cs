using UnityEngine;

public class DarkmoonCards : MonoBehaviour
{
    public bool magneticPull; 
    public int maxHealth;
    public float knockbackForce;
    public float speedBoost;
    public float healthRegeneration;
    public float spawnRateCapsule;
    public float dropRatePotion;
    public float increaseKnockbackRange;
    public float decreasePullCd;
    public float increasePullRange;
    // changing parameters
    private int levelToUnlockPull;

    void Start()
    {

    }

    void Update()
    {

    }
    public void ChooseCard()
    {

    }
}
public class Card
{
    public string name;
    public string description;
    public Sprite icon;
    public DarkmoonCards cardType;
    public bool canSpawn = true;

    public Card(string name, string description, Sprite icon, DarkmoonCards cardType, bool canSpawn)
    {
        this.name = name;
        this.description = description;
        this.icon = icon;
        this.cardType = cardType;
        this.canSpawn = canSpawn;
    }
}
