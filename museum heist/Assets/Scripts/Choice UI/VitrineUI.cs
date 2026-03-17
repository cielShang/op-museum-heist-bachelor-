using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VitrineUI : MonoBehaviour
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
                "Find a way to the treasure vitrine.",
                " "
        );
    }
}
