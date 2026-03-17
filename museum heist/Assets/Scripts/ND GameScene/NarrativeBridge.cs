using UnityEngine;

public class NarrativeBridge : MonoBehaviour
{
    public static NarrativeBridge Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
    }

    public void Fire(string triggerName)
    {
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            Debug.LogWarning($"[NarrativeBridge] No ActiveNPC, cannot fire '{triggerName}'.");
            return;
        }

        npc.TriggerEvent(triggerName);
        Debug.Log($"[NarrativeBridge] Fired trigger '{triggerName}' on {npc.characterName}");
    }
}
