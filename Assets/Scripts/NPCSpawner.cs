using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab asset or hidden scene prototype to instantiate for spawned NPCs.")]
    private GameObject npcPawnPrefab;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private NPCGenerator npcGenerator;

    private void Awake()
    {
        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
        }

        if (npcGenerator == null)
        {
            Debug.LogError("NPCGenerator not found in scene!", this);
        }
    }

    public void SpawnNPC()
    {
        if (npcPawnPrefab == null)
        {
            Debug.LogError("NPC Pawn Prefab is not assigned!", this);
            return;
        }

        if (npcGenerator == null || !npcGenerator.IsCatalogLoaded)
        {
            Debug.LogError("NPCGenerator is not available or catalog not loaded!", this);
            return;
        }

        // Спавним prefab
        GameObject spawnedNpcObject = Instantiate(npcPawnPrefab, spawnParent);
        
        // Получаем компонент NpcOrderVisitor
        NpcOrderVisitor npcOrderVisitor = spawnedNpcObject.GetComponent<NpcOrderVisitor>();
        if (npcOrderVisitor == null)
        {
            Debug.LogError("Spawned NPC Pawn does not have NpcOrderVisitor component!", this);
            Destroy(spawnedNpcObject);
            return;
        }

        // Генерируем данные NPC используя существующий generator
        NPC generatedNpc = GenerateUniqueNPC();
        
        npcOrderVisitor.SetNpcData(generatedNpc);

        Debug.Log($"Spawned NPC: {generatedNpc?.Name}", spawnedNpcObject);
    }

    private NPC GenerateUniqueNPC()
    {
        if (npcGenerator == null)
        {
            return null;
        }

        // Используем метод NPCGenerator для создания уникального NPC
        npcGenerator.GenerateNpc();
        return npcGenerator.GeneratedNpc;
    }

    public void SpawnMultipleNPCs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnNPC();
        }
    }
}

