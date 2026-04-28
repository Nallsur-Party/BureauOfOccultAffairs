using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCGenerator : MonoBehaviour
{
    [Serializable]
    private class NamePool
    {
        [SerializeField] private List<string> maleNames = new List<string>();
        [SerializeField] private List<string> femaleNames = new List<string>();
        [SerializeField] private List<string> otherNames = new List<string>();

        public string GetRandomName(NPC.GenderType gender)
        {
            List<string> selectedPool = GetPool(gender);

            if (selectedPool.Count == 0)
            {
                return "Unnamed NPC";
            }

            return selectedPool[UnityEngine.Random.Range(0, selectedPool.Count)];
        }

        private List<string> GetPool(NPC.GenderType gender)
        {
            switch (gender)
            {
                case NPC.GenderType.Male:
                    return maleNames;

                case NPC.GenderType.Female:
                    return femaleNames;

                default:
                    return otherNames.Count > 0 ? otherNames : femaleNames.Count > 0 ? femaleNames : maleNames;
            }
        }
    }

    [Header("Data")]
    [SerializeField] private TextAsset npcProblemsXml;
    [SerializeField] private TextAsset npcSymptomeLinesXml;
    [SerializeField] private TextAsset npcTraitFallbackLinesXml;

    [Header("NPC Settings")]
    [SerializeField] private int minAge = 18;
    [SerializeField] private int maxAge = 65;
    [SerializeField, Range(0f, 1f)] private float noProblemChance = 0.15f;
    [SerializeField] private NamePool namePool = new NamePool();

    [Header("Debug")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private NPC generatedNpc;

    private NPCProblemCatalog problemCatalog;
    private NPCSymptomLinesCatalog symptomLinesCatalog;
    private NPCTraitFallbackCatalog traitFallbackCatalog;

    public NPC GeneratedNpc => generatedNpc;
    public NPCProblemCatalog ProblemCatalog => problemCatalog;
    public NPCSymptomLinesCatalog SymptomLinesCatalog => symptomLinesCatalog;
    public NPCTraitFallbackCatalog TraitFallbackCatalog => traitFallbackCatalog;
    public bool IsCatalogLoaded => problemCatalog != null;

    private void Awake()
    {
        if (loadOnAwake)
        {
            LoadCatalog();
        }
    }

    [ContextMenu("Load NPC Catalog")]
    public void LoadCatalog()
    {
        symptomLinesCatalog = null;
        traitFallbackCatalog = null;

        if (npcProblemsXml == null)
        {
            Debug.LogWarning($"{nameof(NPCGenerator)} on {name} has no XML assigned.", this);
            problemCatalog = null;
            return;
        }

        problemCatalog = NPCProblemsLoader.Load(npcProblemsXml);

        if (npcSymptomeLinesXml != null)
        {
            symptomLinesCatalog = NPCSymptomLinesLoader.Load(npcSymptomeLinesXml);
        }

        if (npcTraitFallbackLinesXml != null)
        {
            traitFallbackCatalog = NPCTraitFallbackLoader.Load(npcTraitFallbackLinesXml);
        }
    }

    [ContextMenu("Generate NPC")]
    public void GenerateNpc()
    {
        EnsureCatalogLoaded();

        NPC.GenderType gender = GetRandomGender();
        string npcName = namePool.GetRandomName(gender);
        int age = UnityEngine.Random.Range(Mathf.Min(minAge, maxAge), Mathf.Max(minAge, maxAge) + 1);
        NPCTraitType trait = NPCDialogueUtility.GetRandomTrait();

        generatedNpc = new NPC(npcName, gender, age, trait);

        if (problemCatalog == null || problemCatalog.Problems.Count == 0 || UnityEngine.Random.value <= noProblemChance)
        {
            return;
        }

        NPCProblemDefinition problem = problemCatalog.Problems[UnityEngine.Random.Range(0, problemCatalog.Problems.Count)];
        generatedNpc.SetProblem(problem);
        generatedNpc.SetPreparedConversationLines(BuildPreparedConversationLines(generatedNpc));
    }

    public NPC CreateNpc(string npcName, NPC.GenderType gender, int age, string problemName = null)
    {
        EnsureCatalogLoaded();

        NPC npc = new NPC(npcName, gender, age, NPCDialogueUtility.GetRandomTrait());

        if (string.IsNullOrWhiteSpace(problemName))
        {
            return npc;
        }

        if (problemCatalog != null && problemCatalog.TryGetProblem(problemName, out NPCProblemDefinition problem))
        {
            npc.SetProblem(problem);
            npc.SetPreparedConversationLines(BuildPreparedConversationLines(npc));
        }

        return npc;
    }

    public bool TryGetProblem(string problemName, out NPCProblemDefinition problem)
    {
        EnsureCatalogLoaded();

        if (problemCatalog == null)
        {
            problem = null;
            return false;
        }

        return problemCatalog.TryGetProblem(problemName, out problem);
    }

    public string GetDialogueLine(NPC npc)
    {
        EnsureCatalogLoaded();
        return NPCDialogueUtility.GetDialogueLine(npc, symptomLinesCatalog, traitFallbackCatalog);
    }

    public string GetQuestionResponse(NPC npc, NPCQuestionType questionType, PlayerProfile playerProfile)
    {
        EnsureCatalogLoaded();
        return NPCDialogueUtility.GetQuestionResponse(
            npc,
            questionType,
            playerProfile,
            symptomLinesCatalog,
            traitFallbackCatalog
        );
    }

    private void EnsureCatalogLoaded()
    {
        if (problemCatalog == null)
        {
            LoadCatalog();
        }
    }

    private List<string> BuildPreparedConversationLines(NPC npc)
    {
        List<string> preparedLines = new List<string>();

        if (npc == null || symptomLinesCatalog == null || npc.SymptomIds.Count == 0)
        {
            return preparedLines;
        }

        List<string> candidateLines = new List<string>();

        for (int i = 0; i < npc.SymptomIds.Count; i++)
        {
            if (!symptomLinesCatalog.TryGetLines(npc.SymptomIds[i], out IReadOnlyList<string> symptomLines) || symptomLines.Count == 0)
            {
                continue;
            }

            for (int lineIndex = 0; lineIndex < symptomLines.Count; lineIndex++)
            {
                string line = symptomLines[lineIndex];

                if (string.IsNullOrWhiteSpace(line) || candidateLines.Contains(line))
                {
                    continue;
                }

                candidateLines.Add(line);
            }
        }

        int preparedCount = Mathf.Min(npc.RemainingTruthTokens, candidateLines.Count);

        for (int i = 0; i < preparedCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidateLines.Count);
            preparedLines.Add(candidateLines[selectedIndex]);
            candidateLines.RemoveAt(selectedIndex);
        }

        return preparedLines;
    }

    private static NPC.GenderType GetRandomGender()
    {
        Array values = Enum.GetValues(typeof(NPC.GenderType));
        return (NPC.GenderType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }
}
