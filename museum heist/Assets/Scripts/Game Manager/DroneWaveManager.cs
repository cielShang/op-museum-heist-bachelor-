using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class DroneWaveManager : MonoBehaviour
{
    
    [Header("Wave Settings")]
    public GameObject dronePrefab;          // small attack drone prefab
    public Transform[] spawnPoints;         // where they appear
    public int dronesToSpawn = 2;

    [Header("Target & Allies")]
    public Transform player;                // Player to chase
    public GuardDronePatrol mainGuard;      // main patrol guard
    public ConvaiNPC convaiNPC; // active NPC (Sakura or Mila)

[Header("Sakura Stun Settings")]
public float firstStunDelay = 2f;
public float stunInterval = 5f;
public float stunDurationCombat = 2f;  // ALWAYS used for wave drones


[Tooltip("Used ONLY when player chose keycard plan")]
public float stunDurationKeycard = 25f;


    [Header("Debug")]
    [HideInInspector]
    public bool waveSpawned = false;

    private readonly List<SimpleEnemyDrone> _activeDrones = new List<SimpleEnemyDrone>();
    private float _nextStunTime;

void Awake()
{
    if (convaiNPC == null && NPCBootstrapper.ActiveNPC != null)
        convaiNPC = NPCBootstrapper.ActiveNPC;

    waveSpawned = false;
}


    void Update()
    {
        if (!waveSpawned) return;

        // Regularly give Sakura a chance to stun a drone
        if (Time.time >= _nextStunTime && _activeDrones.Count > 0)
        {
            TrySakuraStunOneDrone();
        }
    }

    public void TriggerAlertWave()
    {
        if (waveSpawned)
        {
            Debug.Log("[DroneWaveManager] Wave already spawned.");
            return;
        }

        if (dronePrefab == null)
        {
            Debug.LogWarning("[DroneWaveManager] No dronePrefab assigned.");
            return;
        }

        // Ensure we have a player reference
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("[DroneWaveManager] No player assigned or found with tag 'Player'.");
            return;
        }

        // Ensure we have Sakura reference – fallback to GamePhaseManager if not assigned
        if (convaiNPC == null && GamePhaseManager.Instance != null)
        {
            convaiNPC = GamePhaseManager.Instance.convaiNPC;
            if (convaiNPC != null)
                Debug.Log("[DroneWaveManager] Fetched SakuraNPC from GamePhaseManager.");
        }

        waveSpawned = true;
        Debug.Log("[DroneWaveManager] Spawning alert wave...");

        _activeDrones.Clear();

        int count = 0;
        foreach (Transform spawn in spawnPoints)
        {
            if (count >= dronesToSpawn) break;
            if (spawn == null) continue;

            GameObject d = Instantiate(dronePrefab, spawn.position, spawn.rotation);
            var enemy = d.GetComponent<SimpleEnemyDrone>();
            if (enemy != null)
            {
                enemy.SetTarget(player);
                enemy.owner = this;          // so it can notify us on death
                _activeDrones.Add(enemy);
            }

            count++;
        }

        // Have the main guard join the fight
        if (mainGuard != null)
        {
            mainGuard.EnterAlertMode(player);
        }
        else
        {
            Debug.LogWarning("[DroneWaveManager] mainGuard not assigned – only wave drones will attack.");
        }

        // npc explains immediately that you've been spotted and she will taser drones
//     if (convaiNPC != null)
// {
//     SakuraFollowPlayer follow = convaiNPC.GetComponent<SakuraFollowPlayer>();
//     if (follow != null) follow.SetTalking(true);

//     convaiNPC.InterruptCharacterSpeech();
//     convaiNPC.TriggerSpeech(
//         "[ALERT][WHISPER] Here they come. Focus the ones I lock up."
//     );

//     StartCoroutine(EndTalkingAfterDelay(follow, 3f));
// }

//         else
//         {
//             Debug.LogWarning("[DroneWaveManager] convaiNPC is NULL – cannot play alert/taser line.");
//         }

        _nextStunTime = Time.time + firstStunDelay;
    }

    public void ForceClearAllEnemies(bool playDeath = true)
{
    Debug.Log($"[DroneWaveManager] ForceClearAllEnemies called on: {name}", this);

    waveSpawned = false;

    // Kill spawned drones
    _activeDrones.RemoveAll(d => d == null);

    foreach (var d in _activeDrones)
    {
        if (d == null) continue;

        if (playDeath) d.ForceDie();
        else Destroy(d.gameObject);
    }
    _activeDrones.Clear();

    // Always remove main guard if assigned (do NOT depend on isActive)
    Debug.Log($"[DroneWaveManager] mainGuard ref = {(mainGuard ? mainGuard.name : "NULL")}", this);

    // 2) remove main guard
if (mainGuard != null)
{
    Debug.Log($"[DroneWaveManager] Clearing assigned mainGuard: {mainGuard.name}", mainGuard);
    mainGuard.ForceRemoveNow();
}
else
{
    // Inspector wiring failed -> remove the closest guard to THIS manager
    GuardDronePatrol closest = FindClosestGuard(transform.position, 50f);
    if (closest != null)
    {
        Debug.Log($"[DroneWaveManager] mainGuard was NULL, removing closest guard: {closest.name}", closest);
        closest.ForceRemoveNow();
    }
    else
    {
        Debug.LogWarning("[DroneWaveManager] No GuardDronePatrol found to remove.");
    }
}
}
private GuardDronePatrol FindClosestGuard(Vector3 origin, float maxRadius)
{
    var guards = UnityEngine.Object.FindObjectsByType<GuardDronePatrol>(FindObjectsSortMode.None);
    GuardDronePatrol best = null;
    float bestDist = float.MaxValue;

    foreach (var g in guards)
    {
        if (g == null) continue;

        float d = Vector3.Distance(origin, g.transform.position);
        if (d < bestDist && d <= maxRadius)
        {
            bestDist = d;
            best = g;
        }
    }
    return best;
}






    /// <summary>
    /// Called by SimpleEnemyDrone.Die()
    /// </summary>
    public void NotifyDroneKilled(SimpleEnemyDrone drone)
    {
        if (drone == null) return;

        _activeDrones.Remove(drone);

        if (_activeDrones.Count == 0)
        {
            OnWaveCleared();
        }
    }

    private void OnWaveCleared()
    {
        waveSpawned = false;

        Debug.Log("[DroneWaveManager] All spawned drones defeated.");

        // Main guard should also be out of the picture now
        // if (mainGuard != null && mainGuard.isActive)
        // {
        //     mainGuard.DisableGuard();
        // }

        // if (convaiNPC != null)
        // {
        //     SakuraFollowPlayer follow = convaiNPC.GetComponent<SakuraFollowPlayer>();
        //     if (follow != null) follow.SetTalking(true);

        //     convaiNPC.InterruptCharacterSpeech();
        //     convaiNPC.TriggerSpeech(
        //         "[RELIEF][PRIDE] That’s all of them—and the main guard’s down too. " +
        //         "We never got his key, so we’ll have to crack the terminal ourselves."
        //     );

        //     StartCoroutine(EndTalkingAfterDelay(follow, 4f));
        // }
    }

    private void TrySakuraStunOneDrone()
    {
        _activeDrones.RemoveAll(d => d == null);

        SimpleEnemyDrone candidate = null;
        for (int i = 0; i < 10; i++)
        {
            var d = _activeDrones[Random.Range(0, _activeDrones.Count)];
            if (d != null && !d.IsStunned)
            {
                candidate = d;
                break;
            }
        }
        if (candidate == null) return;

        candidate.ApplyStun(stunDurationCombat);




        // Sakura calls it out
        // if (convaiNPC != null)
        // {
        //     SakuraFollowPlayer follow = convaiNPC.GetComponent<SakuraFollowPlayer>();
        //     if (follow != null) follow.SetTalking(true);

        //     convaiNPC.InterruptCharacterSpeech();
        //     convaiNPC.TriggerSpeech(
        //         "[EXCITED][WHISPER] Tasing that one—it’s frozen. Go for it while it can’t move!"
        //     );

        //     StartCoroutine(EndTalkingAfterDelay(follow, 3f));
        // }

        _nextStunTime = Time.time + stunInterval;
    }

    // private IEnumerator EndTalkingAfterDelay(SakuraFollowPlayer follow, float delay)
    // {
    //     if (follow == null) yield break;
    //     yield return new WaitForSeconds(delay);
    //     follow.SetTalking(false);
    // }
}
