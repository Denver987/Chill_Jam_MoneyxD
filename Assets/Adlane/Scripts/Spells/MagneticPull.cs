using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.VFX;

public class MagneticPull : MonoBehaviour
{
    public Camera cam;


    [Header("Pull Settings")]
    [SerializeField] private float pullStrength = 10f;
    [SerializeField] private float pullRange = 5f;
    [SerializeField] private float snapSpeed = 5f;
    [SerializeField] private float aimTolerance = 0.5f;
    [SerializeField] private float floatingPosition = 3f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float throwStrength = 10f;
    [SerializeField] private float pullCooldown = 0.5f;
    private SpellSelector _selector;
    private bool _lastInRange; // to avoid retriggering the tween every frame
    private Outline _lastOutline;
    private bool _suppressAimUI; // true while clicking/using the spell



    [SerializeField] private Vector3 rotationAxis = new Vector3(1, 1, 1);
    private Rigidbody heldItem;
    private bool isHolding;
    private bool isThrowing = false;
    private float pullCooldownTimer = 0f;


    [Header("Telekinesis")]
    [SerializeField] private float telekinesisDuration = 2f;
    [SerializeField] private float telekinesisHeight = 2f;
    [SerializeField] private float telekinesisUpForce = 12f;
    [SerializeField] private float telekinesisCD = 3f;
    private float telekinesisTimer = 0f;
    private float telekinesisStartY = 0f; 
    private Rigidbody telekinesisTarget;
    private float cdTimer;

    void Start()
    {
      /*  if (cam == null)
        {
            cam=Camera.main;
        }*/
    }
    private void Awake()
    {
        _selector = GetComponent<SpellSelector>();
    }

    private void ResetAimUI()
    {
        if (_lastOutline) _lastOutline.enabled = false;     // make sure outline is OFF
        CameraSettings.Instance?.SetCrosshairTargetable(false, null);
        _lastInRange = false;
        _lastOutline = null;
    }


    private void Update()
    {
        if (_suppressAimUI) return;  // pause highlight while using the spell

        // Only show aim UI when MagneticPull is active AND not currently using it
        if (_selector == null|| _selector.currentSpell != SpellSelector.SpellType.MagneticPull|| _suppressAimUI)
        {
            if (_lastInRange || _lastOutline) ResetAimUI();
            return;
        }

        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        bool inRange = false;
        Outline outlineTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, pullRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody != null && hit.collider.CompareTag("Pullable"))
            {
                inRange = true;
                outlineTarget =
                      hit.transform.GetComponent<Outline>()
                   ?? hit.transform.GetComponentInParent<Outline>()
                   ?? hit.transform.GetComponentInChildren<Outline>();
            }
        }

        // update when state OR target changed (fixes clone swap)
        if (inRange != _lastInRange || outlineTarget != _lastOutline)
        {
            CameraSettings.Instance?.SetCrosshairTargetable(inRange, outlineTarget);
            _lastInRange = inRange;
            _lastOutline = outlineTarget;
        }
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

                _suppressAimUI = true;   // 🔹 stop Update() from re-highlighting
                ResetAimUI();            // 🔹 ensure outline/crosshair are OFF before TK starts

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
        if (pullCooldownTimer > 0f) return;
        if (context.performed)
        {
            _suppressAimUI = true;   // stop Update aim-ui
            ResetAimUI();            // kill outline BEFORE duplication/hold
            isHolding = true;
        }
        if (isHolding && context.canceled)
        {
            isHolding = false;
            _suppressAimUI = false;  // allow aim-ui again
        }
    }

    public void Throw(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (heldItem == null) return;
        if (!isHolding) return;
        if (pullCooldownTimer > 0f) return;
        if (context.performed)
        {
            isHolding = false;
            isThrowing = true;
        }
    }
    private void FixedUpdate()
    {
        if (cdTimer > 0f) cdTimer -= Time.fixedDeltaTime;
        if (pullCooldownTimer > 0f) pullCooldownTimer -= Time.fixedDeltaTime;

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
                _suppressAimUI = false;   // ✅ allow Update() highlight again
            }

        }

        if (isHolding && cam != null && !isThrowing)
        {
            if (pullCooldownTimer > 0f) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange))
            {
                if (hit.rigidbody != null && hit.collider.CompareTag("Pullable"))
                {
                    heldItem = hit.rigidbody;

                    // prepare duplicate when grabbed
                    var dup = heldItem.GetComponent<DuplicateOnSpellInteract>();
                    if (dup) dup.OnSpellPrepare();

                    if (heldItem.isKinematic) heldItem.isKinematic = false;
                    Vector3 targetPosition = cam.transform.position + cam.transform.forward * floatingPosition;
                    Vector3 direction = targetPosition - heldItem.position;
                    float distance = direction.magnitude;


                    if (distance > aimTolerance)
                    {
                        heldItem.AddForce(direction.normalized * pullStrength * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }
                    else
                    {
                        // instead of direct MovePosition, use physics-based approach
                        Vector3 toTarget = targetPosition - heldItem.position;

                        // apply gentle force toward target instead of teleporting
                        heldItem.AddForce(toTarget * snapSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);

                        heldItem.linearVelocity *= 0.75f;

                        Quaternion deltaRotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, rotationAxis);
                        hit.rigidbody.MoveRotation(hit.rigidbody.rotation * deltaRotation);
                    }
                }
            }
        }

        if (isThrowing && cam != null && heldItem != null)
        {
            isThrowing = false;

            // trigger duplicate activation + delete original on throw
            var dup = heldItem.GetComponent<DuplicateOnSpellInteract>();
            if (dup) dup.OnSpellThrown();

            heldItem.AddForce(cam.transform.forward * throwStrength, ForceMode.Impulse);
            pullCooldownTimer = pullCooldown;
            heldItem.useGravity = true;

            // Reset crosshair + outline so it doesn’t stick to the old target
            CameraSettings.Instance?.SetCrosshairTargetable(false, null);
            _lastInRange = false;
            _lastOutline = null;

            ResetAimUI();         // make 100% sure old outline is off
            _suppressAimUI = false;

            heldItem = null;
        }
    }

   
}
