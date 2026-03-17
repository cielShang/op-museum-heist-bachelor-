using System.Collections.Generic;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

public static class NarrativeTriggerDebouncer
{
    // triggerName -> last fired time
    private static readonly Dictionary<string, float> _lastFired = new Dictionary<string, float>();

    // Optional: clear this when changing scenes/checkpoints
    public static void Clear() => _lastFired.Clear();

    public static bool TryFire(ConvaiNPC npc, string triggerName, float cooldownSeconds = 1.0f, Object context = null)
    {
        if (npc == null || string.IsNullOrEmpty(triggerName)) return false;

        float now = Time.time;

        if (_lastFired.TryGetValue(triggerName, out float last))
        {
            if (now - last < cooldownSeconds)
            {
                Debug.Log($"[NarrativeTriggerDebouncer] Blocked duplicate trigger '{triggerName}' ({now - last:F2}s since last)", context);
                return false;
            }
        }

        _lastFired[triggerName] = now;

        npc.InterruptCharacterSpeech();
        npc.TriggerEvent(triggerName);

        Debug.Log($"[NarrativeTriggerDebouncer] Fired '{triggerName}' on {npc.characterName}", context);
        return true;
    }
}
