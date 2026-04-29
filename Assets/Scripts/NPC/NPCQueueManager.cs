using System.Collections.Generic;
using UnityEngine;

public class NPCQueueManager : MonoBehaviour
{
    private Queue<NpcOrderVisitor> npcQueue = new Queue<NpcOrderVisitor>();

    public void EnqueueNPC(NpcOrderVisitor npc)
    {
        if (npc != null && !npcQueue.Contains(npc))
        {
            npcQueue.Enqueue(npc);
            Debug.Log($"NPC {npc.gameObject.name} added to queue. Queue size: {npcQueue.Count}");
        }
    }

    public void DequeueNPC(NpcOrderVisitor npc)
    {
        if (npc != null && npcQueue.Contains(npc))
        {
            // Создаём временную очередь без этого NPC
            Queue<NpcOrderVisitor> tempQueue = new Queue<NpcOrderVisitor>();
            while (npcQueue.Count > 0)
            {
                NpcOrderVisitor current = npcQueue.Dequeue();
                if (current != npc)
                {
                    tempQueue.Enqueue(current);
                }
            }
            npcQueue = tempQueue;
            Debug.Log($"NPC {npc.gameObject.name} removed from queue. Queue size: {npcQueue.Count}");
        }
    }

    public NpcOrderVisitor GetNextWaitingNPC()
    {
        // Возвращаем первого NPC в очереди, который ждёт у стойки
        foreach (NpcOrderVisitor npc in npcQueue)
        {
            if (npc.IsWaitingAtCounter)
            {
                return npc;
            }
        }
        return null;
    }

    public int QueueSize => npcQueue.Count;
}