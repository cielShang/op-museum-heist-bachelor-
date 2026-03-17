using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpOutcomeTrigger : MonoBehaviour
{
    public JumpOutcomeState.Outcome setsOutcome = JumpOutcomeState.Outcome.Good;
    public string playerTag = "Player";
    public bool debugLogs = true;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // already decided -> ignore all other landing triggers
        if (JumpOutcomeState.Current != JumpOutcomeState.Outcome.None) return;

        JumpOutcomeState.Current = setsOutcome;

        if (debugLogs)
            Debug.Log($"[JumpOutcomeTrigger] Outcome locked: {JumpOutcomeState.Current}", this);
    }
}
