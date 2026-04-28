using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueUtility
{
    private const string MemoryGapSymptomId = "S1";
    private const string ConfusedDetailsSymptomId = "S4";
    private const string ContradictoryStorySymptomId = "S18";

    public static NPCTraitType GetRandomTrait()
    {
        int traitCount = System.Enum.GetValues(typeof(NPCTraitType)).Length;
        return (NPCTraitType)Random.Range(0, traitCount);
    }

    public static int CalculateTruthTokens(NPCTraitType trait, int symptomCount)
    {
        int clampedSymptomCount = Mathf.Max(0, symptomCount);

        switch (trait)
        {
            case NPCTraitType.Honest:
                return Mathf.Max(1, clampedSymptomCount);

            case NPCTraitType.Liar:
                return Mathf.Max(1, Mathf.CeilToInt(clampedSymptomCount * 0.35f));

            default:
                return Mathf.Max(1, Mathf.CeilToInt(clampedSymptomCount * 0.6f));
        }
    }

    public static int CalculateFollowUpStoryTokens(NPCTraitType trait, int symptomCount)
    {
        int clampedSymptomCount = Mathf.Max(0, symptomCount);

        switch (trait)
        {
            case NPCTraitType.Honest:
                return Mathf.Max(1, Mathf.CeilToInt(clampedSymptomCount * 0.85f));

            case NPCTraitType.Liar:
                return Mathf.Max(1, Mathf.CeilToInt(clampedSymptomCount * 0.4f));

            default:
                return Mathf.Max(1, Mathf.CeilToInt(clampedSymptomCount * 0.6f));
        }
    }

    public static int CalculateDetectiveQuestionTokens(NPCTraitType trait, int symptomCount)
    {
        int baseTokens = trait == NPCTraitType.Honest ? 4 : trait == NPCTraitType.Neutral ? 3 : 2;

        if (symptomCount <= 0)
        {
            return Mathf.Max(2, baseTokens - 1);
        }

        return Mathf.Clamp(baseTokens, 1, 4);
    }

    public static string GetFallbackLine(NPCTraitType trait, NPCTraitFallbackCatalog fallbackCatalog)
    {
        if (fallbackCatalog != null && fallbackCatalog.TryGetLines(trait, out IReadOnlyList<string> lines) && lines.Count > 0)
        {
            return lines[Random.Range(0, lines.Count)];
        }

        return "Мне больше нечего добавить.";
    }

    public static string GetQuestionLimitLine()
    {
        return "Я уже ответил на все, что готов сейчас рассказать.";
    }

    public static string GetDialogueLine(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return "Данные NPC еще не сгенерированы.";
        }

        if (!npc.HasProblem || npc.SymptomIds.Count == 0)
        {
            return "Ничего странного со мной вроде бы не происходит.";
        }

        if (npc.RemainingTruthTokens <= 0 || symptomLinesCatalog == null)
        {
            return GetFallbackLine(npc.Trait, fallbackCatalog);
        }

        if (!TryGetConversationLine(npc, symptomLinesCatalog, out string conversationLine))
        {
            return GetFallbackLine(npc.Trait, fallbackCatalog);
        }

        return conversationLine;
    }

    public static string GetQuestionResponse(
        NPC npc,
        NPCQuestionType questionType,
        PlayerProfile playerProfile,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return "Сначала нужно найти собеседника.";
        }

        if (questionType == NPCQuestionType.AnotherStory)
        {
            return GetAnotherStoryQuestionResponse(npc, playerProfile, symptomLinesCatalog, fallbackCatalog);
        }

        if (npc.HasRememberedAnswer(questionType, out string rememberedAnswer))
        {
            return rememberedAnswer;
        }

        int playerQuestionLimit = playerProfile != null ? playerProfile.InterrogationLimit : 1;

        if (npc.GetRememberedQuestionCount() >= playerQuestionLimit || npc.RemainingDetectiveQuestionTokens <= 0)
        {
            return GetQuestionLimitLine();
        }

        npc.MarkQuestionAsked(questionType);
        npc.ConsumeDetectiveQuestionToken();

        string answer = BuildQuestionAnswer(npc, questionType);
        npc.RememberAnswer(questionType, answer);
        return answer;
    }

    private static string BuildQuestionAnswer(NPC npc, NPCQuestionType questionType)
    {
        switch (questionType)
        {
            case NPCQuestionType.Name:
                return $"Меня зовут {npc.Name}.";

            case NPCQuestionType.Gender:
                return npc.Gender == NPC.GenderType.Male
                    ? "Я мужчина."
                    : npc.Gender == NPC.GenderType.Female
                        ? "Я женщина."
                        : "Я не хочу уточнять пол.";

            case NPCQuestionType.Age:
                return $"Мне {npc.Age}.";

            default:
                return "Не понимаю, о чем именно вы спрашиваете.";
        }
    }

    private static string GetAnotherStoryQuestionResponse(
        NPC npc,
        PlayerProfile playerProfile,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        if (npc == null)
        {
            return "Сначала нужно найти собеседника.";
        }

        if (npc.RemainingFollowUpStoryTokens > 0
            && symptomLinesCatalog != null
            && TryGetFollowUpLine(npc, symptomLinesCatalog, out string followUpLine))
        {
            int playerQuestionLimit = playerProfile != null ? playerProfile.InterrogationLimit : 1;

            if (npc.GetRememberedQuestionCount() >= playerQuestionLimit || npc.RemainingDetectiveQuestionTokens <= 0)
            {
                return GetQuestionLimitLine();
            }

            npc.MarkQuestionAsked(NPCQuestionType.AnotherStory);
            npc.ConsumeDetectiveQuestionToken();
            npc.ConsumeFollowUpStoryToken();
            npc.MarkFollowUpLineTold(followUpLine);
            return followUpLine;
        }

        string repeatedMemoryLine = TryGetMemoryGapRepeat(npc);

        if (!string.IsNullOrWhiteSpace(repeatedMemoryLine))
        {
            return repeatedMemoryLine;
        }

        if (CanVaryFollowUpLine(npc)
            && symptomLinesCatalog != null
            && TryGetUnstableFollowUpLine(npc, symptomLinesCatalog, out string unstableLine))
        {
            return unstableLine;
        }

        return GetFallbackLine(npc.Trait, fallbackCatalog);
    }

    private static bool TryGetConversationLine(NPC npc, NPCSymptomLinesCatalog symptomLinesCatalog, out string selectedLine)
    {
        return TryGetCandidateLine(
            npc,
            symptomLinesCatalog,
            excludeConversationLines: true,
            excludeFollowUpLines: false,
            allowAnyKnownLines: false,
            out selectedLine
        ) && MarkConversationLine(npc, selectedLine);
    }

    private static bool TryGetFollowUpLine(NPC npc, NPCSymptomLinesCatalog symptomLinesCatalog, out string selectedLine)
    {
        return TryGetCandidateLine(
            npc,
            symptomLinesCatalog,
            excludeConversationLines: true,
            excludeFollowUpLines: true,
            allowAnyKnownLines: false,
            out selectedLine
        );
    }

    private static bool TryGetUnstableFollowUpLine(NPC npc, NPCSymptomLinesCatalog symptomLinesCatalog, out string selectedLine)
    {
        return TryGetCandidateLine(
            npc,
            symptomLinesCatalog,
            excludeConversationLines: false,
            excludeFollowUpLines: false,
            allowAnyKnownLines: true,
            out selectedLine
        );
    }

    private static bool TryGetCandidateLine(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        bool excludeConversationLines,
        bool excludeFollowUpLines,
        bool allowAnyKnownLines,
        out string selectedLine)
    {
        selectedLine = null;

        if (npc == null || symptomLinesCatalog == null)
        {
            return false;
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

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                bool wasInConversation = npc.HasToldConversationLine(line);
                bool wasInFollowUp = npc.HasToldFollowUpLine(line);

                if (!allowAnyKnownLines)
                {
                    if (excludeConversationLines && wasInConversation)
                    {
                        continue;
                    }

                    if (excludeFollowUpLines && wasInFollowUp)
                    {
                        continue;
                    }
                }

                candidateLines.Add(line);
            }
        }

        if (candidateLines.Count == 0)
        {
            return false;
        }

        selectedLine = candidateLines[Random.Range(0, candidateLines.Count)];
        return true;
    }

    private static bool MarkConversationLine(NPC npc, string line)
    {
        if (npc == null || string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (npc.HasToldConversationLine(line))
        {
            return false;
        }

        npc.ConsumeTruthToken();
        npc.MarkConversationLineTold(line);
        return true;
    }

    private static string TryGetMemoryGapRepeat(NPC npc)
    {
        if (npc == null || !npc.HasSymptomId(MemoryGapSymptomId))
        {
            return null;
        }

        List<string> knownLines = new List<string>();
        IReadOnlyList<string> conversationHistory = npc.GetConversationHistory();
        IReadOnlyList<string> followUpHistory = npc.GetFollowUpHistory();

        for (int i = 0; i < conversationHistory.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(conversationHistory[i]))
            {
                knownLines.Add(conversationHistory[i]);
            }
        }

        for (int i = 0; i < followUpHistory.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(followUpHistory[i]))
            {
                knownLines.Add(followUpHistory[i]);
            }
        }

        if (knownLines.Count == 0)
        {
            return null;
        }

        return knownLines[Random.Range(0, knownLines.Count)];
    }

    private static bool CanVaryFollowUpLine(NPC npc)
    {
        return npc != null
            && (npc.HasSymptomId(ConfusedDetailsSymptomId) || npc.HasSymptomId(ContradictoryStorySymptomId));
    }
}
