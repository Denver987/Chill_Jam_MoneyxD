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

    public void ShockWave(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Shockwave spell selected");
            currentSpell = SpellType.Shockwave;
        }
    }
    public void MagneticPull(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Magnetic Pull spell selected");
            currentSpell = SpellType.MagneticPull;
        }
    }

}