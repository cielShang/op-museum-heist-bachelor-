using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DemoLootPickup : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip pickupClip;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip);

        DemoManager.Instance.SetObjective("Continue to the combat area.");

        Destroy(gameObject);
    }
}
