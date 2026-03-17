using UnityEngine;

public class P2TrigerUI : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Tag used by the player GameObject")]
    public string playerTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;

        if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                "BLUE: Steal the key by stunning the guard's system.",
                "ORANGE: Hack the terminal"
            );
        }
        else
        {
            Debug.LogWarning("ChoiceUIManager.Instance is null!");
        }
    }
}
