using System.Collections;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class Phase1AutoNarrativeTrigger : MonoBehaviour
{
    [Header("Convai Trigger Name (must match your Narrative Design Trigger)")]
    public string triggerName = "P1_START";

    [Header("Tags")]
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLogs = true;

    private Collider _col;
    private bool _fired;

    void Awake()
    {
    QualitySettings.vSyncCount = 1;      // lock to display refresh (often 60)
    Application.targetFrameRate = 60;    // safety; can remove later


        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    IEnumerator Start()
    {
        // Give scene bootstrappers time to set ActiveNPC + Convai init
        yield return null;
        yield return null;

        // If player spawns inside fire once
        TryFireIfPlayerInside();

        
        if (!_fired)
        {
            if (debugLogs) Debug.Log("[Phase1AutoNarrativeTrigger] Refreshing trigger collider...", this);
            _col.enabled = false;
            yield return null;
            _col.enabled = true;

            TryFireIfPlayerInside();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;
        Fire();
    }

    private void TryFireIfPlayerInside()
    {
        if (_fired) return;

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            if (debugLogs) Debug.LogWarning("[Phase1AutoNarrativeTrigger] Player not found yet.", this);
            return;
        }

        bool inside = _col.bounds.Contains(player.transform.position);
        if (debugLogs) Debug.Log("[Phase1AutoNarrativeTrigger] Player inside trigger? " + inside, this);

        if (inside) Fire();
    }

    private void Fire()
{
    if (_fired) return;

    // reset jump outcome at start of phase 1
    JumpOutcomeState.Reset();
    _fired = true;

    ConvaiNPC npc = NPCBootstrapper.ActiveNPC;
    if (npc == null)
    {
        if (debugLogs) Debug.LogError("[Phase1AutoNarrativeTrigger] ActiveNPC is NULL.", this);
        return;
    }

    if (debugLogs) Debug.Log("[Phase1AutoNarrativeTrigger] Preparing to fire: " + triggerName + " on " + npc.characterName, this);

    StartCoroutine(FireWhenReady(npc));
}

private IEnumerator FireWhenReady(ConvaiNPC npc)
{
    var enforcer = Object.FindFirstObjectByType<ConvaiActiveNPCEnforcer>();
    if (enforcer != null) enforcer.EnsureActiveNPC();

    yield return new WaitForSeconds(0.2f);

    npc.InterruptCharacterSpeech();
    npc.TriggerEvent(triggerName);
}



}
