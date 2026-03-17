using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class GuardSightAlert : MonoBehaviour
{
    [Header("References")]
    public DroneWaveManager waveManager;   // assign the *local* manager (corridor or jump side)
    public Transform player;               // will auto-find if left empty
    public ConvaiNPC sakuraNPC;           // optional – Sakura reacts

    [Header("Vision")]
    public float visionRange = 12f;
    [Range(0f, 180f)]
    public float visionAngle = 70f;
    public LayerMask visionMask = ~0;

    [Header("Integration with stun")]
    [Tooltip("If Sakura stun sets this to false, the guard temporarily cannot detect the player.")]
    public bool canDetect = true;

    [Header("Debug")]
    public bool debugLogs = true;

    bool _alertTriggered = false;

    void Awake()
    {
        if (sakuraNPC == null && NPCBootstrapper.ActiveNPC != null)
    sakuraNPC = NPCBootstrapper.ActiveNPC;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (_alertTriggered) return;
        if (!canDetect) return;
        if (waveManager == null || player == null) return;

if (CanSeePlayer())
{
    _alertTriggered = true;

    if (debugLogs) Debug.Log("[GuardSightAlert] Player spotted – triggering local alert wave.");

    waveManager.TriggerAlertWave();

    // if (sakuraNPC != null)
    // {
    //     sakuraNPC.InterruptCharacterSpeech();
    //     sakuraNPC.TriggerSpeech(
    //         "[WHISPER][PANIC] Shoot, we’ve been spotted! Backup drones are coming—get ready!"
    //     );
    // }
}

    }

    bool CanSeePlayer()
    {
        Vector3 origin   = transform.position + Vector3.up * 1.0f;
        Vector3 toPlayer = player.position - origin;

        float dist = toPlayer.magnitude;
        if (dist > visionRange) return false;

        Vector3 flatDir = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float angle = Vector3.Angle(transform.forward, flatDir.normalized);
        if (angle > visionAngle * 0.5f) return false;

        if (Physics.Raycast(origin, flatDir.normalized, out RaycastHit hit, visionRange, visionMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (debugLogs) Debug.Log("[GuardSightAlert] LoS to player confirmed.");
                return true;
            }
        }

#if UNITY_EDITOR
        Debug.DrawRay(origin, flatDir.normalized * visionRange, Color.red, 0.1f);
#endif

        return false;
    }

   
    public void SetDetectEnabled(bool enabled)
    {
        canDetect = enabled;
        if (!enabled && debugLogs)
            Debug.Log("[GuardSightAlert] Detection disabled (stunned).");
    }
}
