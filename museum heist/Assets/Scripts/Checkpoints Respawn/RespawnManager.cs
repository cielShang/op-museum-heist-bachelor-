using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Core;

public class RespawnManager : MonoBehaviour
{
    [System.Serializable]
    public class CheckpointSpawn
    {
        public CheckpointID id;
        public Transform playerSpawn;
        public Transform npcSpawn; // optional
  

    }

    [Header("Default spawns (used when no checkpoint yet)")]
    public Transform defaultPlayerSpawn;
    public Transform defaultNpcSpawn;

    [Header("Checkpoint spawns")]
    public List<CheckpointSpawn> checkpointSpawns = new List<CheckpointSpawn>();

    [Header("References")]
    public GameObject player;
    public ConvaiNPC npc; 
          private ConvaiNPC _lastNpc;
        private Coroutine _syncRoutine;
        private int _respawnTicket = 0;
private bool _syncRunning = false;
private int _lastNdTicketFired = -1;
private float _lastNdFireTime = -999f;
private const float ND_DOUBLE_FIRE_GUARD_SECONDS = 0.25f;


    Dictionary<CheckpointID, CheckpointSpawn> _map;

    void Awake()
    {
        QualitySettings.vSyncCount = 1;      // lock to display refresh (often 60)
    Application.targetFrameRate = 60;    // safety; can remove later

        _map = new Dictionary<CheckpointID, CheckpointSpawn>();
        foreach (var c in checkpointSpawns)
            if (c != null) _map[c.id] = c;
    }

void Start()
{
    if (player == null)
        player = GameObject.FindGameObjectWithTag("Player");

    DoRespawn(); // one unified entry point
}




public void DoRespawn()
{
    _respawnTicket++;                 // new “respawn cycle”
    TeleportPlayerToCheckpoint();     // immediate
    StartCoroutine(EnsureNpcThenTeleportAndSync(_respawnTicket));
}


    private void TeleportPlayerToCheckpoint()
    {
        var gsm = GameStateManager.Instance;

        Transform pSpawn = defaultPlayerSpawn;
        if (gsm != null && gsm.currentCheckpoint != CheckpointID.None &&
            _map.TryGetValue(gsm.currentCheckpoint, out var cp) &&
            cp.playerSpawn != null)
        {
            pSpawn = cp.playerSpawn;
        }

        if (player != null && pSpawn != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.SetPositionAndRotation(pSpawn.position, pSpawn.rotation);

            if (cc != null) cc.enabled = true;

            // your lives/logging block kept as-is
            var gsm2 = GameStateManager.Instance;
            bool firstSpawn = (gsm2 != null && gsm2.isNewGame);

            var ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                if (firstSpawn) ph.SetLives(ph.maxLives);
                else ph.SetLives(3);
            }

            string spawnName = "DefaultSpawn";
            if (gsm2 != null && gsm2.currentCheckpoint != CheckpointID.None)
                spawnName = gsm2.currentCheckpoint.ToString();

            Vector3 spawnPos = pSpawn.position;

            if (firstSpawn)
                GameRunLogger.Instance?.PlayerSpawned(spawnName, spawnPos);
            else
                GameRunLogger.Instance?.PlayerRespawnedAfterDeath(spawnName, spawnPos);

            if (gsm2 != null && firstSpawn)
                gsm2.isNewGame = false;
        }
    }

private IEnumerator EnsureNpcThenTeleportAndSync(int ticket)
{
    if (_syncRunning) yield break;
    _syncRunning = true;

    // Wait until NPC exists
    float timeout = 5f;
    while (NPCBootstrapper.ActiveNPC == null && timeout > 0f)
    {
        timeout -= Time.deltaTime;
        yield return null;
    }

    npc = NPCBootstrapper.ActiveNPC;
    if (npc == null)
    {
        Debug.LogWarning("[Respawn] ActiveNPC not found (timeout). NPC teleport/ND sync skipped.");
        _syncRunning = false;
        yield break;
    }

    
    yield return null;

    // If a NEW respawn started while we were waiting stop this one
    if (ticket != _respawnTicket)
    {
        _syncRunning = false;
        yield break;
    }

    TeleportNpcToCheckpoint_WarpSafe();

    yield return new WaitForSeconds(0.05f);

    // UI sync is always ok
    SyncChoiceUIToCheckpoint();

    //  ND should happen EVERY respawn, but never twice per respawn
    FireNarrativeOncePerRespawn(ticket);

    _syncRunning = false;
}

