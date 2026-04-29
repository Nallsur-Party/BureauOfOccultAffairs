using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerProfile))]
public class PlayerController : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int SpeedXHash = Animator.StringToHash("speedX");
    private static readonly int SpeedZHash = Animator.StringToHash("speedZ");
    private static readonly int IsMovingForwardHash = Animator.StringToHash("isMovingForward");
    private static readonly int IsMovingBackwardHash = Animator.StringToHash("isMovingBackward");
    private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private bool useDepthMovement = false;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float wallCheckDistance = 0.15f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private GameObject interactionPrompt;

    private Rigidbody rb;
    private PlayerProfile playerProfile;
    private Collider bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private float depthInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isSprinting;
    private NpcOrderVisitor currentInteractableNpc;
    private NpcOrderVisitor activeDialogueNpc;
    private NPCSpawner npcSpawner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerProfile = GetComponent<PlayerProfile>();
        bodyCollider = GetComponent<Collider>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        npcSpawner = FindObjectOfType<NPCSpawner>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        SetInteractionPromptVisible(false);
    }

    private void Update()
    {
        ReadMovementInput();
        currentInteractableNpc = FindNearestInteractableNpc();

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }

        HandleDialogueInput();
        HandleDebugNpcCommands();

        UpdateGroundedState();
        UpdateFacing();
        UpdateAnimator();
        SetInteractionPromptVisible(currentInteractableNpc != null);

        if (currentInteractableNpc == null)
        {
            activeDialogueNpc = null;
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 moveDirection = useDepthMovement
            ? new Vector3(moveInput, 0f, depthInput)
            : new Vector3(moveInput, 0f, 0f);
        
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
        }
        
        Vector3 targetPlanarVelocity = moveDirection * (isSprinting ? moveSpeed * sprintMultiplier : moveSpeed);
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

    private void ReadMovementInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        depthInput = useDepthMovement ? Input.GetAxisRaw("Vertical") : 0f;
        isSprinting = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleDialogueInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentInteractableNpc != null)
        {
            StartNpcConversation(currentInteractableNpc);
        }

        if (activeDialogueNpc == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AskNpcQuestion(NPCQuestionType.Name);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AskNpcQuestion(NPCQuestionType.Gender);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AskNpcQuestion(NPCQuestionType.Age);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AskNpcQuestion(NPCQuestionType.AnotherStory);
        }
    }

    private void HandleDebugNpcCommands()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SendNpcToExit("Z");
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            SendNpcToExit("N");
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnNPC();
        }
    }

    private void SpawnNPC()
    {
        if (npcSpawner != null)
        {
            npcSpawner.SpawnNPC();
        }
        else
        {
            Debug.LogWarning("NPCSpawner not found in scene!");
        }
    }

    private void SendNpcToExit(string exitName)
    {
        NpcOrderVisitor[] allNpcs = FindObjectsOfType<NpcOrderVisitor>();

        foreach (NpcOrderVisitor npc in allNpcs)
        {
            if (npc.IsWaitingAtCounter)
            {
                npc.LeaveThroughExitByName(exitName);
                Debug.Log($"Sending NPC {npc.gameObject.name} to exit {exitName}");
                return;
            }
        }

        Debug.Log($"No NPC waiting at counter to send to exit {exitName}");
    }

    private void StartNpcConversation(NpcOrderVisitor npc)
    {
        activeDialogueNpc = npc;
        npc.Interact();
        npc.ShowDialogue(npc.GetInteractionText());
    }

    private void AskNpcQuestion(NPCQuestionType questionType)
    {
        if (activeDialogueNpc == null)
        {
            return;
        }

        activeDialogueNpc.ShowDialogue(activeDialogueNpc.GetQuestionResponse(questionType, playerProfile));
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 velocity = rb != null ? rb.velocity : Vector3.zero;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        float speedX = Mathf.Abs(localVelocity.x);
        float speedZ = localVelocity.z;
        bool isMovingForward = speedZ >= speedX;
        bool isMovingBackward = speedZ <= -speedX;
        bool isRunning = planarSpeed > moveSpeed + 0.1f;
        animator.SetFloat(SpeedHash, planarSpeed);
        animator.SetFloat(SpeedXHash, speedX);
        animator.SetFloat(SpeedZHash, speedZ);
        animator.SetBool(IsMovingForwardHash, isMovingForward);
        animator.SetBool(IsMovingBackwardHash, isMovingBackward);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetFloat(VerticalSpeedHash, velocity.y);
        animator.SetBool(IsRunningHash, isRunning);
    }

    private void SetFacingRight(bool facingRight)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingRight;
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

    private NpcOrderVisitor FindNearestInteractableNpc()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        NpcOrderVisitor nearestNpc = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            NpcOrderVisitor npc = nearbyColliders[i].GetComponentInParent<NpcOrderVisitor>();

            if (npc == null)
            {
                continue;
            }

            float distance = (npc.transform.position - transform.position).sqrMagnitude;

            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestNpc = npc;
        }

        return nearestNpc;
    }

    private void SetInteractionPromptVisible(bool isVisible)
    {
        if (interactionPrompt == null || interactionPrompt.activeSelf == isVisible)
        {
            return;
        }

        interactionPrompt.SetActive(isVisible);
    }

    private void ShowDialogue(string message)
    {
        // Dialogue display is now handled by NPCDialogueBubble on the NPC pawn.
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        Gizmos.DrawLine(checkPosition, checkPosition + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
