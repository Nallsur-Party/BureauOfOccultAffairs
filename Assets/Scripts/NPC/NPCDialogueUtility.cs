using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueUtility
{
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

    public static string GetFallbackLine(NPCTraitType trait, NPCTraitFallbackCatalog fallbackCatalog)
    {
        if (fallbackCatalog != null && fallbackCatalog.TryGetLines(trait, out IReadOnlyList<string> lines) && lines.Count > 0)
        {
            return lines[Random.Range(0, lines.Count)];
        }

        return "Мне больше нечего добавить.";
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
        return selectedLine;
    }
}
