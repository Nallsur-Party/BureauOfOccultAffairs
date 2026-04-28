using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public static class NPCTraitFallbackLoader
{
    public static NPCTraitFallbackCatalog Load(TextAsset xmlAsset)
    {
        if (xmlAsset == null)
        {
            throw new ArgumentNullException(nameof(xmlAsset));
        }

        return Load(xmlAsset.text);
    }

    public static NPCTraitFallbackCatalog Load(string xmlContent)
    {
        Dictionary<NPCTraitType, List<string>> linesByTrait = new Dictionary<NPCTraitType, List<string>>();

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new NPCTraitFallbackCatalog(linesByTrait);
        }

        XDocument document = XDocument.Parse(xmlContent);
        XElement root = document.Element("TraitFallbackLines");

        if (root == null)
        {
            return new NPCTraitFallbackCatalog(linesByTrait);
        }

        foreach (XElement traitElement in root.Elements("Trait"))
        {
            string traitValue = traitElement.Attribute("type")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(traitValue) || !Enum.TryParse(traitValue, true, out NPCTraitType trait))
            {
                continue;
            }

            List<string> lines = new List<string>();

            foreach (XElement sayElement in traitElement.Elements("Say"))
            {
                string line = sayElement.Value?.Trim();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            if (lines.Count > 0)
            {
                linesByTrait[trait] = lines;
            }
        }

        return new NPCTraitFallbackCatalog(linesByTrait);
    }
}
