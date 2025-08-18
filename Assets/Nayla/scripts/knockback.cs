
using UnityEngine;
using UnityEngine.InputSystem;

public class Knockback : MonoBehaviour
{
    private Camera cam;

    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float rayRange = 5f;
    [SerializeField] private float initialKnockbackStrength = 10f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float maxKnockbackStrength = 20f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardModifier = 1f; 
    [SerializeField] private LayerMask affectedLayers = 0; 
    
    [SerializeField] private AnimationCurve chargeCurve;
    private float nextAllowedHitTime = 0f;

    private float chargeStartTime = -1f;
    private bool isCharging = false;


    private void Awake()
    {
        cam = Camera.main;

    }

    public void knockback(InputAction.CallbackContext context)
    {
        if (Time.time < nextAllowedHitTime) return;
        if (context.phase == InputActionPhase.Started)
        {
            chargeStartTime = Time.time;
        }

        if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
        {
            isCharging = true;
            return;
        }

        if (context.phase == InputActionPhase.Canceled && isCharging)
        {
            float held = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.0001f, maxChargeTime));
            float t = chargeCurve.Evaluate(held);
            float finalKnockbackStrength = Mathf.Lerp(initialKnockbackStrength, maxKnockbackStrength, t);

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, rayRange))
            {
                CreateExplosion(hit.point, finalKnockbackStrength);
                nextAllowedHitTime = Time.time + hitCooldown;
            }
        }
    }
    private void CreateExplosion(Vector3 explosionPoint, float strength)
    {
        Collider[] colliders = Physics.OverlapSphere(explosionPoint, explosionRadius, affectedLayers);
        foreach (Collider col in colliders)
        {
            Rigidbody targetRb = col.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                
                targetRb.AddExplosionForce(strength, explosionPoint, explosionRadius, upwardModifier, ForceMode.VelocityChange);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (cam != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(cam.transform.position, cam.transform.forward * rayRange);
        }
        
    }
}
