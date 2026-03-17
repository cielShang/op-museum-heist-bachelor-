using System.Collections.Generic;
using UnityEngine;

public class EliteWaveManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject elitePrefab;
    public Transform[] spawnPoints;
    public int maxSimultaneousElites = 1;

    [Header("Debug")]
    public bool debugLogs = true;

    private Transform _player;
    private bool _defenseActive = false;

    private readonly List<EliteEnemy> _aliveElites = new List<EliteEnemy>();

    // Called from HackFailDefensePhase when defense starts
    public void BeginDefense(Transform player)
    {
        _player = player;
        _defenseActive = true;

        if (debugLogs)
            Debug.Log("[EliteWaveManager] Defense started.");

        SpawnWave();   // spawn first batch immediately
    }

    public void StopDefense()
    {
        _defenseActive = false;

        if (debugLogs)
            Debug.Log("[EliteWaveManager] Defense stopped – no more new elites.");
    }

    public void SpawnWave()
    {
        if (!_defenseActive) return;
        if (elitePrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        // maintain limit of simultaneous elites
        int aliveCount = _aliveElites.RemoveAll(e => e == null);
        aliveCount = _aliveElites.Count;

        if (aliveCount >= maxSimultaneousElites)
        {
            if (debugLogs) Debug.Log("[EliteWaveManager] Max elites already alive, not spawning new yet.");
            return;
        }

        if (debugLogs)
            Debug.Log("[EliteWaveManager] Spawning elite wave...");

        foreach (Transform sp in spawnPoints)
        {
            if (aliveCount >= maxSimultaneousElites) break;
            if (sp == null) continue;

            GameObject go = Instantiate(elitePrefab, sp.position, sp.rotation);
            EliteEnemy ee = go.GetComponent<EliteEnemy>();
            if (ee != null)
            {
                ee.waveManager = this;

                // tell him who to chase
                if (_player == null)
                {
                    GameObject p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) _player = p.transform;
                }
                ee.SetTarget(_player);

                ee.PlaySpawnTaunt();
                _aliveElites.Add(ee);
                aliveCount++;
            }
        }
    }

    // Called by EliteEnemy when it dies
    public void OnEliteDied(EliteEnemy enemy)
    {
        _aliveElites.Remove(enemy);

        if (debugLogs)
            Debug.Log("[EliteWaveManager] Elite died. Alive left: " + _aliveElites.Count);

        // If defense is still active, spawn another after a short delay
        if (_defenseActive)
        {
            // simple respawn; you could add a coroutine with delay if you like
            SpawnWave();
        }
    }

    // Used when Sakura finishes the hack successfully
    public void KillAllElites()
    {
        if (debugLogs)
            Debug.Log("[EliteWaveManager] Killing all remaining elites due to Sakura hack.");

        // First, disable defense so deaths don't cause new spawns
        _defenseActive = false;

        // Copy list so modification during iteration is safe
        EliteEnemy[] copy = _aliveElites.ToArray();
        foreach (EliteEnemy e in copy)
        {
            if (e != null)
            {
                // big damage so they play normal death animation
                e.TakeHit(9999);
            }
        }

        _aliveElites.Clear();
    }
}
