using UnityEngine;
using UnityEngine.AI;
using Convai.Scripts.Runtime.Core;   

public class DroneNoiseResponder : MonoBehaviour
{
    public enum DroneState { Patrol, Investigate, Return }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform[] patrolPoints;
    public Transform player;
    public ConvaiNPC sakuraNPC;      
    public DroneWaveManager waveManager;

    [Header("Investigate Settings")]
    public float investigateDuration = 5f;   // how long to search once at the spot
    public float visionRange = 12f;
    [Range(0f, 180f)]
    public float visionAngle = 70f;
    public LayerMask visionMask = ~0;       // what layers raycast can hit 

    [Header("Debug")]
    public bool debugLogs = true;

    private DroneState _state = DroneState.Patrol;
    private int _patrolIndex = 0;

    private Vector3 _investigateTarget;
    private Vector3 _returnPosition;
    private float _investigateTimer;
    private bool _hasReachedInvestigatePoint = false;
    private bool _playerSeenDuringSearch = false;

    
    private bool _hasAnnouncedAlert = false;

    void Awake()
    {
        if (sakuraNPC == null && NPCBootstrapper.ActiveNPC != null)
    sakuraNPC = NPCBootstrapper.ActiveNPC;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Start()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && agent != null)
        {
            _state = DroneState.Patrol;
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        switch (_state)
        {
            case DroneState.Patrol:
                UpdatePatrol();
                break;

            case DroneState.Investigate:
                UpdateInvestigate();
                break;

            case DroneState.Return:
                UpdateReturn();
                break;
        }
    }

    // Called by JumpLandingDetector when landing is noisy
    public void InvestigateNoise(Vector3 position)
    {
        _investigateTarget = position;
        _returnPosition = transform.position;

        _hasReachedInvestigatePoint = false;
        _playerSeenDuringSearch = false;
        _investigateTimer = 0f;   // will be set when arriving
        _hasAnnouncedAlert = false;   // reset for this investigation

        _state = DroneState.Investigate;

        if (debugLogs) Debug.Log("[DroneNoiseResponder] Investigating noise at " + position);
        if (agent != null)
            agent.SetDestination(_investigateTarget);
    }

    private void UpdatePatrol()
    {
        if (agent == null || patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void UpdateInvestigate()
    {
        if (agent == null) return;

        // 1) move to investigate point first
        if (!_hasReachedInvestigatePoint)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                _hasReachedInvestigatePoint = true;
                _investigateTimer = investigateDuration;

                if (debugLogs) Debug.Log("[DroneNoiseResponder] Reached noise location, starting search.");
            }
            return;
        }

        // 2) once there, search for full duration
        _investigateTimer -= Time.deltaTime;

        // rotate to simulate scanning
        transform.Rotate(0f, 60f * Time.deltaTime, 0f);

        // --- LOS check – sets flag AND can trigger Sakura alert line ---
        if (CanSeePlayer())
        {
            if (!_playerSeenDuringSearch && debugLogs)
                Debug.Log("[DroneNoiseResponder] Player seen during search (will matter at the end).");

            _playerSeenDuringSearch = true;

            
            // if (!_hasAnnouncedAlert && sakuraNPC != null)
            // {
            //     _hasAnnouncedAlert = true;

            //     Speak(
            //         "[WHISPER][URGENT] The guard has eyes on us. He’ll call in drones—" +
            //         "I’ll help by paralyzing some with my taser. Get ready for a fight."
            //     );
            // }
        }

        if (debugLogs) Debug.Log("[DroneNoiseResponder] timer = " + _investigateTimer);

        // 3) when timer finished, decide what to do
        if (_investigateTimer <= 0f)
        {
            if (debugLogs)
            {
                if (_playerSeenDuringSearch)
                    Debug.Log("[DroneNoiseResponder] Search done – player WAS seen at some point.");
                else
                    Debug.Log("[DroneNoiseResponder] Search done – no player detected.");
            }

            HandleSearchEnd();
        }
    }

private void HandleSearchEnd()
{
    if (_playerSeenDuringSearch)
    {
        if (debugLogs)
            Debug.Log("[DroneNoiseResponder] Player was seen – triggering alert wave.");

        if (waveManager != null)
            waveManager.TriggerAlertWave();

        FireND("P1_INVESTIGATION_SPOTTED"); //  ND trigger
    }
    else
    {
        if (debugLogs)
            Debug.Log("[DroneNoiseResponder] Player not seen – safe this time.");

        FireND("P1_INVESTIGATION_CLEAR"); // ND trigger
    }

    _state = DroneState.Return;
    if (agent != null)
        agent.SetDestination(_returnPosition);
}


    private void FireND(string trigger)
{
    var npc = NPCBootstrapper.ActiveNPC;
    if (npc == null) return;

    npc.InterruptCharacterSpeech();
    npc.TriggerEvent(trigger);
}

    private void Speak(string msg)
    {
        if (sakuraNPC == null) return;
        sakuraNPC.InterruptCharacterSpeech();
        sakuraNPC.TriggerSpeech(msg);
    }

    private void UpdateReturn()
    {
        if (agent == null) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (debugLogs) Debug.Log("[DroneNoiseResponder] Returned to original position, resuming patrol.");

            _state = DroneState.Patrol;
            if (patrolPoints != null && patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.0f;
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
                if (debugLogs) Debug.Log("[DroneNoiseResponder] Player spotted!");
                return true;
            }
        }

        #if UNITY_EDITOR
        Debug.DrawRay(origin, flatDir.normalized * visionRange, Color.red, 0.1f);
        #endif

        return false;
    }
}
