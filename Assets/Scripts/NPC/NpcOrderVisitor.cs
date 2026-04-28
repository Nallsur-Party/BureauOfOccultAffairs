using UnityEngine;
using UnityEngine.Events;

public class NpcOrderVisitor : MonoBehaviour
{
    private enum VisitorState
    {
        Idle,
        GoingToCounter,
        WaitingAtCounter,
        Leaving
    }

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

    public bool IsWaitingAtCounter => currentState == VisitorState.WaitingAtCounter;
    public NPC NpcData => npcData;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
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
            $"NPC Debug | Action: {actionLabel} | Response: {safeResponseText} | NPC: {npcData.Name} | Gender: {npcData.Gender} | Age: {npcData.Age} | Trait: {npcData.Trait} | Problem: {problemText} | Symptoms: {symptomsText} | TruthTokens: {npcData.RemainingTruthTokens} | LieTokens: {npcData.RemainingLieTokens} | FollowUpTokens: {npcData.RemainingFollowUpStoryTokens} | QuestionTokens: {npcData.RemainingDetectiveQuestionTokens} | SpentQuestions: {npcData.SpentDetectiveQuestionCount}",
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
        if (currentTarget == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition(currentTarget);
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        Vector3 delta = nextPosition - transform.position;

        transform.position = nextPosition;
        UpdateFacing(delta);

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

        currentTarget = counterPoint;
        currentState = VisitorState.GoingToCounter;
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

        currentTarget = exitPoints[exitIndex];
        currentState = VisitorState.Leaving;
    }

    private void ArriveAtTarget()
    {
        switch (currentState)
        {
            case VisitorState.GoingToCounter:
                currentState = VisitorState.WaitingAtCounter;
                currentTarget = null;
                onReachedCounter.Invoke();
                break;

            case VisitorState.Leaving:
                currentState = VisitorState.Idle;
                currentTarget = null;
                onLeftScene.Invoke();
                break;
        }
    }

    private Vector3 GetTargetPosition(Transform targetPoint)
    {
        Vector3 targetPosition = targetPoint.position;

        if (keepCurrentY)
        {
            targetPosition.y = transform.position.y;
        }

        return targetPosition;
    }

    private void UpdateFacing(Vector3 delta)
    {
        if (spriteRenderer == null || Mathf.Abs(delta.x) <= 0.001f)
        {
            return;
        }

        bool movingRight = delta.x > 0f;
        spriteRenderer.flipX = invertFlipX ? movingRight : !movingRight;
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
    }
}
