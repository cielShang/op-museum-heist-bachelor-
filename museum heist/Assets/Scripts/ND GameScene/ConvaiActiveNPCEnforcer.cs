using System.Collections;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class ConvaiActiveNPCEnforcer : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLogs = true;

    // Cache so we don't scan the whole scene repeatedly
    private static MonoBehaviour _cachedConvaiNpcManager;

    IEnumerator Start()
    {
        // wait a bit so Convai singletons are initialized after scene load
        yield return null;
        yield return null;
        yield return null;

        EnsureActiveNPC();
    }

    public void EnsureActiveNPC()
    {
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            if (debugLogs) Debug.LogWarning("[ConvaiActiveNPCEnforcer] ActiveNPC is null.");
            return;
        }

        // Use cached manager if we already found it
        MonoBehaviour convaiManager = _cachedConvaiNpcManager;
        if (convaiManager == null)
        {
            // Find all MonoBehaviours (including inactive) WITHOUT sorting (faster)
         var managers = Object.FindObjectsByType<MonoBehaviour>(
    FindObjectsInactive.Include,
    FindObjectsSortMode.None
);


            foreach (var m in managers)
            {
                if (m == null) continue;

                // Match by type name so we don't hard-depend on Convai types
                if (m.GetType().Name == "ConvaiNPCManager")
                {
                    convaiManager = m;
                    _cachedConvaiNpcManager = m;
                    break;
                }
            }
        }

        if (convaiManager == null)
        {
            if (debugLogs) Debug.LogWarning("[ConvaiActiveNPCEnforcer] ConvaiNPCManager not found in this scene.");
            return;
        }

        if (debugLogs) Debug.Log("[ConvaiActiveNPCEnforcer] Setting active NPC to: " + npc.characterName);

        // Try common method names via SendMessage so we don't depend on exact API
        convaiManager.SendMessage("SetActiveNPC", npc, SendMessageOptions.DontRequireReceiver);
        convaiManager.SendMessage("ChangeActiveNPC", npc, SendMessageOptions.DontRequireReceiver);
        convaiManager.SendMessage("SetCurrentNPC", npc, SendMessageOptions.DontRequireReceiver);
        convaiManager.SendMessage("SetNPCActive", npc, SendMessageOptions.DontRequireReceiver);
    }
}
