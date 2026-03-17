using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LootPickup : MonoBehaviour
{
    public int lootValue = 1;

    public AudioSource audioSource;
    public AudioClip pickupClip;

    LootId _lootId;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        _lootId = GetComponent<LootId>();
    }

    void Start()
    {
        
        if (GameStateManager.Instance != null && _lootId != null)
        {
            if (!string.IsNullOrEmpty(_lootId.id) &&
                GameStateManager.Instance.collectedLoot.Contains(_lootId.id))
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        string id = _lootId != null ? _lootId.id : "";

        if (GameStateManager.Instance != null)
            // existing
        GameStateManager.Instance.AddLoot(id, lootValue);

       
        if (GameRunLogger.Instance != null)
            GameRunLogger.Instance.LootCollected(id, lootValue, phase: "Game"); // or Phase2/Phase1 depending on where it is


        if (audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip);

        Destroy(gameObject);
    }
}
