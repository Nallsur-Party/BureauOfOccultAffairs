using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueUtility
{
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

    public static string GetNextSymptomLine(
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

        List<int> candidateIndices = new List<int>();

        for (int i = 0; i < npc.SymptomIds.Count; i++)
        {
            if (npc.HasRevealedSymptom(npc.SymptomIds[i]))
            {
                continue;
            }

            if (symptomLinesCatalog.TryGetLines(npc.SymptomIds[i], out IReadOnlyList<string> lines) && lines.Count > 0)
            {
                candidateIndices.Add(i);
            }
        }

        if (candidateIndices.Count == 0)
        {
            return GetFallbackLine(npc.Trait, fallbackCatalog);
        }

        int symptomIndex = candidateIndices[Random.Range(0, candidateIndices.Count)];
        string symptomId = npc.SymptomIds[symptomIndex];

        if (!symptomLinesCatalog.TryGetLines(symptomId, out IReadOnlyList<string> symptomLines) || symptomLines.Count == 0)
        {
            return GetFallbackLine(npc.Trait, fallbackCatalog);
        }

        string selectedLine = symptomLines[Random.Range(0, symptomLines.Count)];
        npc.MarkSymptomRevealed(symptomId);
        npc.ConsumeTruthToken();
        npc.SetLastStoryLine(selectedLine);
        return selectedLine;
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

        bool canRepeatDifferently = CanVaryRepeatedQuestion(npc, questionType);

        if (!canRepeatDifferently && npc.HasRememberedAnswer(questionType, out string rememberedAnswer))
        {
            return rememberedAnswer;
        }

        bool isNewQuestion = !npc.HasAskedQuestion(questionType);
        int playerQuestionLimit = playerProfile != null ? playerProfile.InterrogationLimit : 1;

        if (isNewQuestion)
        {
            if (npc.GetRememberedQuestionCount() >= playerQuestionLimit || npc.RemainingDetectiveQuestionTokens <= 0)
            {
                return GetQuestionLimitLine();
            }

            npc.MarkQuestionAsked(questionType);
            npc.ConsumeDetectiveQuestionToken();
        }

        string answer = BuildQuestionAnswer(npc, questionType, symptomLinesCatalog, fallbackCatalog);

        if (!canRepeatDifferently)
        {
            npc.RememberAnswer(questionType, answer);
        }

        return answer;
    }

    private static string BuildQuestionAnswer(
        NPC npc,
        NPCQuestionType questionType,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
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

            case NPCQuestionType.AnotherStory:
                return GetAnotherStoryAnswer(npc, symptomLinesCatalog, fallbackCatalog);

            default:
                return "Не понимаю, о чем именно вы спрашиваете.";
        }
    }

    private static string GetAnotherStoryAnswer(
        NPC npc,
        NPCSymptomLinesCatalog symptomLinesCatalog,
        NPCTraitFallbackCatalog fallbackCatalog
    )
    {
        string lastStoryLine = npc.GetLastStoryLine();

        if (!CanVaryRepeatedQuestion(npc, NPCQuestionType.AnotherStory) && !string.IsNullOrWhiteSpace(lastStoryLine))
        {
            return lastStoryLine;
        }

        if (npc.RemainingTruthTokens > 0 && symptomLinesCatalog != null)
        {
            return GetNextSymptomLine(npc, symptomLinesCatalog, fallbackCatalog);
        }

        return GetFallbackLine(npc.Trait, fallbackCatalog);
    }

    private static bool CanVaryRepeatedQuestion(NPC npc, NPCQuestionType questionType)
    {
        if (npc == null || questionType != NPCQuestionType.AnotherStory)
        {
            return false;
        }

        return npc.HasSymptomId(ConfusedDetailsSymptomId) || npc.HasSymptomId(ContradictoryStorySymptomId);
    }
}
