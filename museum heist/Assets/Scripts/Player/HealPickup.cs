using UnityEngine;

public class HealPickup : MonoBehaviour
{
    public int healAmount = 2;
    public AudioSource audioSource;
    public AudioClip pickupClip;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.AddLives(healAmount);
            if (audioSource && pickupClip) audioSource.PlayOneShot(pickupClip);
            Destroy(gameObject);
        }
    }
}
