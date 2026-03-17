using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class Phase3RiskyZone : MonoBehaviour
{
    [Header("Wiring")]
    public Phase3RiskyTopDownManager manager;

    [Header("Tags")]
    public string playerTag = "Player";

    [Header("Convai Trigger")]
    public string riskyStartTrigger = "P3_RISKY_START";

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only player
        if (!other.CompareTag(playerTag)) return;

        // Only once
        if (_fired) return;

        // If phase 3 route already chosen, do nothing
        if (Phase3Manager.Instance != null && Phase3Manager.Instance.RouteChosen) return;

        _fired = true;

        // Choose route
        if (Phase3Manager.Instance != null)
        {
            Phase3Manager.Instance.activeRoute = Phase3Route.Risky;
            if (debugLogs) Debug.Log("[Phase3] Risky route chosen.", this);
            if (GameRunLogger.Instance != null)
    GameRunLogger.Instance.Log("Phase3", "Choice", "PathChosen", "Risky");

        }
        else
        {
            Debug.LogWarning("[Phase3] Phase3Manager.Instance is null (route not stored).", this);
        }

        // Fire narrative design trigger
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null && !string.IsNullOrEmpty(riskyStartTrigger))
        {
            npc.TriggerEvent(riskyStartTrigger);
            if (debugLogs) Debug.Log($"[Phase3] Fired ND trigger: {riskyStartTrigger}", this);
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[Phase3] Could not fire ND trigger (NPC null or trigger empty).", this);
        }

        // Start risky top-down phase
        if (manager != null)
        {
            manager.StartRiskyPhase3();
            if (debugLogs) Debug.Log("[Phase3] Player entered -> starting top-down risky phase.", this);
        }
        else
        {
            Debug.LogError("[Phase3] RiskyTopDownManager not assigned on Phase3RiskyZone. Top-down will NOT start.", this);
            // IMPORTANT: We don't disable zones if manager is missing, so you aren't stuck.
            _fired = false;
            return;
        }

        // Disable all choice zones AFTER successful start
        Phase3ChoiceZoneDisabler.DisableAll();
    }
}