private void FireNarrativeOncePerRespawn(int ticket)
{
    // already fired for this respawn
    if (_lastNdTicketFired == ticket) return;

    
    if (Time.time - _lastNdFireTime < ND_DOUBLE_FIRE_GUARD_SECONDS) return;

    SyncNarrativeToCheckpoint();

    _lastNdTicketFired = ticket;
    _lastNdFireTime = Time.time;
}

    private void TeleportNpcToCheckpoint_WarpSafe()
    {
        var gsm = GameStateManager.Instance;

        Transform pSpawn = defaultPlayerSpawn;
        Transform nSpawn = defaultNpcSpawn;

        // If no defaultNpcSpawn set, fall back to player default
        if (nSpawn == null) nSpawn = defaultPlayerSpawn;

        if (gsm != null && gsm.currentCheckpoint != CheckpointID.None &&
            _map.TryGetValue(gsm.currentCheckpoint, out var cp))
        {
            if (cp.playerSpawn != null) pSpawn = cp.playerSpawn;

            //  if npcSpawn is not assigned, fall back to playerSpawn
            if (cp.npcSpawn != null) nSpawn = cp.npcSpawn;
            else if (cp.playerSpawn != null) nSpawn = cp.playerSpawn;
        }

        if (npc == null || nSpawn == null) return;

        
        var agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.Warp(nSpawn.position);
            npc.transform.rotation = nSpawn.rotation;
        }
        else
        {
            npc.transform.SetPositionAndRotation(nSpawn.position, nSpawn.rotation);
        }

        Debug.Log($"[Respawn] NPC moved to {gsm?.currentCheckpoint} at {nSpawn.position}");
    }

void SyncNarrativeToCheckpoint()
{
    var activeNpc = NPCBootstrapper.ActiveNPC;
    var gsm = GameStateManager.Instance;
    if (activeNpc == null || gsm == null) return;

    string trigger = null;

    switch (gsm.currentCheckpoint)
    {
        case CheckpointID.P1_Safe:
            trigger = "P1_SAFE_REACH_LASERS";
            break;

        case CheckpointID.P1_Risky:
            trigger = "P1_LANDED_GOOD";
            break;

        case CheckpointID.P2_DoneSafe:
        case CheckpointID.P2_DoneRisky:
            trigger = "P2_SUCCESS";
            break;

        case CheckpointID.P3_SafeLeft:
        case CheckpointID.P3_SafeRight:
            trigger = "P3_CHOICE";
            break;
    }

    if (!string.IsNullOrEmpty(trigger))
    {
        Debug.Log($"[Respawn] Syncing narrative → {trigger}");
        activeNpc.InterruptCharacterSpeech();
        activeNpc.TriggerEvent(trigger);
    }
}


    void SyncChoiceUIToCheckpoint()
    {
        if (ChoiceUIManager.Instance == null) return;

        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        switch (gsm.currentCheckpoint)
        {
            case CheckpointID.None:
                ChoiceUIManager.Instance.Show(
                    "BLUE: Sneak through the corridor",
                    "ORANGE: Go up the stairs to jump down. Be careful not to be seen."
                );
                break;

            case CheckpointID.P1_Safe:
            case CheckpointID.P1_Risky:
            case CheckpointID.P2_DoneSafe:
            case CheckpointID.P2_DoneRisky:
                if (gsm.p2TerminalCleared)
                {
                    ChoiceUIManager.Instance.Show("Lasers are down. Go down the stairs", ".");
                }
                else if (gsm.p2KeyTaken)
                {
                    ChoiceUIManager.Instance.Show("BLUE: Use keycard on terminal", ".");
                }
                else
                {
                    ChoiceUIManager.Instance.Show(
                        "BLUE: Stun the guards by short circuiting the system.",
                        "ORANGE: Hack the terminal. Failure will spawn enemies."
                    );
                }
                break;

            case CheckpointID.P3_SafeLeft:
            case CheckpointID.P3_SafeRight:
                ChoiceUIManager.Instance.Show(
                    "BLUE: Take the stealthy way through the lasers.",
                    "ORANGE: Dash through the main hall and confront enemies."
                );
                break;
        }
    }
}
