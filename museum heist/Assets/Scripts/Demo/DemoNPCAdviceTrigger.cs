using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class DemoNPCAdviceTrigger : MonoBehaviour
{
    public string adviceTrigger = "DEMO_NPC_ADVICE";
    bool _fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag("Player")) return;

        _fired = true;

        if (NPCBootstrapper.ActiveNPC != null)
            NPCBootstrapper.ActiveNPC.TriggerEvent(adviceTrigger);

        DemoManager.Instance.SetObjective(
            "Once you're ready go to the exit. (green light)"
        );
    }
}
