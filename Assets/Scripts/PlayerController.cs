using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private bool useDepthMovement = false;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody rb;
    private float moveInput;
    private float depthInput;
    private bool jumpPressed;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
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

        if (useDepthMovement)
        {
            velocity.x = moveInput * moveSpeed;
            velocity.z = depthInput * moveSpeed;
        }
        else
        {
            velocity.x = moveInput * moveSpeed;
            velocity.z = 0f;
        }

        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
        }

        rb.velocity = velocity;
        jumpPressed = false;
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
        Transform target = visualRoot != null ? visualRoot : transform;

        if (moveInput > 0.01f)
        {
            Vector3 scale = target.localScale;
            scale.x = Mathf.Abs(scale.x);
            target.localScale = scale;
        }
        else if (moveInput < -0.01f)
        {
            Vector3 scale = target.localScale;
            scale.x = -Mathf.Abs(scale.x);
            target.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * groundCheckDistance);
    }
}
