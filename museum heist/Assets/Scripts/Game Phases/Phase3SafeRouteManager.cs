using System.Collections.Generic;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class Phase3SafeRouteManager : MonoBehaviour
{
    [Header("Route ID (for debugging)")]
    public string routeId = "SAFE_A";

    [Header("Convai Triggers")]
    public string ndStartTrigger = "P3_SAFE_START";
    public string ndAllBypassedTrigger = "P3_SAFE_CLEAR";

    [Header("Laser Sections (one controller per section)")]
    public List<P3LaserGroupPatternController> laserSections = new List<P3LaserGroupPatternController>();

    [Header("Bypass Nodes (press E to cycle patterns)")]
    public List<P3BypassNodeInteract> bypassNodes = new List<P3BypassNodeInteract>();

    [Header("UI (optional)")]
    public TMPro.TMP_Text objectiveText;

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _active;

    public bool IsActive => _active;

    public void StartSafeRoute()
    {
        if (_active) return;
        _active = true;

        if (debugLogs) Debug.Log($"[P3 Safe] StartSafeRoute() route={routeId}", this);

        // Enable all laser sections at the start
        foreach (var s in laserSections)
        {
            if (s == null) continue;
            s.permanentlyCleared = false;
            s.SetSectionEnabled(true);
        }

        // Ensure bypass nodes are active (no manager assignment anymore)
        foreach (var n in bypassNodes)
        {
            if (n == null) continue;
            n.gameObject.SetActive(true);
        }

        SetObjective("SAFE ROUTE: Reroute lasers to pass each section");

        // Fire ND
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null && !string.IsNullOrEmpty(ndStartTrigger))
            npc.TriggerEvent(ndStartTrigger);
    }

    // Call this from the LAST checkpoint if you want the route to be considered “cleared”.
    public void CompleteSafeRoute()
    {
        if (!_active) return;

        if (debugLogs) Debug.Log("[P3 Safe] CompleteSafeRoute()", this);

        SetObjective("Proceed to the vitrine");

        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null && !string.IsNullOrEmpty(ndAllBypassedTrigger))
            npc.TriggerEvent(ndAllBypassedTrigger);
    }

    private void SetObjective(string msg)
    {
        if (objectiveText != null)
            objectiveText.text = msg;
    }
}
