using SmallHedge.SoundManager;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Knockback : MonoBehaviour
{
    private Camera cam;

    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float initialKnockbackStrength = 10f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float maxKnockbackStrength = 20f;
    [SerializeField] private float groundMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.6f;
    [SerializeField] private AnimationCurve chargeCurve;
    public float jumpForceKnock = 5f; // force applied when jumping

    public float maxDistance = 100f; // maximum distance to hit something   
    public float upwardForce = 0.5f; // how much upward force to apply when hitting the ground
    private float nextAllowedHitTime = 0f;
    private float chargeStartTime = -1f;
    private bool isCharging = false;
    private Rigidbody rb;

    private void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    public void knockback(InputAction.CallbackContext context)
    {
        if (Time.time < nextAllowedHitTime) return;

        if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            return;
        }

        if (context.phase == InputActionPhase.Canceled && isCharging)
        {
            float held = Mathf.Clamp01((Time.time - chargeStartTime) / Mathf.Max(0.0001f, maxChargeTime));
            float t = chargeCurve.Evaluate(held);
            float finalKnockbackStrengthEnemy = Mathf.Lerp(initialKnockbackStrength, maxKnockbackStrength, t);
            float finalKnockbackStrengthPlayer = jumpForceKnock * groundMultiplier;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit,maxDistance))
            {
                IHitable hitable = hit.transform.GetComponent<IHitable>();
                if (hitable != null)
                {
                    SoundManager.PlaySound(SoundType.spell1, this.GetComponent<AudioSource>(), 0.5f); // Play knockback sound with volume 0.5
                    hitable.ExecuteWithUpwardBias(transform, finalKnockbackStrengthEnemy, upwardForce);
                    nextAllowedHitTime = Time.time + hitCooldown;
                }
                else
                {
                    // Ground check: surface normal is basically "up"
                    if (hit.normal.y >= 0.95f) // ~perfectly flat ground
                    {
                        if (rb != null)
                        {
                            // Always go straight up
                            rb.AddForce(Vector3.up * finalKnockbackStrengthPlayer, ForceMode.VelocityChange);
                            nextAllowedHitTime = Time.time + hitCooldown;
                        }
                    }
                }
            }
        }
    }
}

