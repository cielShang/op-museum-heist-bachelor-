using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DemoTrigger : MonoBehaviour
{
    public enum DemoStep
    {
        MovementDone,
        InteractionDone,
        SafeChosen,
        RiskyChosen,
        LootDemo,
        DemoEnd
    }

    public DemoStep step;
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

        _fired = true;
        HandleStep();
    }

    void HandleStep()
    {
        switch (step)
        {
            case DemoStep.MovementDone:
                DemoManager.Instance.SetObjective("Press E to interact with objects.");
                break;

            case DemoStep.InteractionDone:
                DemoManager.Instance.SetObjective("Choose a way: look for the orange or blue light on the floor.");
                break;

            case DemoStep.SafeChosen:
               // DemoManager.Instance.FireND(DemoManager.Instance.demoSafeChoiceTrigger);
                DemoManager.Instance.SetObjective("Good choice. Proceed.");
                break;

            case DemoStep.RiskyChosen:
                // DemoManager.Instance.FireND(DemoManager.Instance.demoRiskyChoiceTrigger);
                DemoManager.Instance.SetObjective("Good choice. Proceed.");
                break;
            
            case DemoStep.LootDemo:
                // DemoManager.Instance.FireND(DemoManager.Instance.demoRiskyChoiceTrigger);
                DemoManager.Instance.SetObjective("Collect loot by touching it.");
                break;

            case DemoStep.DemoEnd:
                DemoManager.Instance.SetObjective("Demo complete.");
                //DemoManager.Instance.FinishDemo();
                break;
        }
    }
}
