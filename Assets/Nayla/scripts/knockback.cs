using System.Collections;
using System.Collections.Generic;
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
            float finalKnockbackStrengthPlayer = finalKnockbackStrengthEnemy * groundMultiplier;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IHitable hitable = hit.transform.GetComponent<IHitable>();
                if (hitable != null)
                {
                    hitable.Execute(transform, finalKnockbackStrengthEnemy);
                    nextAllowedHitTime = Time.time + hitCooldown;
                }
                else
                {
                    if (hit.normal.y >= 0.7f)
                    {
                        
                        if (rb != null)
                        {
                            
                                // horizontal "away" direction from hit point (remove vertical)
                                Vector3 away = rb.position - hit.point;
                               Vector3 awayHoriz = Vector3.ProjectOnPlane(away, Vector3.up);
                                // fallback: if hit point is basically under/at player, use camera ray horizontal
                               if (awayHoriz.sqrMagnitude < 1e-6f)
                                 awayHoriz = Vector3.ProjectOnPlane(-ray.direction, Vector3.up);

                               awayHoriz.Normalize();
                                float b = Mathf.Clamp01(upwardBias);
                               Vector3 combined = (awayHoriz * (1f - b) + hit.normal.normalized * b).normalized;
                              // use VelocityChange so mass doesn't make small pushes feel tiny.
                              rb.AddForce(combined * finalKnockbackStrengthPlayer, ForceMode.VelocityChange);
                                 nextAllowedHitTime = Time.time + hitCooldown;
                        }
                    }
                }
            }
        }
    }
}
