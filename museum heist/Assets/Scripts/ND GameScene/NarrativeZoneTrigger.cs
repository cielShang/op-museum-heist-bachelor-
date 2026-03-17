using System.Collections;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class NarrativeZoneTrigger : MonoBehaviour
{
    [Header("Convai Trigger Name (matches Narrative Design trigger)")]
    public string triggerName;

    [Header("Who triggers this zone?")]
    public string triggeringTag = "Player";

    [Header("Behavior")]
    public bool fireOnce = true;
    public float fireDelay = 0.2f;
    public float autoStopAfter = 0f; // 0 = don't force stop

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _fired;
    private Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fireOnce && _fired) return;
        if (!other.CompareTag(triggeringTag)) return;
        if (other.GetComponent<CharacterController>() == null) return;


        Fire();
    }

    private void Fire()
    {
        if (fireOnce && _fired) return;
        _fired = true;

        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            if (debugLogs) Debug.LogError($"[NarrativeZoneTrigger] ActiveNPC is NULL (trigger {triggerName})", this);
            return;
        }

        if (debugLogs) Debug.Log($"[NarrativeZoneTrigger] Firing {triggerName} on {npc.characterName}", this);
        StartCoroutine(FireRoutine(npc));
    }

    private IEnumerator FireRoutine(ConvaiNPC npc)
    {
        yield return new WaitForSeconds(fireDelay);

NarrativeTriggerDebouncer.TryFire(npc, triggerName, 1.0f, this);


        if (autoStopAfter > 0f)
        {
            yield return new WaitForSeconds(autoStopAfter);
            npc.InterruptCharacterSpeech();
        }
    }
}
