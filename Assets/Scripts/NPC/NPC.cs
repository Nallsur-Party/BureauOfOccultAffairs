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
    [SerializeField] private int truthTokens;
    [SerializeField] private List<string> revealedSymptomIds = new List<string>();

    public string Name => npcName;
    public GenderType Gender => gender;
    public int Age => age;
    public NPCTraitType Trait => trait;
    public string ProblemName => problemName;
    public IReadOnlyList<string> SymptomIds => symptomIds;
    public IReadOnlyList<string> Symptoms => symptoms;
    public int RemainingTruthTokens => truthTokens;
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

        truthTokens = NPCDialogueUtility.CalculateTruthTokens(trait, symptomIds.Count);
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

    public void ClearProblem()
    {
        problemName = null;
        symptomIds.Clear();
        symptoms.Clear();
        ResetDialogueState();
    }

    public bool HasRevealedSymptom(string symptomId)
    {
        if (string.IsNullOrWhiteSpace(symptomId))
        {
            return false;
        }

        return revealedSymptomIds.Contains(symptomId.Trim());
    }

    public void MarkSymptomRevealed(string symptomId)
    {
        if (string.IsNullOrWhiteSpace(symptomId) || HasRevealedSymptom(symptomId))
        {
            return;
        }

        revealedSymptomIds.Add(symptomId.Trim());
    }

    public void ConsumeTruthToken()
    {
        truthTokens = Math.Max(0, truthTokens - 1);
    }

    private void ResetDialogueState()
    {
        truthTokens = 0;
        revealedSymptomIds.Clear();
    }
}
