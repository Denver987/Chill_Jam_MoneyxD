using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MagneticPull : MonoBehaviour
{
    private Camera cam;
    [Header("Pull Settings")]
    [SerializeField] private float pullStrength = 10f;
    [SerializeField] private float pullRange = 5f;
    [SerializeField] private float snapSpeed = 5f;
    [SerializeField] private float aimTolerance = 0.5f;
    [SerializeField] private float floatingPosition = 3f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float throwStrength = 10f;
    [SerializeField] private Vector3 rotationAxis = new Vector3(1, 1, 1);
    [Header("Telekinesis")]
    [SerializeField] private float telekinesisDuration = 2f;
    [SerializeField] private float telekinesisHeight = 2f;
    [SerializeField] private float telekinesisUpForce = 12f;
    [SerializeField] private float telekinesisCD = 3f;
    private float telekinesisTimer = 0f;
    private bool isHolding;
    private Rigidbody heldItem;
    private float telekinesisStartY = 0f; 
    private Rigidbody telekinesisTarget;
    private float cdTimer;

    void Start()
    {
        cam = Camera.main;
    }
    public void Telekinesis(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (telekinesisTarget != null) return;
        if (telekinesisTimer > 0f) return; 
        if (cdTimer > 0f) return; 
        if (context.performed)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange))
            {
                if (hit.rigidbody == null) return;
                if (!hit.collider.CompareTag("Pullable")) return;
                telekinesisTarget = hit.rigidbody;
                telekinesisStartY = telekinesisTarget.position.y;

                telekinesisTarget.useGravity = false;
                telekinesisTimer = telekinesisDuration;
                cdTimer = telekinesisCD;

            }
        }
        
    }

    public void Pull(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (context.performed)
        {
            isHolding = true;
        }
        if (isHolding && context.canceled)
        {
            isHolding = false;
        }
    }

    public void Throw(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (heldItem == null) return;
        if (!isHolding) return;
        if (context.performed)
        {
            isHolding = false;
            heldItem.AddForce(cam.transform.forward * throwStrength, ForceMode.Impulse);
        }
    }
    private void FixedUpdate()
    {
        if (cdTimer > 0f) cdTimer -= Time.fixedDeltaTime;
        if (telekinesisTimer > 0f && telekinesisTarget != null)
        {
            float currentHeight = telekinesisTarget.position.y;
            float targetHeight = telekinesisStartY + telekinesisHeight;
            float heightDifference = targetHeight - currentHeight;
            float decelerationZone = 0.5f;
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, rotationAxis);
            telekinesisTarget.MoveRotation(telekinesisTarget.rotation * deltaRotation);

            if (heightDifference > decelerationZone)
            {
                telekinesisTarget.AddForce(Vector3.up * telekinesisUpForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
            else if (heightDifference > 0f)
            {

                float decelerationFactor = heightDifference / decelerationZone;
                float reducedForce = telekinesisUpForce * decelerationFactor;

                Vector3 vel = telekinesisTarget.linearVelocity;
                float dampingFactor = 0.95f;
                telekinesisTarget.linearVelocity = new Vector3(vel.x, vel.y * dampingFactor, vel.z);

                telekinesisTarget.AddForce(Vector3.up * reducedForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
            else
            {
                Vector3 vel = telekinesisTarget.linearVelocity;
                telekinesisTarget.linearVelocity = new Vector3(vel.x, vel.y * 0.9f, vel.z);
            }

            telekinesisTimer -= Time.fixedDeltaTime;

            if (telekinesisTimer <= 0f)
            {
                telekinesisTarget.useGravity = true;
                telekinesisTarget = null;
            }
        }
        
        if (isHolding && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange))
            {
               if (hit.rigidbody != null && hit.collider.CompareTag("Pullable"))
                {
                    heldItem = hit.rigidbody;
                    Vector3 targetPosition = cam.transform.position + cam.transform.forward * floatingPosition;
                    Vector3 direction = targetPosition - heldItem.position;
                    float distance = direction.magnitude;

                    if (distance > aimTolerance)
                    {
                        heldItem.AddForce(direction.normalized * pullStrength * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }
                    else
                    {
                        heldItem.linearVelocity = Vector3.Lerp(heldItem.linearVelocity, Vector3.zero, snapSpeed * Time.fixedDeltaTime);
                        heldItem.MovePosition(Vector3.Lerp(heldItem.position, targetPosition, snapSpeed * Time.fixedDeltaTime * 0.1f));
                        
                        Quaternion deltaRotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, rotationAxis);
                        hit.rigidbody.MoveRotation(hit.rigidbody.rotation * deltaRotation);
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
            if (cam != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cam.transform.position, pullRange);
        }
    }
}
