using UnityEngine;
using UnityEngine.AI;

public class Phase3TopDownEnemy : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float waitTimeAtPoint = 0.5f;
    public float arriveDistance = 0.35f; //  not 0. EVER!!

    [Header("Vision (matches cone)")]
    public float viewDistance = 6f;
    [Range(1f, 180f)] public float viewAngleFull = 60f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public Transform eyePoint;
    public float eyeHeight = 1.0f;

    [Header("Damage")]
    public int damage = 1;
    public float damageCooldown = 1.0f;

    [Header("Takedown (player presses P nearby)")]
    public KeyCode takedownKey = KeyCode.Q;
    public float takedownRange = 1.6f;
    public AudioSource audioSource;
    public AudioClip takedownClip;
    public Animator animator;
    public string takedownAnimState = "FallDown";
    public float destroyDelay = 1.5f;

    [Header("NavMesh Safety")]
    public bool lockToNavMesh = true;
    public float warpSearchRadius = 3f;

    [Header("Runtime")]
    public bool isActive = false;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugGizmos = true;

    private NavMeshAgent _agent;
    private int _index;
    private float _waitTimer;
    private float _nextDamageTime;
    private bool _dead;

    // anti-stuck / anti-override
    private Vector3 _lastPos;
    private float _stuckTimer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (_agent == null) return;

        // Force agent to actually move the transform
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        // !! so "arrival" works (GuardDrone style)
        if (_agent.stoppingDistance < arriveDistance)
            _agent.stoppingDistance = arriveDistance;

        _agent.isStopped = !isActive;

        // If active at start, begin patrol
        if (isActive)
            ForceStartPatrol();
    }

    void Update()
    {
        if (_dead) return;
        if (!isActive || _agent == null || !_agent.enabled) return;

        // keep it on navmesh if needed
        if (lockToNavMesh)
            EnsureOnNavMesh();

        // If something is overriding movement, hard-correct
        AntiOverrideTick();

        TryTakedown();
        PatrolTick();
        DetectAndDamage();
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (_agent == null) return;

        _agent.enabled = true;

        // Force correct settings each time (fixes cases where Unity flips them)
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        if (_agent.stoppingDistance < arriveDistance)
            _agent.stoppingDistance = arriveDistance;

        if (lockToNavMesh)
            EnsureOnNavMesh();

        _agent.isStopped = !active;

        if (active)
            ForceStartPatrol();

        if (debugLogs)
            Debug.Log($"[P3Enemy] {name} SetActive({active}) | enabled={_agent.enabled} isOnNavMesh={_agent.isOnNavMesh} updatePos={_agent.updatePosition}");
    }

    void ForceStartPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // reset to first point
        _index = Mathf.Clamp(_index, 0, waypoints.Length - 1);
        SetWaypointDestination(_index);

        _waitTimer = 0f;
        _lastPos = transform.position;
        _stuckTimer = 0f;
    }

    void SetWaypointDestination(int i)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (waypoints[i] == null) return;

        Vector3 dest = waypoints[i].position;

        // snap destination onto navmesh (prevents unreachable tiny offsets)
        if (_agent.isOnNavMesh && NavMesh.SamplePosition(dest, out var hit, 1.0f, _agent.areaMask))
            dest = hit.position;

        bool ok = _agent.SetDestination(dest);

        if (debugLogs)
            Debug.Log($"[P3Enemy] {name} SetDestination ok={ok} dest={dest} isOnNavMesh={_agent.isOnNavMesh} vel={_agent.velocity.magnitude:F2}");
    }

    void PatrolTick()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (_agent.pathPending) return;

        // GuardDrone-style arrival
        if (_agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, arriveDistance))
        {
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= waitTimeAtPoint)
            {
                _index = (_index + 1) % waypoints.Length;
                _waitTimer = 0f;
                SetWaypointDestination(_index);
            }
        }
    }

    void DetectAndDamage()
    {
        if (playerMask.value == 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance, playerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        Transform bestPlayer = null;
        float bestDist = float.MaxValue;
        float halfAngle = viewAngleFull * 0.5f;

        foreach (var h in hits)
        {
            if (h == null) continue;

            Transform player = h.transform;

            Vector3 toPlayer = (player.position - transform.position);
            toPlayer.y = 0f;

            float dist = toPlayer.magnitude;
            if (dist < 0.001f) continue;

            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            if (angle > halfAngle) continue;

            Vector3 eye = (eyePoint != null) ? eyePoint.position : (transform.position + Vector3.up * eyeHeight);
            Vector3 target = player.position + Vector3.up * eyeHeight;

            if (obstacleMask.value != 0)
            {
                if (Physics.Linecast(eye, target, obstacleMask, QueryTriggerInteraction.Ignore))
                    continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPlayer = player;
            }
        }

        if (bestPlayer == null) return;

        if (Time.time < _nextDamageTime) return;
        _nextDamageTime = Time.time + damageCooldown;

        PlayerHealth ph = bestPlayer.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);
    }

    void TryTakedown()
    {
        if (!Input.GetKeyDown(takedownKey)) return;
        if (playerMask.value == 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, takedownRange, playerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        Transform player = hits[0].transform;
        if (Vector3.Distance(transform.position, player.position) > takedownRange) return;

        Die();
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;
        isActive = false;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        if (audioSource != null && takedownClip != null)
            audioSource.PlayOneShot(takedownClip);

        if (animator != null && !string.IsNullOrEmpty(takedownAnimState))
            animator.CrossFade(takedownAnimState, 0.1f);

        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    void EnsureOnNavMesh()
    {
        if (_agent.isOnNavMesh) return;

        // try warp to nearest navmesh near current position
        if (NavMesh.SamplePosition(transform.position, out var hit, warpSearchRadius, _agent.areaMask))
        {
            bool warped = _agent.Warp(hit.position);
            if (debugLogs)
                Debug.Log($"[P3Enemy] {name} warped={warped} to NavMesh at {hit.position}");
        }
    }

    // If velocity says "moving" but transform isn't changing, something is overriding it
    void AntiOverrideTick()
    {
        float moved = (transform.position - _lastPos).sqrMagnitude;

        if (moved < 0.000001f && _agent.velocity.magnitude > 0.2f)
        {
            _stuckTimer += Time.deltaTime;

            // after a short moment, force transform to agent.nextPosition (hard fix)
            if (_stuckTimer > 0.2f)
            {
                transform.position = _agent.nextPosition;
                _stuckTimer = 0f;

                if (debugLogs)
                    Debug.Log($"[P3Enemy] {name} was overridden -> forced to agent.nextPosition { _agent.nextPosition }");
            }
        }
        else
        {
            _stuckTimer = 0f;
        }

        _lastPos = transform.position;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        float half = viewAngleFull * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0f, -half, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f, half, 0f) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, takedownRange);
    }
}
