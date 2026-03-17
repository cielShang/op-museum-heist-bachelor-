using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Checkpoint")]
    public CheckpointID currentCheckpoint = CheckpointID.None;

    [Header("NPC Selection")]
    public string selectedNPC; // "Mila" / "Sakura"

    [Header("Loot")]
    public int lootCount = 0;
    public HashSet<string> collectedLoot = new HashSet<string>();

    [Header("Respawn Lives")]
    public int pendingLivesOnSpawn = 0;

    public bool isNewGame = true;
    public bool p2KeyTaken = false;
    public bool p2TerminalCleared = false; // set true after hack success OR keycard override success



    void Awake()
    {
        QualitySettings.vSyncCount = 1;      
    Application.targetFrameRate = 60;    // safety; can remove later

        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCheckpoint(CheckpointID cp)
    {
        currentCheckpoint = cp;
        Debug.Log($"[GameState] Checkpoint set → {cp}");
    }

    public void AddLoot(string lootId, int amount)
    {
        if (!string.IsNullOrEmpty(lootId))
            collectedLoot.Add(lootId);

        lootCount += amount;
        Debug.Log($"[GameState] Loot +{amount} (Total {lootCount})");
    }

    // call this from menu "New Game" button
    public void ResetForNewGame()
{
    currentCheckpoint = CheckpointID.None;

    lootCount = 0;
    collectedLoot.Clear();

    pendingLivesOnSpawn = 0;

    
    isNewGame = true;
    p2KeyTaken = false;
    p2TerminalCleared = false;

    Debug.Log("[GameState] ResetForNewGame()");
}

}
