using UnityEngine;

public enum NPCPersonality { Sakura, Mila }

public class NPCSelectionManager : MonoBehaviour
{
    public static NPCSelectionManager Instance { get; private set; }

    public NPCPersonality Selected = NPCPersonality.Sakura;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("NPCSelectionManager");
        go.AddComponent<NPCSelectionManager>();
    }
}
