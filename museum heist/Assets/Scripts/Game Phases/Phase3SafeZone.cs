using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class Phase3SafeZone : MonoBehaviour
{
    [Header("Wiring")]
    public Phase3SafeRouteManager manager;   // drag the correct side manager here
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        if (Phase3Manager.Instance != null && Phase3Manager.Instance.RouteChosen) return;

        _fired = true;

        if (Phase3Manager.Instance != null)
            Phase3Manager.Instance.activeRoute = Phase3Route.Safe;

        if (debugLogs) Debug.Log("[Phase3] Safe route chosen.", this);

        if (GameRunLogger.Instance != null)
    GameRunLogger.Instance.Log("Phase3", "Choice", "PathChosen", "Safe");

        // Mila explains safe plan
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null)
            npc.TriggerEvent("P3_SAFE_START");

        // prevent choosing the other route afterwards
        Phase3ChoiceZoneDisabler.DisableAll();

        // start the safe mechanics
        if (manager != null)
        {
            manager.StartSafeRoute();
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[Phase3SafeZone] No Phase3SafeRouteManager assigned!", this);
        }
    }
}
