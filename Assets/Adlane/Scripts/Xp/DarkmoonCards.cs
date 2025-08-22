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
        CanvasSingleton.Instance.GetComponentInChildren<CardSelectionUI>().gameObject.SetActive(true);
        CanvasSingleton.Instance.GetComponentInChildren<CardSelectionUI>().gameObject.SetActive(true);

    }
}

