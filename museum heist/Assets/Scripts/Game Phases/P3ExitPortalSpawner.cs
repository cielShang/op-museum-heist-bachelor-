using System.Collections;
using UnityEngine;

public class P3ExitPortalSpawner : MonoBehaviour
{
    [Header("Portal Prefab")]
    public GameObject portalPrefab;      // your light orb prefab
    public Transform portalSpawnPoint;   // empty transform near vitrine
    public float spawnDelay = 1.5f;

    [Header("One-time")]
    public bool spawnOnlyOnce = true;

    private bool _spawned;

    // Call this from your vitrine "success" moment
    public void SpawnPortalAfterWin()
    {
        if (spawnOnlyOnce && _spawned) return;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (spawnOnlyOnce && _spawned) yield break;

        yield return new WaitForSeconds(spawnDelay);

        if (portalPrefab == null || portalSpawnPoint == null)
        {
            Debug.LogWarning("[P3ExitPortalSpawner] Missing portalPrefab or portalSpawnPoint.", this);
            yield break;
        }

        Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
        _spawned = true;
    }
}
