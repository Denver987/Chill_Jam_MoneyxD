using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    private Rigidbody rb;
    [Header("Movement Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 50f; 
    [SerializeField] private float SpeedMultiplier = 2f;
    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 10f;  // much lower than ground acceleration
    [SerializeField] private float airSpeedLimit = 8f;     // max air speed to prevent infinite acceleration
    [Header("Look Settings")]
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
    private Knockback knockback;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pitchTarget = Camera.main.transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
        knockback = GetComponent<Knockback>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        }
    }

    // Input callbacks
    public void Jump(InputAction.CallbackContext context)
    {
        if (rb == null) return;
        if (grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);          
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
    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveSpeed *= SpeedMultiplier; 
        }
        else if (context.canceled)
        {
            moveSpeed /= SpeedMultiplier;
        }
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

        
        grounded = CheckGrounded();
        if (grounded)
        {
            Movement();
        }
        else if (!grounded)
        {
            MovementInAir();
        }
    }

    private void Movement()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;

        targetVelocity = transform.TransformDirection(targetVelocity);
        Vector3 velocityChange = targetVelocity - currentVelocity;
        velocityChange.y = 0f; 
        Vector3.ClampMagnitude(velocityChange, acceleration);

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
    private void MovementInAir()
{
    if (moveInput.sqrMagnitude < 0.01f) return;


    Vector3 inputDir = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)).normalized;


    Vector3 vel = rb.linearVelocity;
    Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);

    float currentSpeed = Vector3.Dot(horizVel, inputDir); 

    
    float addSpeed = airSpeedLimit - currentSpeed;
    if (addSpeed <= 0) return;


    float accel = airAcceleration * Time.fixedDeltaTime;
    if (accel > addSpeed) accel = addSpeed;


    Vector3 airForce = inputDir * accel;
    rb.AddForce(airForce, ForceMode.VelocityChange); 
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
