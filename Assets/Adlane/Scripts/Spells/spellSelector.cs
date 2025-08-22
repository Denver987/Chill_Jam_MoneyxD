using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
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
    public GameObject knockbackIcon;
    public GameObject magneticPullIcon;
    public GameObject knockbackHighlited;
    public GameObject magneticPullHighlited;

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
            knockbackHighlited.SetActive(true);
            magneticPullHighlited.SetActive(false);
            magneticPullIcon.SetActive(true);
            knockbackIcon.SetActive(false);
            if (GameManager.Instance.isTutorial == false)
            {
                magneticPull.enabled = false;
            }
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
            knockbackHighlited.SetActive(false);
            magneticPullHighlited.SetActive(true);
            magneticPullIcon.SetActive(false);
            knockbackIcon.SetActive(true);
            Debug.Log("Magnetic Pull spell selected");
            currentSpell = SpellType.MagneticPull;
        }
    }

}