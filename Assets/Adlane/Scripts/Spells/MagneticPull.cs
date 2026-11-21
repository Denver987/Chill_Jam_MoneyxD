using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.VFX;

public class MagneticPull : MonoBehaviour
{
    public Camera cam;


    [Header("Pull Settings")]
    [SerializeField] private float pullStrength = 10f;
    [SerializeField] public float pullRange = 5f;
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
    public bool isHolding;
    private bool isThrowing = false;
    public float pullCooldownTimer = 0f;


    [Header("Telekinesis")]
    [SerializeField] private float telekinesisDuration = 2f;
    [SerializeField] private float telekinesisHeight = 2f;
    [SerializeField] private float telekinesisUpForce = 12f;
    [SerializeField] private float telekinesisCD = 3f;
    [SerializeField] private Animator animator;
    private float telekinesisTimer = 0f;
    private float telekinesisStartY = 0f; 
    private Rigidbody telekinesisTarget;
    private float cdTimer;
    private bool telekinesisOriginalGravity;
    private bool telekinesisOriginalKinematic;
    private EnnemyMovement telekinesisEnemy;
    private EnnemyMovement heldEnemy;

    void Start()
    {

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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
        if (cam == null) return;

        Ray ray = GetAimRay();
        bool inRange = false;
        Outline outlineTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, pullRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (CanPull(hit.rigidbody) || CanTelekinesis(hit.rigidbody))
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
        if (Time.timeScale == 0f) return; // game frozen (cards, pause, game over)
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (cam == null) cam = Camera.main;
        if (cam == null || !isActiveAndEnabled) return;
        if (telekinesisTarget != null) return;
        if (isHolding && heldItem != null) return; // LMB throws the held object, not a second lift.
        if (telekinesisTimer > 0f) return; 
        if (cdTimer > 0f) return; 
        if (context.performed)
        {
            Ray ray = GetAimRay();
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange, ~0, QueryTriggerInteraction.Ignore))
            {
                TryStartTelekinesis(hit.rigidbody);
            }
        }
        
    }

    public void Pull(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            isHolding = false;
            _suppressAimUI = false;
            ReleaseHeldEnemy();
            heldItem = null;
            if (animator != null) animator.SetBool("Magnetic", false);
            return;
        }
        if (!isActiveAndEnabled) return;
        if (Time.timeScale == 0f) return; // game frozen (cards, pause, game over)
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (pullCooldownTimer > 0f) return;
        if (context.performed)
        {
            _suppressAimUI = true;   // stop Update aim-ui
            ResetAimUI();            // kill outline BEFORE duplication/hold
            isHolding = true;
            animator.SetBool("Magnetic", true);
        }
    }

    public void Throw(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || Time.timeScale == 0f) return;
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (heldItem == null) return;
        if (!isHolding) return;
        if (pullCooldownTimer > 0f) return;
        if (context.performed)
        {
            animator.SetBool("Magnetic", false);
            isHolding = false;
            isThrowing = true;
        }
    }
    private void FixedUpdate()
    {
        if (cam == null) cam = Camera.main;
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
                EndTelekinesis();
            }

        }

        if (isHolding && cam != null && !isThrowing)
        {
            if (pullCooldownTimer > 0f) return;
            if (heldItem == null && Physics.Raycast(GetAimRay(), out RaycastHit hit, pullRange, ~0, QueryTriggerInteraction.Ignore))
            {
                if (CanPull(hit.rigidbody) && hit.rigidbody != telekinesisTarget)
                {
                    heldItem = hit.rigidbody;

                    // prepare duplicate when grabbed
                    var dup = heldItem.GetComponent<DuplicateOnSpellInteract>();
                    if (dup) dup.OnSpellPrepare();

                    if (heldItem.isKinematic) heldItem.isKinematic = false;
                }
            }
            // Keep the acquired object, even after it moves away from the aim ray.
            if (heldItem != null)
            {
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
                        heldItem.MoveRotation(heldItem.rotation * deltaRotation);
                    }
            }
        }
        if (!isHolding && !isThrowing) animator.SetBool("Magnetic", false);
        if (isThrowing && cam != null && heldItem != null)
        {
            isThrowing = false;

            // trigger duplicate activation + delete original on throw
            var dup = heldItem.GetComponent<DuplicateOnSpellInteract>();
            if (dup) dup.OnSpellThrown();

            heldItem.AddForce(cam.transform.forward * throwStrength, ForceMode.Impulse);
            pullCooldownTimer = pullCooldown;
            heldItem.useGravity = true;
            ReleaseHeldEnemy();

            // Reset crosshair + outline so it doesn’t stick to the old target
            CameraSettings.Instance?.SetCrosshairTargetable(false, null);
            _lastInRange = false;
            _lastOutline = null;

            ResetAimUI();         // make 100% sure old outline is off
            _suppressAimUI = false;

            heldItem = null;
        }
    }

    /// <summary>
    /// Drop the current input state without throwing. A level-up freezes the
    /// game while the player may have been holding the spell button, so the
    /// release event is never delivered.
    /// </summary>
    public void CancelInputState()
    {
        isHolding = false;
        isThrowing = false;
        ReleaseHeldEnemy();
        heldItem = null;
        EndTelekinesis();
        _suppressAimUI = false;
        if (animator != null) animator.SetBool("Magnetic", false);
        ResetAimUI();
    }

    public static bool CanPull(Rigidbody body)
    {
        if (body == null || !body.gameObject.activeInHierarchy) return false;
        // Enemies cannot be pulled or held into hands; only pullable stones/objects can be pulled
        var enemy = body.GetComponent<EnnemyMovement>() ?? body.GetComponentInParent<EnnemyMovement>();
        if (enemy != null) return false;
        return body.CompareTag("Pullable");
    }

    public static bool CanTelekinesis(Rigidbody body)
    {
        if (body == null || !body.gameObject.activeInHierarchy) return false;
        var enemy = body.GetComponent<EnnemyMovement>() ?? body.GetComponentInParent<EnnemyMovement>();
        return enemy != null ? !enemy.isDead : body.CompareTag("Pullable");
    }

    public bool TryStartTelekinesis(Rigidbody body)
    {
        if (!CanTelekinesis(body) || telekinesisTarget != null || cdTimer > 0f || heldItem != null) return false;
        telekinesisTarget = body;
        telekinesisOriginalGravity = body.useGravity;
        telekinesisOriginalKinematic = body.isKinematic;
        telekinesisEnemy = body.GetComponent<EnnemyMovement>() ?? body.GetComponentInParent<EnnemyMovement>();
        if (telekinesisEnemy != null) telekinesisEnemy.SetSpellSuspended(true);
        body.isKinematic = false;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        telekinesisStartY = body.position.y;
        telekinesisTimer = telekinesisDuration;
        cdTimer = telekinesisCD;
        _suppressAimUI = true;
        ResetAimUI();
        if (animator != null) animator.SetTrigger("Telekinesis");
        return true;
    }

    private void EndTelekinesis()
    {
        if (telekinesisTarget != null)
        {
            telekinesisTarget.useGravity = telekinesisOriginalGravity;
            telekinesisTarget.isKinematic = telekinesisOriginalKinematic;
        }
        if (telekinesisEnemy != null) telekinesisEnemy.SetSpellSuspended(false);
        telekinesisTarget = null;
        telekinesisEnemy = null;
        telekinesisTimer = 0f;
        _suppressAimUI = false;
    }

    private void ReleaseHeldEnemy()
    {
        if (heldEnemy != null)
        {
            heldEnemy.SetSpellSuspended(false);
            if (heldItem != null)
            {
                heldItem.useGravity = true;
                heldItem.isKinematic = false;
            }
        }
        heldEnemy = null;
    }

    private void OnDisable()
    {
        CancelInputState();
    }

    private Ray GetAimRay()
    {
        return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
}
