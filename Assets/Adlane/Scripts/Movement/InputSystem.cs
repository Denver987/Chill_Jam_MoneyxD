using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    private Rigidbody rb;
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;

    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 7f;
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 70f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float SprintMultiplier = 2f;

    [Header("Look")]
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float maxGroundAngle = 45f;
    private bool grounded;
    private float externalImpulseUntil;

    [Header("Gravity Tweaks")]
    [SerializeField] private float fallMultiplier = 3.5f;
    [SerializeField] private float riseGravityMultiplier = 1.25f;

    private Transform pitchTarget;
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;

    private float yaw;
    private float pitch;
    private float currentRoll;
    private bool paused = false;

    public bool IsPaused => paused;

    // debounce lock to ignore leftover input when switching maps
    private bool pauseInputLocked = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pitchTarget = Camera.main != null ? Camera.main.transform : transform;

        Cursor.lockState = CursorLockMode.Locked;

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    // --------------------
    // INPUTS
    // --------------------
    public void Jump(InputAction.CallbackContext context)
    {
        if (rb == null) return;
        if (context.performed && grounded)
        {
            // reset vertical velocity for consistent jump feel
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }
    public void Pause(InputAction.CallbackContext context)
    {
        // only react on button-down (avoid performed/release confusion)
        if (!context.started) return;

        // prevent immediate reentry while switching maps / changing cursor
        if (pauseInputLocked) return;

        // guard: if card selection active don't pause
        var canvas = CanvasSingleton.Instance;
        if (canvas != null && canvas.CardsSelection != null)
        {
            var cs = canvas.CardsSelection.GetComponent<CardSelectionUI>();
            if (cs != null && cs.choosingCards) return;
        }

        // guard: the game may already be frozen (level-up card screen still
        // opening, game-over screen). Pausing on top of it would fight over
        // Time.timeScale.
        if (!paused && Time.timeScale == 0f) return;

        StartCoroutine(HandlePauseToggle());
    }

    private IEnumerator HandlePauseToggle()
    {
        pauseInputLocked = true;

        var playerInput = gameObject.GetComponent<PlayerInput>();

        if (!paused)
        {
            // Pause
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            paused = true;
            if (CanvasSingleton.Instance != null) CanvasSingleton.Instance.showPausePanel();
            if (playerInput != null) playerInput.SwitchCurrentActionMap("UI");
        }
        else
        {
            // Resume
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            paused = false;
            if (CanvasSingleton.Instance != null) CanvasSingleton.Instance.HidePausePanel();
            if (playerInput != null) playerInput.SwitchCurrentActionMap("Player");
        }

        // wait one frame then a tiny realtime delay to consume any leftover event
        yield return null;
        yield return new WaitForSecondsRealtime(0.05f);

        pauseInputLocked = false;
    }

    // Add this public helper so UI buttons can resume cleanly
    public void ResumeFromUI()
    {
        // if already resumed do nothing
        if (!paused) return;

        // stop any running pause coroutine to avoid races
        StopAllCoroutines();

        var playerInput = gameObject.GetComponent<PlayerInput>();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        paused = false;
        pauseInputLocked = false;

        if (CanvasSingleton.Instance != null) CanvasSingleton.Instance.HidePausePanel();
        if (playerInput != null) playerInput.SwitchCurrentActionMap("Player");
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed || context.started) moveInput = context.ReadValue<Vector2>().normalized;
        else if (context.canceled) moveInput = Vector2.zero;
    }

    public void Look(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }
    
    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveSpeed *= SprintMultiplier; // Increase speed when sprinting
        }
        else if (context.canceled)
        {
            moveSpeed /= SprintMultiplier; // Reset speed when not sprinting
        }
    }
    // --------------------
    // GROUND CHECK
    // --------------------
    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float castDist = groundCheckDistance + groundCheckRadius;
        if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            return Vector3.Angle(hit.normal, Vector3.up) <= maxGroundAngle;
        }
        return false;
    }

    // --------------------
    // MOVEMENT
    // --------------------
    private void FixedUpdate()
    {
        if (rb == null) return;

        grounded = CheckGrounded();

        Vector3 targetVelocity = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * moveSpeed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 velocityDiff = targetVelocity - currentVelocity;

        float accelRate;
        if (grounded)
        {
            // If moving input exists → accelerate, otherwise decelerate quickly
            accelRate = (moveInput.sqrMagnitude > 0.01f) ? groundAcceleration : groundDeceleration;
        }
        else
        {
            accelRate = airAcceleration;
        }

        if (Time.time >= externalImpulseUntil)
        {
            Vector3 accelerationForce = Vector3.ClampMagnitude(velocityDiff, accelRate * Time.fixedDeltaTime);
            rb.AddForce(accelerationForce, ForceMode.VelocityChange);
        }

        // Apply custom gravity when airborne
        if (!grounded)
        {
            if (rb.linearVelocity.y < 0) // falling
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (fallMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
            else if (rb.linearVelocity.y > 0) // rising
            {
                rb.AddForce(Vector3.down * (Mathf.Abs(Physics.gravity.y) * (riseGravityMultiplier - 1) * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
        }
    }

    public void TakeKnockback(Vector3 direction, float strength, float upwardBias = 0.3f, float recoveryTime = 0.2f)
    {
        Vector3 horiz = new Vector3(direction.x, 0f, direction.z);
        if (horiz.sqrMagnitude > 0.001f) horiz.Normalize();
        else horiz = -transform.forward;

        Vector3 velocityChange = horiz * strength;
        velocityChange.y = upwardBias;
        ApplyExternalImpulse(velocityChange, recoveryTime);
    }

    public void ApplyExternalImpulse(Vector3 velocityChange, float recoveryTime = 0.3f)
    {
        if (rb == null) return;
        externalImpulseUntil = Mathf.Max(externalImpulseUntil, Time.time + recoveryTime);
        rb.WakeUp();
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }


    private void LateUpdate()
    {
        pitch = Mathf.Clamp(pitch - lookInput.y * lookSpeed * Time.deltaTime, minPitch, maxPitch);
        float targetRoll = -moveInput.x * 0.4f;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 10f);
        pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);

        yaw += lookInput.x * lookSpeed * Time.deltaTime;
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(targetRotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
