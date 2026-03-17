using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P2ChoiceGate : MonoBehaviour
{
    private bool _playerInside;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = true;

        // do NOT override UI if phase 2 already progressed
        if (GameStateManager.Instance.p2KeyTaken ||
            GameStateManager.Instance.p2TerminalCleared)
            return;

        ShowChoiceUI();

        GameRunLogger.Instance.PhaseComplete("Phase1", "CorridorPassed"); // safe


    }

    void OnTriggerStay(Collider other)
    {
        if (!_playerInside) return;
        if (!other.CompareTag("Player")) return;

        // Ensures UI shows after respawn inside trigger
        if (GameStateManager.Instance.p2KeyTaken ||
            GameStateManager.Instance.p2TerminalCleared)
            return;

        ShowChoiceUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = false;

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
