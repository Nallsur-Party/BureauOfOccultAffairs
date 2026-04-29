using UnityEngine;
using UnityEngine.Events;

public class NpcOrderVisitor : MonoBehaviour
{
    private enum VisitorState
    {
        Idle,
        GoingToCounter,
        WaitingInQueue,
        WaitingAtCounter,
        Leaving
    }

    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int SpeedXHash = Animator.StringToHash("speedX");
    private static readonly int SpeedZHash = Animator.StringToHash("speedZ");
    private static readonly int IsMovingForwardHash = Animator.StringToHash("isMovingForward");
    private static readonly int IsMovingBackwardHash = Animator.StringToHash("isMovingBackward");
    private static readonly int IsLookingDownHash = Animator.StringToHash("isLookingDown");
    private static readonly int IsLookingHorizontalHash = Animator.StringToHash("isLookingHorizontal");
    private static readonly int IsPlayerNearHash = Animator.StringToHash("isPlayerNear");

    [Header("Route")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private bool snapToStartPointOnAwake = true;
    [SerializeField] private bool beginRouteOnAwake = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 0.05f;
    [SerializeField] private bool keepCurrentY = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool invertFlipX;
    [SerializeField] private bool facesRightByDefault = true;
    [SerializeField] private Animator animator;
    [SerializeField] private float lookAtPlayerRadius = 2f;
    [SerializeField] private NPCDialogueBubble dialogueBubble;

    [Header("NPC Data")]
    [SerializeField] private NPCGenerator npcGenerator;
    [SerializeField] private bool generateNpcDataOnAwake = true;
    [SerializeField] private NPC npcData;
    [SerializeField] private bool renameGameObjectToNpcName = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onReachedCounter;
    [SerializeField] private UnityEvent onLeftScene;

    private VisitorState currentState = VisitorState.Idle;
    private Transform currentTarget;
    private bool useCustomTarget;
    private Vector3 customTargetPosition;
    private Vector3 lastFrameVelocity = Vector3.zero;
    private PlayerController playerController;
    private NPCQueueManager npcQueueManager;

    public bool IsWaitingAtCounter => currentState == VisitorState.WaitingAtCounter;
    public bool IsInQueue => currentState == VisitorState.WaitingInQueue;
    public NPC NpcData => npcData;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
        }

        if (dialogueBubble == null)
        {
            dialogueBubble = GetComponentInChildren<NPCDialogueBubble>();
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (generateNpcDataOnAwake)
        {
            GenerateNpcData();
        }

        if (snapToStartPointOnAwake && startPoint != null)
        {
            transform.position = GetTargetPosition(startPoint);
        }

        if (beginRouteOnAwake)
        {
            SendToCounter();
        }
    }

    public void GenerateNpcData()
    {
        if (npcGenerator == null)
        {
            Debug.LogWarning($"{nameof(NpcOrderVisitor)} on {name} could not find {nameof(NPCGenerator)}.", this);
            return;
        }

        npcGenerator.GenerateNpc();
        npcData = npcGenerator.GeneratedNpc;

        if (renameGameObjectToNpcName && npcData != null && !string.IsNullOrWhiteSpace(npcData.Name))
        {
            gameObject.name = $"NPC - {npcData.Name}";
        }
    }

    public void Interact()
    {
        if (npcData == null)
        {
            Debug.Log($"NPC {name}: data has not been generated yet.", this);
            return;
        }

        string interactionText = GetInteractionText();
        DebugLogNpcState("Interact", interactionText);
    }

