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
    [SerializeField] private string problemName;
    [SerializeField] private List<string> symptoms = new List<string>();

    public string Name => npcName;
    public GenderType Gender => gender;
    public int Age => age;
    public string ProblemName => problemName;
    public IReadOnlyList<string> Symptoms => symptoms;
    public bool HasProblem => !string.IsNullOrWhiteSpace(problemName);

    public NPC(string npcName, GenderType gender, int age)
    {
        this.npcName = npcName;
        this.gender = gender;
        this.age = age;
    }

    public NPC(string npcName, GenderType gender, int age, string problemName, IEnumerable<string> symptoms)
        : this(npcName, gender, age)
    {
        SetProblem(problemName, symptoms);
    }

    public void SetProblem(string newProblemName, IEnumerable<string> newSymptoms)
    {
        problemName = string.IsNullOrWhiteSpace(newProblemName) ? null : newProblemName.Trim();
        symptoms.Clear();

        if (!HasProblem || newSymptoms == null)
        {
            return;
        }

        foreach (string symptom in newSymptoms)
        {
            if (string.IsNullOrWhiteSpace(symptom))
            {
                continue;
            }

            symptoms.Add(symptom.Trim());
        }
    }

    public void SetProblem(NPCProblemDefinition problem)
    {
        if (problem == null)
        {
            ClearProblem();
            return;
        }

        SetProblem(problem.Name, problem.Symptoms);
    }

    public void ClearProblem()
    {
        problemName = null;
        symptoms.Clear();
    }
}
