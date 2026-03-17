using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P3VitrineUnlockZone : MonoBehaviour
{
    public Phase3RiskyTopDownManager manager;
    public string playerTag = "Player";

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (manager != null) manager.SetPlayerInVitrineUnlockZone(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (manager != null) manager.SetPlayerInVitrineUnlockZone(false);
    }
}
