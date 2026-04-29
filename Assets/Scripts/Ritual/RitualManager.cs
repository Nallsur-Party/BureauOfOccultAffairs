using System.Collections.Generic;
using UnityEngine;

public class RitualManager : MonoBehaviour
{
    private const int DefaultRitualHealth = 3;
    private const int WrongStepDamage = 1;

    [SerializeField] private RitualSolutionCatalog ritualSolutionCatalog;

    private readonly Dictionary<NpcOrderVisitor, RitualProgressState> progressByNpc = new Dictionary<NpcOrderVisitor, RitualProgressState>();
    private RitualSolutionCatalog runtimeCatalog;

    private sealed class RitualProgressState
    {
        public NpcOrderVisitor TargetNpc;
        public RitualSolutionDefinition Solution;
        public int NextStepIndex;
    }

    private RitualSolutionCatalog ActiveCatalog
    {
        get
        {
            if (ritualSolutionCatalog != null)
            {
                return ritualSolutionCatalog;
            }

            if (runtimeCatalog == null)
            {
                runtimeCatalog = RitualSolutionCatalog.CreateRuntimeDefault();
            }

            return runtimeCatalog;
        }
    }

    public bool TryStartRitual(NpcOrderVisitor npc)
    {
        if (npc == null || npc.NpcData == null)
        {
            return false;
        }

        if (npc.NpcData.IsCured)
        {
            LogAttempt(npc, RitualAttemptResult.NpcAlreadyCured, null, null, null);
            return false;
        }

        if (!TryGetSolution(npc, out RitualSolutionDefinition solution))
        {
            LogAttempt(npc, RitualAttemptResult.NoSolution, null, null, null);
            return false;
        }

        if (!npc.NpcData.IsAlive)
        {
            npc.NpcData.InitializeRitualState(npc.NpcData.MaxHealth > 0 ? npc.NpcData.MaxHealth : DefaultRitualHealth);
        }

        progressByNpc[npc] = new RitualProgressState
        {
            TargetNpc = npc,
            Solution = solution,
            NextStepIndex = 0
        };

        LogAttempt(npc, RitualAttemptResult.Started, null, null, GetExpectedStepDescription(solution, 0));
        return true;
    }

    public RitualAttemptResult TryPerformStep(NpcOrderVisitor npc, RitualItemType item, RitualActionType action)
    {
        if (npc == null || npc.NpcData == null)
        {
            return RitualAttemptResult.NoActiveNpc;
        }

        if (npc.NpcData.IsCured)
        {
            npc.ShowPersistentDialogue("Со мной уже всё в порядке.");
            LogAttempt(npc, RitualAttemptResult.NpcAlreadyCured, item, action, null);
            return RitualAttemptResult.NpcAlreadyCured;
        }

        if (!TryGetSolution(npc, out RitualSolutionDefinition solution))
        {
            npc.ShowPersistentDialogue("Для этой проблемы нет подходящего ритуала.");
            LogAttempt(npc, RitualAttemptResult.NoSolution, item, action, null);
            return RitualAttemptResult.NoSolution;
        }

        if (!npc.NpcData.IsAlive)
        {
            npc.ShowPersistentDialogue("Ритуал сорвался. Начни его заново разговором с NPC.");
            LogAttempt(npc, RitualAttemptResult.WrongStep, item, action, "NPC has no health");
            return RitualAttemptResult.WrongStep;
        }

        if (!progressByNpc.TryGetValue(npc, out RitualProgressState state) || state == null || state.Solution == null)
        {
            progressByNpc[npc] = new RitualProgressState
            {
                TargetNpc = npc,
                Solution = solution,
                NextStepIndex = 0
            };
            state = progressByNpc[npc];
        }

        RitualStepDefinition expectedStep = state.NextStepIndex >= 0 && state.NextStepIndex < state.Solution.Steps.Count
            ? state.Solution.Steps[state.NextStepIndex]
            : null;

        if (expectedStep == null)
        {
            ClearProgress(npc);
            LogAttempt(npc, RitualAttemptResult.NoSolution, item, action, "Missing expected step");
            return RitualAttemptResult.NoSolution;
        }

        if (expectedStep.Item != item || expectedStep.Action != action)
        {
            npc.NpcData.ApplyRitualDamage(WrongStepDamage);
            ClearProgress(npc);

            string failureText = $"Нет. Это неправильно. Здоровье: {npc.NpcData.Health}/{npc.NpcData.MaxHealth}";
            if (!npc.NpcData.IsAlive)
            {
                failureText += "\nНужно начать ритуал заново.";
            }

            npc.ShowPersistentDialogue(failureText);
            LogAttempt(npc, RitualAttemptResult.WrongStep, item, action, FormatStep(expectedStep));
            return RitualAttemptResult.WrongStep;
        }

        state.NextStepIndex++;

        if (state.NextStepIndex >= state.Solution.Steps.Count)
        {
            npc.NpcData.MarkCured();
            npc.ShowPersistentDialogue("Мне стало легче...");
            ClearProgress(npc);
            LogAttempt(npc, RitualAttemptResult.Completed, item, action, FormatStep(expectedStep));
            npc.LeaveRandomExit();
            return RitualAttemptResult.Completed;
        }

        npc.ShowPersistentDialogue("Что-то сработало...");
        LogAttempt(npc, RitualAttemptResult.Advanced, item, action, GetExpectedStepDescription(state.Solution, state.NextStepIndex));
        return RitualAttemptResult.Advanced;
    }

    public void ClearProgress(NpcOrderVisitor npc)
    {
        if (npc == null)
        {
            return;
        }

        progressByNpc.Remove(npc);
    }

    private bool TryGetSolution(NpcOrderVisitor npc, out RitualSolutionDefinition solution)
    {
        solution = null;

        if (npc == null || npc.NpcData == null || !npc.NpcData.HasProblem)
        {
            return false;
        }

        RitualSolutionCatalog catalog = ActiveCatalog;
        return catalog != null && catalog.TryGetSolution(npc.NpcData.ProblemName, out solution);
    }

    private void LogAttempt(
        NpcOrderVisitor npc,
        RitualAttemptResult result,
        RitualItemType? selectedItem,
        RitualActionType? selectedAction,
        string expectedStepDescription)
    {
        string npcName = npc != null && npc.NpcData != null ? npc.NpcData.Name : "No NPC";
        string problemName = npc != null && npc.NpcData != null && npc.NpcData.HasProblem ? npc.NpcData.ProblemName : "No problem";
        string healthText = npc != null && npc.NpcData != null ? $"{npc.NpcData.Health}/{npc.NpcData.MaxHealth}" : "-";
        string itemText = selectedItem.HasValue ? selectedItem.Value.ToString() : "-";
        string actionText = selectedAction.HasValue ? selectedAction.Value.ToString() : "-";
        string expectedText = string.IsNullOrWhiteSpace(expectedStepDescription) ? "-" : expectedStepDescription;

        Debug.Log(
            $"Ritual Debug | NPC: {npcName} | Problem: {problemName} | Item: {itemText} | Action: {actionText} | Expected: {expectedText} | Result: {result} | Health: {healthText}",
            npc
        );
    }

    private static string GetExpectedStepDescription(RitualSolutionDefinition solution, int stepIndex)
    {
        if (solution == null || solution.Steps == null || stepIndex < 0 || stepIndex >= solution.Steps.Count)
        {
            return null;
        }

        return FormatStep(solution.Steps[stepIndex]);
    }

    private static string FormatStep(RitualStepDefinition step)
    {
        return step == null ? null : $"{step.Item} + {step.Action}";
    }
}
