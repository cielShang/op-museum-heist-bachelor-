using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class Phase2Trigger : MonoBehaviour
{
    [TextArea] public string locationDescription =
        "Middle floor: in front of laser-protected corridor to the main hall.";

    [TextArea] public string nextStep =
        "Bypass/disable the laser field to access the artifact area, then move toward the main hall.";

    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("Optional: leave empty, it will use active NPC")]
    public ConvaiNPC convaiNPC;

    private bool _fired = false;

    void Start()
    {
       
        if (convaiNPC == null && NPCBootstrapper.ActiveNPC != null)
            convaiNPC = NPCBootstrapper.ActiveNPC;

        if (convaiNPC == null && GamePhaseManager.Instance != null)
            convaiNPC = GamePhaseManager.Instance.convaiNPC;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;
        Debug.Log("[Phase2Trigger] Player entered: switching to Phase 2.");

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.SetPhase(
                HeistPhase.Phase2,
                location: locationDescription,
                next: nextStep
            );
        }

        // if (convaiNPC == null) return;

        // convaiNPC.InterruptCharacterSpeech();

        // string prompt =
        //     "[CONTEXT] The 'key', 'card', 'key card' always refers to the guard's laser override keycard. " +
        //     "[WHISPER] We’re at the stairwell to the main hall. Lasers block the route and a patrol drone is on watch. " +
        //     "Explain the two options in 1–2 short sentences: " +
        //     "SAFE: you stun/paralyze the drone, player takes it down and uses the keycard for a clean laser shutdown. " +
        //     "RISKY: player hacks the terminal under time pressure for a temporary shutdown with consequences on failure. " +
        //     "Then nudge the player according to YOUR personality (Sakura = risk/thrill, Mila = caution/safety). Keep it short.";

        // convaiNPC.TriggerSpeech(prompt);
    }
}
