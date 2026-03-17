using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DemoEndTrigger : MonoBehaviour
{
    public DemoEndUI demoEndUI;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        demoEndUI.Show();
    }
}
