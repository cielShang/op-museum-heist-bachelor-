using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class RiskyPhase2Trigger : MonoBehaviour
{
    [Header("Who should trigger this?")]
    public string npcTag = "Character";          // Sakura's tag

    [Header("Optional references")]
    public ConvaiNPC convaiNPC;                  // if left empty, will try GamePhaseManager.convaiNPC
    public LaserGateController laserGate;        // laser gate for the middle-floor stairs (optional)
    public TerminalInteractable terminal;        // terminal that controls this laser gate (optional)

    private bool _hasTriggered = false;

    void Reset()
    {
        // Make sure this collider is a trigger by default
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag(npcTag)) return;

        _hasTriggered = true;

        // Get Convai NPC, preferably from inspector, else from GamePhaseManager
        if (convaiNPC == null && GamePhaseManager.Instance != null)
            convaiNPC = GamePhaseManager.Instance.convaiNPC;

        // 1) Update game phase to Phase 2 (jump route)
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.SetPhase(
                HeistPhase.Phase2,
                "Middle floor in front of the laser-protected stairs (jump route).",
                "Decide how to bypass the laser barrier: either hack the nearby terminal for a risky but fast shutdown, or silently take down the patrolling drone to steal its keycard and safely disable the lasers."
            );
        }

        // 2) Tell Sakura to react dynamically to the jump + explain options
        // if (convaiNPC != null)
        // {
        //     convaiNPC.InterruptCharacterSpeech();

        //     // No fixed line – just instructions
        //     string prompt =
        //         "[JUMP_REACTION] You and the player have just taken the risky jump route and landed safely " +
        //         "on the middle floor in front of a set of stairs blocked by security lasers. " +
        //         "React in a thrilled, proud, energized way about the successful jump. Keep it short and natural. " +
        //         "Then, in the same reply, clearly explain that this is now Phase 2 of the heist: " +
        //         "you are on the middle floor, in front of laser-protected stairs to the grand hall. " +
        //         "Explain that the player has two options to deal with these lasers: " +
        //         "1) risky: hack the nearby security terminal to temporarily disable the lasers (like the other set of stairs), " +
        //         "2) safe: sneak up on the patrolling drone, silently take it down, and use its keycard to safely turn off the lasers.";

        //     convaiNPC.TriggerSpeech(prompt);
        // }

        // 
        // The existing LaserGateController + TerminalInteractable logic will handle
        // the actual disarming when the player interacts with them.
    }
}
