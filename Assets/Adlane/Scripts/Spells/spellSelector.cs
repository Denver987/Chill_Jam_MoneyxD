using UnityEngine;
using UnityEngine.InputSystem;

public class SpellSelector : MonoBehaviour
{
    public enum SpellType
    {
        Shockwave,
        MagneticPull,
    }

    public SpellType currentSpell;
    public bool magneticPullEnabled = false;
    private Knockback knockback;
    private MagneticPull magneticPull;
    private void Start()
    {
        if (GameManager.Instance.isTutorial) // Only enable knockback if not in tutorial
        {
            magneticPullEnabled = true;

        }
    }
    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        magneticPull = GetComponent<MagneticPull>();
    }
    public void ShockWave(InputAction.CallbackContext context)
    {
        if (context.performed && !gameObject.GetComponent<Knockback>().isCharging && !gameObject.GetComponent<MagneticPull>().isHolding)
        {
            knockback.enabled = true;
            magneticPull.enabled = false;
         
           
            Debug.Log("Shockwave spell selected");
            currentSpell = SpellType.Shockwave;
        }
    }
    public void MagneticPull(InputAction.CallbackContext context)
    {
        if (!magneticPullEnabled) return;
        if (context.performed && !gameObject.GetComponent<Knockback>().isCharging && !gameObject.GetComponent<MagneticPull>().isHolding)
        {
            knockback.enabled = false;
            magneticPull.enabled = true;
            Debug.Log("Magnetic Pull spell selected");
            currentSpell = SpellType.MagneticPull;
        }
    }

}