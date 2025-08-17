using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 50f; 
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private float groundCheckDistance = 0.15f; // how far below pivot to check
    [SerializeField] private float maxGroundAngle = 45f; // slope limit
    private bool grounded;

    private Transform pitchTarget; // camera only
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

    // Input callbacks
    public void Jump(InputAction.CallbackContext context)
    {
        if (rb == null) return;
        if (grounded)
        {
            if (context.performed)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
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

    private bool CheckGrounded()
    {
        // origin slightly above pivot to avoid starting inside geometry
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float castDist = groundCheckDistance + groundCheckRadius;
        if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            // check slope angle
            return Vector3.Angle(hit.normal, Vector3.up) <= maxGroundAngle;
        }
        return false;
    }

    // Physics movement
    private void FixedUpdate()
    {
        if (rb == null) return;

        // update grounded state each physics step
        grounded = CheckGrounded();

        Vector3 targetVelocity = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * moveSpeed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 velocityDiff = targetVelocity - currentVelocity;
        Vector3 accelerationForce = Vector3.ClampMagnitude(velocityDiff, acceleration * Time.fixedDeltaTime);

        rb.AddForce(accelerationForce, ForceMode.VelocityChange);
    }
    


    private void LateUpdate()
    {
        pitch = Mathf.Clamp(pitch - lookInput.y * lookSpeed * Time.deltaTime, minPitch, maxPitch);
        pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        yaw += lookInput.x * lookSpeed * Time.deltaTime;
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(targetRotation);
    }

    // optional: debug visualization in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
