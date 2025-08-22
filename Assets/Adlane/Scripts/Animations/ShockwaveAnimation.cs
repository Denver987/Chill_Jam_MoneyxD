using UnityEngine;

public class ShockwaveAnimation : MonoBehaviour
{
    public Knockback knockbackReference;

    void Start()
    {
        // Try to find the knockback component in parent if not set
        if (knockbackReference == null)
            knockbackReference = GetComponentInParent<Transform>().GetComponentInParent<Knockback>();
    }

    // Animation event will call this
    public void ShockwaveAnimationComplete()
    {
        if (knockbackReference != null)
            knockbackReference.ShockwaveAnimationComplete();
    }
}
