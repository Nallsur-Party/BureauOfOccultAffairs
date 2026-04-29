using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class DiarySymptomCategoryPanel : MonoBehaviour
{
    [SerializeField] private TextAsset symptomCategoriesXml;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private string targetLabelName = "Categoria";
    [SerializeField] private bool loadOnAwake = true;

    private NPCSymptomCategoryCatalog catalog;

    private void Awake()
    {
        if (loadOnAwake)
        {
            RefreshCategoryButtons();
        }
    }

    [ContextMenu("Refresh Category Buttons")]
    public void RefreshCategoryButtons()
    {
        if (symptomCategoriesXml == null)
        {
            Debug.LogWarning($"{nameof(DiarySymptomCategoryPanel)} on {name} has no categories XML assigned.", this);
            return;
        }

        if (buttonContainer == null)
        {
            buttonContainer = transform;
        }

        catalog = NPCSymptomCategoriesLoader.Load(symptomCategoriesXml);
        ApplyCategoryTexts();
    }

    private void ApplyCategoryTexts()
    {
        if (catalog == null || buttonContainer == null)
        {
            return;
        }

        List<TMP_Text> categoryLabels = new List<TMP_Text>();
        CollectTargetLabels(buttonContainer, categoryLabels);

        int buttonCount = categoryLabels.Count;
        int categoryCount = catalog.Categories.Count;

        for (int i = 0; i < buttonCount; i++)
        {
            bool hasCategory = i < categoryCount;
            TMP_Text label = categoryLabels[i];
            if (label == null)
            {
                continue;
            }

            label.gameObject.SetActive(hasCategory);

            if (hasCategory)
            {
                label.text = catalog.Categories[i].DisplayName;
            }
        }
    }

    private void CollectTargetLabels(Transform root, List<TMP_Text> results)
    {
        if (root == null || results == null)
        {
            return;
        }

        TMP_Text directLabel = root.gameObject.name == targetLabelName
            ? root.GetComponent<TMP_Text>()
            : null;

        if (directLabel != null)
        {
            results.Add(directLabel);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectTargetLabels(root.GetChild(i), results);
        }
    }
}
