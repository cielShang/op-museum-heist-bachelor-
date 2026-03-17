using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class TerminalInteractable : MonoBehaviour
{

    [Header("Combat / Guarding (optional)")]
    public DroneWaveManager localWaveManager;

    [Header("Wiring")]
    public LaserGateController laserGate;    // gate this terminal controls
    public ConvaiNPC convaiNPC;              // Sakura
    public HackMinigame hackMinigame;        // shared hack panel
    public string playerTag = "Player";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip keycardOverrideClip;

    bool _inside = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            _inside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            _inside = false;
    }

    void Update()
    {
        // Must be inside trigger and have a gate
        if (!_inside || laserGate == null) return;

        if (!Input.GetKeyDown(KeyCode.E)) return;

        var gm = GamePhaseManager.Instance;

        // 1) SAFE ROUTE: use guard keycard if we have one
        if (gm != null && gm.hasGuardKeycard)
        {
            gm.UseGuardKeycard();

            // disable this specific gate
            laserGate.DisableForSeconds(laserGate.hackDisableSeconds, "Guard keycard override");

            // play terminal sound
            if (audioSource != null && keycardOverrideClip != null)
                audioSource.PlayOneShot(keycardOverrideClip);

        if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                "Lasers are down.",
                "Move forward to the stairs."
            );
            GameRunLogger.Instance.PhaseComplete("Phase2", "KeycardUsed");

        }


            // if (convaiNPC != null)
            // {
            //     convaiNPC.InterruptCharacterSpeech();
            //     convaiNPC.TriggerSpeech(
            //         "[SECURE] Keycard override accepted. No alarms tripped – lasers are down. Let's move."
            //     );
            // }

            return; // done for this press
        }

// 2) RISKY ROUTE: hack via minigame
if (hackMinigame == null) return;
if (hackMinigame.IsActive) return;       // already hacking
if (hackMinigame.IsCompleted) return;    // this gate already hacked once

// Let the hack know which terminal triggered it
hackMinigame.currentTerminal = this;

// One press -> start hack immediately
hackMinigame.StartHack(laserGate);

    }
}
