using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NPC
{
    public enum GenderType
    {
        Male,
        Female,
        Other
    }

    [SerializeField] private string npcName;
    [SerializeField] private GenderType gender;
    [SerializeField] private int age;
    [SerializeField] private NPCTraitType trait;
    [SerializeField] private string problemName;
    [SerializeField] private List<string> symptomIds = new List<string>();
    [SerializeField] private List<string> symptoms = new List<string>();
    [SerializeField] private List<string> preparedConversationLines = new List<string>();
    [SerializeField] private int truthTokens;
    [SerializeField] private int lieTokens;
    [SerializeField] private int followUpStoryTokens;
    [SerializeField] private int detectiveQuestionTokens;
    [SerializeField] private int spentDetectiveQuestionCount;
    [NonSerialized] private HashSet<NPCQuestionType> askedQuestionTypes = new HashSet<NPCQuestionType>();
    [NonSerialized] private Dictionary<NPCQuestionType, string> rememberedAnswers = new Dictionary<NPCQuestionType, string>();
    [NonSerialized] private HashSet<string> conversationLines = new HashSet<string>();
    [NonSerialized] private HashSet<string> followUpLines = new HashSet<string>();
    [NonSerialized] private List<string> conversationHistory = new List<string>();
    [NonSerialized] private List<string> followUpHistory = new List<string>();

    public string Name => npcName;
    public GenderType Gender => gender;
    public int Age => age;
    public NPCTraitType Trait => trait;
    public string ProblemName => problemName;
    public IReadOnlyList<string> SymptomIds => symptomIds;
    public IReadOnlyList<string> Symptoms => symptoms;
    public IReadOnlyList<string> PreparedConversationLines => preparedConversationLines;
    public int RemainingTruthTokens => truthTokens;
    public int RemainingLieTokens => lieTokens;
    public int RemainingConversationTokens => truthTokens + lieTokens;
    public int RemainingFollowUpStoryTokens => followUpStoryTokens;
    public int RemainingDetectiveQuestionTokens => detectiveQuestionTokens;
    public int SpentDetectiveQuestionCount => spentDetectiveQuestionCount;
    public bool HasProblem => !string.IsNullOrWhiteSpace(problemName);

    public NPC(string npcName, GenderType gender, int age, NPCTraitType trait)
    {
        this.npcName = npcName;
        this.gender = gender;
        this.age = age;
        this.trait = trait;
    }

    public NPC(string npcName, GenderType gender, int age, NPCTraitType trait, string problemName, IEnumerable<string> symptomIds, IEnumerable<string> symptoms)
        : this(npcName, gender, age, trait)
    {
        SetProblem(problemName, symptomIds, symptoms);
    }

    public void SetProblem(string newProblemName, IEnumerable<string> newSymptomIds, IEnumerable<string> newSymptoms)
    {
        problemName = string.IsNullOrWhiteSpace(newProblemName) ? null : newProblemName.Trim();
        symptomIds.Clear();
        symptoms.Clear();
        ResetDialogueState();

        if (!HasProblem || newSymptoms == null)
        {
            return;
        }

        if (newSymptomIds != null)
        {
            foreach (string symptomId in newSymptomIds)
            {
                if (string.IsNullOrWhiteSpace(symptomId))
                {
                    continue;
                }

                symptomIds.Add(symptomId.Trim());
            }
        }

        foreach (string symptom in newSymptoms)
        {
            if (string.IsNullOrWhiteSpace(symptom))
            {
                continue;
            }

            symptoms.Add(symptom.Trim());
        }

        truthTokens = NPCDialogueUtility.CalculateTruthTokens(trait);
        lieTokens = NPCDialogueUtility.CalculateLieTokens(trait);
        followUpStoryTokens = NPCDialogueUtility.CalculateFollowUpStoryTokens(trait, symptomIds.Count);
        detectiveQuestionTokens = NPCDialogueUtility.CalculateDetectiveQuestionTokens(trait);
    }

    public void SetProblem(NPCProblemDefinition problem)
    {
        if (problem == null)
        {
            ClearProblem();
            return;
        }

        SetProblem(problem.Name, problem.SymptomIds, problem.Symptoms);
    }

    public void SetPreparedConversationLines(IEnumerable<string> lines)
    {
        preparedConversationLines.Clear();

        if (lines == null)
        {
            return;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            preparedConversationLines.Add(line.Trim());
        }
    }

    public void ClearProblem()
    {
        problemName = null;
        symptomIds.Clear();
        symptoms.Clear();
        preparedConversationLines.Clear();
        ResetDialogueState();
    }

    public void ConsumeTruthToken()
    {
        truthTokens = Math.Max(0, truthTokens - 1);
    }

    public void ConsumeLieToken()
    {
        lieTokens = Math.Max(0, lieTokens - 1);
    }

    public void ConsumeConversationToken()
    {
        if (truthTokens > 0)
        {
            ConsumeTruthToken();
            return;
        }

        if (lieTokens > 0)
        {
            ConsumeLieToken();
        }
    }

    public void ConsumeFollowUpStoryToken()
    {
        followUpStoryTokens = Math.Max(0, followUpStoryTokens - 1);
    }

    public bool HasSymptomId(string symptomId)
    {
        if (string.IsNullOrWhiteSpace(symptomId))
        {
            return false;
        }

        return symptomIds.Contains(symptomId.Trim());
    }

    public bool HasRememberedAnswer(NPCQuestionType questionType, out string answer)
    {
        EnsureRuntimeState();
        return rememberedAnswers.TryGetValue(questionType, out answer);
    }

    public void RememberAnswer(NPCQuestionType questionType, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        EnsureRuntimeState();
        rememberedAnswers[questionType] = answer.Trim();
    }

    public int GetRememberedQuestionCount()
    {
        return spentDetectiveQuestionCount;
    }

    public bool HasAskedQuestion(NPCQuestionType questionType)
    {
        EnsureRuntimeState();
        return askedQuestionTypes.Contains(questionType);
    }

    public void MarkQuestionAsked(NPCQuestionType questionType)
    {
        EnsureRuntimeState();
        askedQuestionTypes.Add(questionType);
    }

    public void ConsumeDetectiveQuestionToken()
    {
        detectiveQuestionTokens = Math.Max(0, detectiveQuestionTokens - 1);
        spentDetectiveQuestionCount++;
    }

    public bool HasToldConversationLine(string storyLine)
    {
        if (string.IsNullOrWhiteSpace(storyLine))
        {
            return false;
        }

        EnsureRuntimeState();
        return conversationLines.Contains(storyLine.Trim());
    }

    public bool HasToldFollowUpLine(string storyLine)
    {
        if (string.IsNullOrWhiteSpace(storyLine))
        {
            return false;
        }

        EnsureRuntimeState();
        return followUpLines.Contains(storyLine.Trim());
    }

    public bool HasToldAnyLine(string storyLine)
    {
        return HasToldConversationLine(storyLine) || HasToldFollowUpLine(storyLine);
    }

    public void MarkConversationLineTold(string storyLine)
    {
        if (string.IsNullOrWhiteSpace(storyLine))
        {
            return;
        }

        EnsureRuntimeState();
        string normalizedLine = storyLine.Trim();

        if (conversationLines.Add(normalizedLine))
        {
            conversationHistory.Add(normalizedLine);
        }
    }

    public void MarkFollowUpLineTold(string storyLine)
    {
        if (string.IsNullOrWhiteSpace(storyLine))
        {
            return;
        }

        EnsureRuntimeState();
        string normalizedLine = storyLine.Trim();

        if (followUpLines.Add(normalizedLine))
        {
            followUpHistory.Add(normalizedLine);
        }
    }

    public IReadOnlyList<string> GetConversationHistory()
    {
        EnsureRuntimeState();
        return conversationHistory;
    }

    public IReadOnlyList<string> GetFollowUpHistory()
    {
        EnsureRuntimeState();
        return followUpHistory;
    }

    private void ResetDialogueState()
    {
        truthTokens = 0;
        lieTokens = 0;
        followUpStoryTokens = 0;
        detectiveQuestionTokens = 0;
        spentDetectiveQuestionCount = 0;
        EnsureRuntimeState();
        askedQuestionTypes.Clear();
        rememberedAnswers.Clear();
        conversationLines.Clear();
        followUpLines.Clear();
        conversationHistory.Clear();
        followUpHistory.Clear();
    }

    private void EnsureRuntimeState()
    {
        if (askedQuestionTypes == null)
        {
            askedQuestionTypes = new HashSet<NPCQuestionType>();
        }

        if (rememberedAnswers == null)
        {
            rememberedAnswers = new Dictionary<NPCQuestionType, string>();
        }

        if (conversationLines == null)
        {
            conversationLines = new HashSet<string>();
        }

        if (followUpLines == null)
        {
            followUpLines = new HashSet<string>();
        }

        if (conversationHistory == null)
        {
            conversationHistory = new List<string>();
        }

        if (followUpHistory == null)
        {
            followUpHistory = new List<string>();
        }
    }
}
