using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private bool useDepthMovement = false;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float wallCheckDistance = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody rb;
    private Collider bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private float depthInput;
    private bool jumpPressed;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        depthInput = useDepthMovement ? Input.GetAxisRaw("Vertical") : 0f;

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }

        UpdateGroundedState();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 targetPlanarVelocity = useDepthMovement
            ? new Vector3(moveInput, 0f, depthInput) * moveSpeed
            : new Vector3(moveInput * moveSpeed, 0f, 0f);
        Vector3 currentPlanarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float acceleration = isGrounded ? groundAcceleration : airAcceleration;

        currentPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetPlanarVelocity,
            acceleration * Time.fixedDeltaTime
        );

        currentPlanarVelocity = ResolveWallCollision(currentPlanarVelocity);

        velocity.x = currentPlanarVelocity.x;
        velocity.z = currentPlanarVelocity.z;

        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
        }

        rb.velocity = velocity;
        jumpPressed = false;
    }

    private Vector3 ResolveWallCollision(Vector3 planarVelocity)
    {
        if (bodyCollider == null || planarVelocity.sqrMagnitude <= 0.0001f)
        {
            return planarVelocity;
        }

        Vector3 direction = planarVelocity.normalized;
        float sweepDistance = wallCheckDistance + planarVelocity.magnitude * Time.fixedDeltaTime;

        if (!rb.SweepTest(direction, out RaycastHit hit, sweepDistance, QueryTriggerInteraction.Ignore))
        {
            return planarVelocity;
        }

        Vector3 adjustedVelocity = Vector3.ProjectOnPlane(planarVelocity, hit.normal);

        if (!useDepthMovement)
        {
            adjustedVelocity.z = 0f;
        }

        return adjustedVelocity;
    }

    private void UpdateGroundedState()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (!isGrounded)
        {
            isGrounded = Physics.Raycast(
                checkPosition,
                Vector3.down,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    private void UpdateFacing()
    {
        if (moveInput > 0.01f)
        {
            SetFacingRight(true);
        }
        else if (moveInput < -0.01f)
        {
            SetFacingRight(false);
        }
    }

    private void SetFacingRight(bool facingRight)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
            return;
        }

        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        visualRoot.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * groundCheckDistance);
    }
}
