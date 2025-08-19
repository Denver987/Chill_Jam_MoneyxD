using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float moveSpeed = 7f;

    [Header("Acceleration")]
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 70f;
    [SerializeField] private float airAcceleration = 20f;

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

    [Header("Gravity Tweaks")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float riseGravityMultiplier = 1.5f;

    private Transform pitchTarget;
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pitchTarget = Camera.main.transform;

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

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed || context.started)
            moveInput = context.ReadValue<Vector2>().normalized;
        else if (context.canceled)
            moveInput = Vector2.zero;
    }

    public void Look(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
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

        Vector3 accelerationForce = Vector3.ClampMagnitude(velocityDiff, accelRate * Time.fixedDeltaTime);
        rb.AddForce(accelerationForce, ForceMode.VelocityChange);

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

    public void TakeKnockback(Vector3 direction, float strength, float upwardBias = 0.5f, float recoveryTime = 0.2f)
    {
        if (rb == null) return;

        StopAllCoroutines(); // cancel previous knockbacks
        StartCoroutine(KnockbackRoutine(direction, strength, upwardBias, recoveryTime));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float strength, float upwardBias, float recoveryTime)
    {
        // Save original drag
        float originalDrag = rb.linearDamping;

        // Reduce drag for smooth flight
        rb.linearDamping = 0f;

        // Calculate knockback velocity
        Vector3 knockDir = direction.normalized * strength;
        knockDir.y += upwardBias * strength/5 * 0.1f; // upward punch (tweak multiplier)
        knockDir.y = 0.5f;
        // Apply directly as velocity override
        rb.linearVelocity = knockDir;

        // Wait before restoring drag
        yield return new WaitForSeconds(recoveryTime);

        rb.linearDamping = originalDrag;
    }


    // --------------------
    // CAMERA ROTATION
    // --------------------
    private void LateUpdate()
    {
        pitch = Mathf.Clamp(pitch - lookInput.y * lookSpeed * Time.deltaTime, minPitch, maxPitch);
        pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);

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
