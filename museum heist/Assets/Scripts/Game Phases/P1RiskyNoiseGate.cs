using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P1RiskyNoiseGate : MonoBehaviour
{
    private bool _playerInside;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player")) return;

    //  Do NOT override UI if phase 2 already progressed
    if (GameStateManager.Instance.p2KeyTaken ||
        GameStateManager.Instance.p2TerminalCleared)
        return;

    var det = other.GetComponent<JumpLandingDetector>();
    if (det != null) det.SetNoiseSystemEnabled(true);
        
        ShowChoiceUI();
    GameRunLogger.Instance.PhaseComplete("Phase1", "JumpLanded"); // risky
    GameRunLogger.Instance.PhaseStart("Phase2");

    
}


    void OnTriggerStay(Collider other)
    {
        if (!_playerInside) return;
        if (!other.CompareTag("Player")) return;

        // Ensures UI shows even after respawn inside the trigger
        ShowChoiceUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = false;

        var det = other.GetComponent<JumpLandingDetector>();
        if (det != null)
            det.SetNoiseSystemEnabled(false);

        // Optional: hide UI when leaving this area
        if (ChoiceUIManager.Instance != null)
            ChoiceUIManager.Instance.Hide();
    }

    private void ShowChoiceUI()
    {
        if (ChoiceUIManager.Instance == null) return;

        ChoiceUIManager.Instance.Show(
           "BLUE: Steal the key from the guard by stunning him.",
            "ORANGE: Hack the terminal. Failure alerts strong enemies."
        );
    }
}
