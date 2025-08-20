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
    private Knockback knockback;
    private MagneticPull magneticPull;

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        magneticPull = GetComponent<MagneticPull>();
    }
    public void ShockWave(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            knockback.enabled = true;
            magneticPull.enabled = false;
            Debug.Log("Shockwave spell selected");
            currentSpell = SpellType.Shockwave;
        }
    }
    public void MagneticPull(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            knockback.enabled = false;
            magneticPull.enabled = true;
            Debug.Log("Magnetic Pull spell selected");
            currentSpell = SpellType.MagneticPull;
        }
    }

}