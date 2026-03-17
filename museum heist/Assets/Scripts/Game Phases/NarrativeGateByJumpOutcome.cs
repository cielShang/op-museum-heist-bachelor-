using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class NarrativeGateByJumpOutcome : MonoBehaviour
{
    public JumpOutcomeState.Outcome requiredOutcome = JumpOutcomeState.Outcome.Good;
    public string triggerName; // e.g. P1_LANDED_GOOD or P1_BAD_LANDING
    public bool debugLogs = true;

    bool _fired;

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag("Player")) return;
        if (other.GetComponent<CharacterController>() == null) return;


        if (JumpOutcomeState.Current != requiredOutcome)
        {
            if (debugLogs) Debug.Log($"[NarrativeGateByJumpOutcome] Blocked {triggerName} because outcome is {JumpOutcomeState.Current}", this);
            return;
        }

        _fired = true;

        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            if (debugLogs) Debug.LogError("[NarrativeGateByJumpOutcome] ActiveNPC is null.", this);
            return;
        }

NarrativeTriggerDebouncer.TryFire(npc, triggerName, 1.0f, this);


        if (debugLogs) Debug.Log($"[NarrativeGateByJumpOutcome] Fired {triggerName} for {npc.characterName}", this);
    }
}
