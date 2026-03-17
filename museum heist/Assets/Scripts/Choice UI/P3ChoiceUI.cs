using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P3ChoiceUI : MonoBehaviour
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

        ShowChoiceUI();
    }

    void OnTriggerStay(Collider other)
    {
        if (!_playerInside) return;
        if (!other.CompareTag("Player")) return;

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
                "BLUE: Take the stealthy way through the lasers.",
                "ORANGE: Dash through the main hall and confront enemies."
        );
    }
}
