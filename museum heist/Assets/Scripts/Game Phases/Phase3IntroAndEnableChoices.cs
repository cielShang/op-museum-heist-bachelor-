using System.Collections;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class Phase3IntroAndEnableChoices : MonoBehaviour
{
    [Header("Convai Narrative Trigger Name")]
    public string narrativeTriggerName = "P3_CHOICE";

    [Header("Enable these after intro")]
    public GameObject riskyZone;
    public GameObject safeZone;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Timing")]
    public float enableDelay = 0.6f; // give Mila time to start speaking

    [Header("Debug")]
    public bool debugLogs = true;

    bool _fired;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
{
    // default: disabled until intro fires
    if (riskyZone != null) riskyZone.SetActive(false);
    if (safeZone != null)  safeZone.SetActive(false);

    var gsm = GameStateManager.Instance;
    if (gsm == null) return;

    //  If we respawned into Phase 3, enable immediately
    if (gsm.currentCheckpoint == CheckpointID.P3_SafeLeft ||
        gsm.currentCheckpoint == CheckpointID.P3_SafeRight)
    {
        if (debugLogs)
            Debug.Log("[P3Intro] Respawned into Phase 3 checkpoint - enabling choice zones immediately.", this);

        if (riskyZone != null) riskyZone.SetActive(true);
        if (safeZone != null)  safeZone.SetActive(true);

        _fired = true; // prevent intro trigger from firing again
    }
}



    void OnTriggerEnter(Collider other)
    {
            ChoiceUIManager.Instance.Show(
            "BLUE: Take the stealthy way through the lasers.",
            "ORANGE: Dash through the main hall and confront enemies."
        );

        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;

        if (debugLogs) Debug.Log("[P3Intro] Player entered main hall intro trigger.", this);

        GameRunLogger.Instance.PhaseStart("Phase3");


        StartCoroutine(FireNarrativeThenEnable());
    }

    IEnumerator FireNarrativeThenEnable()
    {
        // wait a frame so ActiveNPC is ready after scene load / transitions
        yield return null;

        ConvaiNPC npc = NPCBootstrapper.ActiveNPC;
        if (npc == null)
        {
            Debug.LogError("[P3Intro] ActiveNPC is null - cannot fire narrative trigger.", this);
        }
        else
        {
            if (debugLogs) Debug.Log("[P3Intro] Firing ND trigger: " + narrativeTriggerName + " on " + npc.characterName, this);
            npc.InterruptCharacterSpeech();
            npc.TriggerEvent(narrativeTriggerName);
        }

        // enable choice zones after a small delay (so it feels intentional)
        yield return new WaitForSeconds(enableDelay);

        if (riskyZone != null) riskyZone.SetActive(true);
        if (safeZone != null)  safeZone.SetActive(true);

        if (debugLogs) Debug.Log("[P3Intro] Choice zones enabled.", this);
    }
}