    public void ShowDialogue(string message)
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.Show(message);
    }

    public void HideDialogue()
    {
        if (dialogueBubble == null)
        {
            return;
        }

        dialogueBubble.Hide();
    }

    public void SetNpcData(NPC npc)
    {
        if (npc == null)
        {
            return;
        }

        npcData = npc;

        if (renameGameObjectToNpcName && !string.IsNullOrWhiteSpace(npcData.Name))
        {
            gameObject.name = $"NPC - {npcData.Name}";
        }
    }

    public string GetInteractionText()
    {
        if (npcData == null)
        {
            return "NPC data has not been generated yet.";
        }

        string dialogueLine = npcGenerator != null
            ? npcGenerator.GetDialogueLine(npcData)
            : null;

        if (string.IsNullOrWhiteSpace(dialogueLine))
        {
            dialogueLine = "Мне нечего сказать.";
        }

        return dialogueLine;
    }

    public string GetQuestionResponse(NPCQuestionType questionType, PlayerProfile playerProfile)
    {
        if (npcData == null)
        {
            return "NPC data has not been generated yet.";
        }

        if (npcGenerator == null)
        {
            return "Говорить пока не о чем.";
        }

        string answer = npcGenerator.GetQuestionResponse(npcData, questionType, playerProfile);
        DebugLogNpcState($"Question {questionType}", answer);

        return answer;
    }

    private void DebugLogNpcState(string actionLabel, string responseText)
    {
        if (npcData == null)
        {
            return;
        }

        string symptomsText = npcData.Symptoms.Count > 0
            ? BuildSymptomsDebugText()
            : "No symptoms";
        string problemText = npcData.HasProblem ? npcData.ProblemName : "No problem";
        string safeResponseText = string.IsNullOrWhiteSpace(responseText) ? "No response" : responseText;

        Debug.Log(
            $"NPC Debug | Action: {actionLabel} | Response: {safeResponseText} | NPC: {npcData.Name} | Gender: {npcData.Gender} | Age: {npcData.Age} | Trait: {npcData.Trait} | Problem: {problemText} | Symptoms: {symptomsText} | TruthTokens: {npcData.RemainingTruthTokens} | LieTokens: {npcData.RemainingLieTokens} | QuestionTokens: {npcData.RemainingDetectiveQuestionTokens} | SpentQuestions: {npcData.SpentDetectiveQuestionCount}",
            this
        );
    }

    private string BuildSymptomsDebugText()
    {
        if (npcData == null || npcData.SymptomIds.Count == 0 || npcData.Symptoms.Count == 0)
        {
            return "No symptoms";
        }

        int symptomCount = Mathf.Min(npcData.SymptomIds.Count, npcData.Symptoms.Count);
        string[] symptomEntries = new string[symptomCount];

        for (int i = 0; i < symptomCount; i++)
        {
            symptomEntries[i] = $"{npcData.SymptomIds[i]}: {npcData.Symptoms[i]}";
        }

        return string.Join(", ", symptomEntries);
    }

    private void Update()
    {
        if (currentTarget == null && !useCustomTarget)
        {
            lastFrameVelocity = Vector3.zero;
            UpdateAnimator();
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        Vector3 delta = nextPosition - transform.position;

        transform.position = nextPosition;
        lastFrameVelocity = delta / Time.deltaTime;
        UpdateFacing(delta);
        UpdateAnimator();

        if (Vector3.Distance(transform.position, targetPosition) <= stoppingDistance)
        {
            ArriveAtTarget();
        }
    }

    public void SendToCounter()
    {
        if (counterPoint == null)
        {
            return;
        }

        SetTargetTransform(counterPoint);
        currentState = VisitorState.GoingToCounter;
    }

    public void SendToQueuePosition(Vector3 queuePosition)
    {
        SetTargetPosition(queuePosition);
        currentState = VisitorState.WaitingInQueue;
    }

    public void LeaveRandomExit()
    {
        if (exitPoints == null || exitPoints.Length == 0)
        {
            currentState = VisitorState.Idle;
            currentTarget = null;
            return;
        }

        int index = Random.Range(0, exitPoints.Length);
        LeaveThroughExit(index);
    }

    public void LeaveThroughExit(int exitIndex)
    {
        if (exitPoints == null || exitIndex < 0 || exitIndex >= exitPoints.Length || exitPoints[exitIndex] == null)
        {
            return;
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        if (npcQueueManager != null)
        {
            npcQueueManager.DequeueNPC(this);
        }

        SetTargetTransform(exitPoints[exitIndex]);
        currentState = VisitorState.Leaving;
    }

    public void LeaveThroughExitByName(string exitName)
    {
        if (exitPoints == null || string.IsNullOrWhiteSpace(exitName))
        {
            return;
        }

        for (int i = 0; i < exitPoints.Length; i++)
        {
            if (exitPoints[i] != null && exitPoints[i].name.Contains(exitName))
            {
                LeaveThroughExit(i);
                return;
            }
        }
    }

    private void ArriveAtTarget()
    {
        switch (currentState)
        {
            case VisitorState.GoingToCounter:
                currentState = VisitorState.WaitingAtCounter;
                ClearTarget();
                onReachedCounter.Invoke();
                break;

            case VisitorState.WaitingInQueue:
                currentState = VisitorState.WaitingInQueue;
                ClearTarget();
                break;

            case VisitorState.Leaving:
                currentState = VisitorState.Idle;
                ClearTarget();
                onLeftScene.Invoke();
                HideDialogue();
                if (npcQueueManager != null)
                {
                    npcQueueManager.DequeueNPC(this);
                }
                gameObject.SetActive(false);
                break;
        }
    }

    private Vector3 GetTargetPosition(Transform targetPoint)
    {
        if (targetPoint == null)
        {
            return GetTargetPosition();
        }

        Vector3 targetPosition = targetPoint.position;

        if (keepCurrentY)
        {
            targetPosition.y = transform.position.y;
        }

        return targetPosition;
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 targetPosition;

        if (useCustomTarget)
        {
            targetPosition = customTargetPosition;
        }
        else
        {
            targetPosition = currentTarget != null ? currentTarget.position : transform.position;
        }

        if (keepCurrentY)
        {
            targetPosition.y = transform.position.y;
        }

        return targetPosition;
    }

    private void SetTargetTransform(Transform target)
    {
        currentTarget = target;
        useCustomTarget = false;
    }

    private void SetTargetPosition(Vector3 position)
    {
        customTargetPosition = position;
        useCustomTarget = true;
        currentTarget = null;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        useCustomTarget = false;
    }

    private void UpdateFacing(Vector3 delta)
    {
        if (spriteRenderer == null || Mathf.Abs(delta.x) <= 0.001f)
        {
            return;
        }

        bool movingRight = delta.x > 0f;
        bool shouldFaceRight = movingRight;

        if (invertFlipX)
        {
            shouldFaceRight = !shouldFaceRight;
        }

        if (facesRightByDefault)
        {
            spriteRenderer.flipX = !shouldFaceRight;
        }
        else
        {
            spriteRenderer.flipX = shouldFaceRight;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(lastFrameVelocity);
        float planarSpeed = new Vector2(lastFrameVelocity.x, lastFrameVelocity.z).magnitude;
        float speedX = Mathf.Abs(localVelocity.x);
        float speedZ = localVelocity.z;
        bool isMovingForward = speedZ >= speedX;
        bool isMovingBackward = speedZ <= -speedX;

        bool isLookingDown = false;
        bool isLookingHorizontal = false;
        bool isPlayerNear = false;
        if (playerController != null)
        {
            Vector3 playerPosition = playerController.transform.position;
            float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
            isPlayerNear = distanceToPlayer <= lookAtPlayerRadius;
            bool canLookAtPlayer = isPlayerNear && currentState == VisitorState.WaitingAtCounter;
            if (canLookAtPlayer)
            {
                Vector3 localDirection = transform.InverseTransformDirection((playerPosition - transform.position).normalized);
                float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

                // Горизонтальный сектор примерно 45…135 градусов в боковых четвертях
                isLookingHorizontal = Mathf.Abs(angle) >= 45f && Mathf.Abs(angle) <= 135f;
                // Если игрок ниже и не в боковой области, считаем взгляд вниз
                isLookingDown = playerPosition.z < transform.position.z && !isLookingHorizontal;
                // Флипим спрайт в зависимости от X позиции игрока только если NPC на стойке
                UpdateLookDirection();
            }
        }

        animator.SetFloat(SpeedHash, planarSpeed);
        animator.SetFloat(SpeedXHash, speedX);
        animator.SetFloat(SpeedZHash, speedZ);
        animator.SetBool(IsMovingForwardHash, isMovingForward);
        animator.SetBool(IsMovingBackwardHash, isMovingBackward);
        animator.SetBool(IsLookingDownHash, isLookingDown);
        animator.SetBool(IsLookingHorizontalHash, isLookingHorizontal);
        animator.SetBool(IsPlayerNearHash, isPlayerNear);
    }

    private void UpdateLookDirection()
    {
        if (playerController == null || spriteRenderer == null)
        {
            return;
        }

        bool playerIsToRight = playerController.transform.position.x > transform.position.x;
        bool shouldFaceRight = playerIsToRight;

        if (invertFlipX)
        {
            shouldFaceRight = !shouldFaceRight;
        }

        if (facesRightByDefault)
        {
            spriteRenderer.flipX = !shouldFaceRight;
        }
        else
        {
            spriteRenderer.flipX = shouldFaceRight;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.15f);
        }

        if (counterPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(counterPoint.position, 0.15f);
        }

        if (exitPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < exitPoints.Length; i++)
        {
            if (exitPoints[i] != null)
            {
                Gizmos.DrawWireSphere(exitPoints[i].position, 0.15f);
            }
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, lookAtPlayerRadius);
    }
}
