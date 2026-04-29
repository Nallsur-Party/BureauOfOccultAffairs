using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab asset or hidden scene prototype to instantiate for spawned NPCs.")]
    private GameObject npcPawnPrefab;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private NPCGenerator npcGenerator;
    [SerializeField] private NPCQueueManager npcQueueManager;
    [SerializeField] private Transform routeRoot;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform[] exitPoints;

    private void Awake()
    {
        if (npcGenerator == null)
        {
            npcGenerator = FindObjectOfType<NPCGenerator>();
        }

        if (npcQueueManager == null)
        {
            npcQueueManager = FindObjectOfType<NPCQueueManager>();
        }

        ResolveRouteReferences();

        if (npcGenerator == null)
        {
            Debug.LogError("NPCGenerator not found in scene!", this);
        }

        if (npcQueueManager == null)
        {
            Debug.LogError("NPCQueueManager not found in scene!", this);
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
        npcOrderVisitor.ConfigureRoute(startPoint, counterPoint, exitPoints, true);

        // Регистрируем NPC в очереди
        if (npcQueueManager != null)
        {
            npcQueueManager.EnqueueNPC(npcOrderVisitor);
        }

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

    private void ResolveRouteReferences()
    {
        if (routeRoot == null)
        {
            GameObject routeRootObject = GameObject.Find("WayPoints");
            if (routeRootObject != null)
            {
                routeRoot = routeRootObject.transform;
            }
        }

        if (routeRoot == null)
        {
            return;
        }

        if (startPoint == null)
        {
            startPoint = routeRoot.Find("StartPoint");
        }

        if (counterPoint == null)
        {
            counterPoint = routeRoot.Find("CounterPoint");
        }

        if (exitPoints == null || exitPoints.Length == 0)
        {
            exitPoints = new Transform[]
            {
                routeRoot.Find("ExitPoint_Z"),
                routeRoot.Find("ExitPoint_N")
            };
        }
    }
}

