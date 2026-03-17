using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    public CheckpointID checkpoint;
    public bool fireOnce = true;

    bool _fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired && fireOnce) return;
        if (!other.CompareTag("Player")) return;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetCheckpoint(checkpoint);

        _fired = true;

        
    }
}
