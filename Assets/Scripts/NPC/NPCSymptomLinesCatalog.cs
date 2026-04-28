using System;
using System.Collections.Generic;

public class NPCSymptomLinesCatalog
{
    private readonly Dictionary<string, List<string>> linesBySymptomId;

    public NPCSymptomLinesCatalog(Dictionary<string, List<string>> loadedLines)
    {
        linesBySymptomId = loadedLines ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetLines(string symptomId, out IReadOnlyList<string> lines)
    {
        if (string.IsNullOrWhiteSpace(symptomId) || !linesBySymptomId.TryGetValue(symptomId.Trim(), out List<string> storedLines))
        {
            lines = null;
            return false;
        }

        lines = storedLines;
        return true;
    }
}
