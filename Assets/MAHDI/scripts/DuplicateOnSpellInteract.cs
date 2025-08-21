// DuplicateOnSpellInteract.cs
using UnityEngine;

public class DuplicateOnSpellInteract : MonoBehaviour
{
    [Header("Clone activation")]
    [Tooltip("Delay AFTER the throw before the prepared clone becomes active.")]
    public float delayAfterThrow = 2f;

    [Tooltip("Remove this script from the clone so clones don't keep duplicating later.")]
    public bool stripOnClone = true;

    [Tooltip("Only prepare one clone per grab/hold.")]
    public bool onlyOncePerGrab = true;

    [Header("Original deletion on throw")]
    [Tooltip("Delete the original object when it is thrown.")]
    public bool destroyOriginalOnThrow = true;

    [Tooltip("Delay before deleting the original (0 = instant on throw).")]
    public float destroyOriginalDelay = 0f;

    private bool preparedThisGrab;

    /// Call when the spell STARTS interacting/holding this object.
    public void OnSpellPrepare()
    {
        if (onlyOncePerGrab && preparedThisGrab) return;
        preparedThisGrab = true;
        DuplicatorManager.PrepareClone(gameObject, stripOnClone);
    }

    /// Call when the spell THROWS this object (after applying your impulse).
    public void OnSpellThrown()
    {
        // start the timer that will ENABLE the prepared clone
        DuplicatorManager.BeginActivationAfterThrow(gameObject, delayAfterThrow);

        // delete the original (now-thrown) object
        if (destroyOriginalOnThrow)
            Destroy(gameObject, Mathf.Max(0f, destroyOriginalDelay));

        preparedThisGrab = false; // allow a new cycle on next grab
    }

    /// Call if you DROP (not throw) and want to cancel the prepared clone.
    public void OnSpellCancelPrepared(bool destroyPrepared = true)
    {
        DuplicatorManager.CancelPrepared(gameObject, destroyPrepared);
        preparedThisGrab = false;
    }
}
