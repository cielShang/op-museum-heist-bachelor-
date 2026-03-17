using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GuardDronePatrol : MonoBehaviour
{
    public Transform[] waypoints;
    public float waitTimeAtPoint = 1f;

    [Header("Animation & Audio")]
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip takedownClip;

    [Header("Sight Component")]
    public GuardSightAlert sightAlert;

    [Header("Alert Chase & Attack")]
    public float alertChaseSpeed = 4f;
    public float attackRange = 1.6f;
    public float attackInterval = 1.0f;
    public int damageToPlayer = 1;
    public string attackAnimationName = "AttackDrone";
    public AudioClip attackClip;

    [Header("Combat From Player")]
    public int health = 3;
    public KeyCode playerAttackKey = KeyCode.Q;
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.12f;
    public AudioClip hitClip;

    [Header("Stun (Sakura assist)")]
    public AudioClip stunClip;
    [Tooltip("Prevents repeated ApplyStun calls from re-triggering audio/logic every frame.")]
    public float stunRetriggerCooldown = 0.35f;

    private NavMeshAgent _agent;
    private int _index;
    private float _waitTimer;
    private float _baseSpeed;

    public bool isActive = true;

    // alert mode
    private bool _isInAlertMode = false;
    private Transform _alertTarget;
    private float _attackTimer;
    private bool _isDead = false;
    private bool _isKnockback = false;

    // stun
    private bool _isStunned = false;
    private float _stunTimer = 0f;
    private Vector3 _stunOrigin;
    private float _lastStunRequestTime = -999f;

    public bool IsStunned => _isStunned;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (_agent != null)
            _baseSpeed = _agent.speed;

        if (waypoints != null && waypoints.Length > 0 && _agent != null)
            _agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (!isActive || _agent == null || _isDead)
            return;

        // ---------------- STUN ----------------
        if (_isStunned)
        {
            _stunTimer -= Time.deltaTime;

            float shakeAmount = 0.05f;
            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                0f,
                Random.Range(-shakeAmount, shakeAmount)
            );
            transform.position = _stunOrigin + randomOffset;

            if (_stunTimer <= 0f)
                EndStun();

            return; // stunned => no patrol/attack
        }

        // ---------------- ALERT MODE ----------------
        // if (_isInAlertMode && _alertTarget != null)
        // {
        //     if (!_isKnockback)
        //         _agent.SetDestination(_alertTarget.position);

        //     float dist = Vector3.Distance(transform.position, _alertTarget.position);

        //     if (dist <= attackRange)
        //     {
        //         _attackTimer -= Time.deltaTime;
        //         if (_attackTimer <= 0f)
        //         {
        //             _attackTimer = attackInterval;
        //             DoAttack();
        //         }

        //         if (Input.GetKeyDown(playerAttackKey))
        //             OnHitByPlayer();
        //     }

        //     return;
        // }

        // ---------------- PATROL ----------------
        if (waypoints == null || waypoints.Length == 0)
            return;

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= waitTimeAtPoint)
            {
                _index = (_index + 1) % waypoints.Length;
                _agent.SetDestination(waypoints[_index].position);
                _waitTimer = 0f;
            }
        }
    }

    private void DoAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(attackAnimationName))
            animator.CrossFade(attackAnimationName, 0.1f);

        if (audioSource != null && attackClip != null)
            audioSource.PlayOneShot(attackClip);

        if (_alertTarget != null)
        {
            PlayerHealth ph = _alertTarget.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damageToPlayer);
        }
    }

    private void OnHitByPlayer()
    {
        if (_isDead) return;

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        if (_alertTarget != null)
        {
            Vector3 dir = (transform.position - _alertTarget.position).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackCoroutine(dir));
        }

        health -= 1;
        Debug.Log($"[GuardDronePatrol] Hit by player. Remaining health = {health}");

        if (health <= 0)
            DisableGuard();
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction)
    {
        _isKnockback = true;

        if (_agent != null)
            _agent.isStopped = true;

        Vector3 start = transform.position;
        Vector3 end = start + direction.normalized * knockbackDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / knockbackDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (!_isDead && _agent != null && !_isStunned)
            _agent.isStopped = false;

        _isKnockback = false;
    }

    //this no longer “restarts” stun forever
    public void ApplyStun(float duration)
    {
        if (_isDead || !isActive) return;
        if (duration <= 0f) return;

        // prevent spam calls (like every frame) from re-triggering
        if (Time.time - _lastStunRequestTime < stunRetriggerCooldown)
        {
            // but still allow extending the timer if needed
            if (_isStunned)
                _stunTimer = Mathf.Max(_stunTimer, duration);
            return;
        }
        _lastStunRequestTime = Time.time;

        if (_isStunned)
        {
            // already stunned -> only extend
            _stunTimer = Mathf.Max(_stunTimer, duration);
            return;
        }

        // start stun
        _isStunned = true;
        _stunTimer = duration;
        _stunOrigin = transform.position;

        if (_agent != null)
            _agent.isStopped = true;

        if (sightAlert != null)
            sightAlert.SetDetectEnabled(false);

        // play single zap once
        if (audioSource != null)
        {
            audioSource.loop = false;
            if (stunClip != null)
                audioSource.PlayOneShot(stunClip);
        }

        Debug.Log("[GuardDronePatrol] Stunned for " + duration + "s.");
    }

    private void EndStun()
    {
        _isStunned = false;
        transform.position = _stunOrigin;

        if (_agent != null)
            _agent.isStopped = false;

        if (sightAlert != null)
            sightAlert.SetDetectEnabled(true);

        Debug.Log("[GuardDronePatrol] Stun ended.");
    }

    // public void EnterAlertMode(Transform target)
    // {
    //     _isInAlertMode = true;
    //     _alertTarget = target;

    //     if (_agent != null)
    //     {
    //         _agent.speed = alertChaseSpeed;
    //         _agent.SetDestination(_alertTarget.position);
    //     }

    //     Debug.Log("[GuardDronePatrol] Entered alert mode, chasing player.");
    // }

    // public void ExitAlertMode()
    // {
    //     _isInAlertMode = false;
    //     _alertTarget = null;

    //     if (!isActive || _isDead) return;

    //     if (_agent != null)
    //     {
    //         _agent.speed = _baseSpeed;
    //         if (waypoints != null && waypoints.Length > 0)
    //             _agent.SetDestination(waypoints[_index].position);
    //     }

    //     Debug.Log("[GuardDronePatrol] Exited alert mode, resuming patrol.");
    // }

    public void EnterAlertMode(Transform target)
{
    // Guard does NOT chase/attack in your design.
    // Keep this method so DroneWaveManager can call it without errors.
    _isInAlertMode = false;
    _alertTarget = null;

    // optional: if you want him to "react" but still patrol, do nothing here.
    // If you want him to immediately continue patrol route:
    if (_agent != null && !_isDead && isActive && waypoints != null && waypoints.Length > 0)
        _agent.SetDestination(waypoints[_index].position);

    Debug.Log("[GuardDronePatrol] Alert ignored (guard remains patrol-only).");
}

public void ExitAlertMode()
{
    // Kept for compatibility; nothing to exit.
    _isInAlertMode = false;
    _alertTarget = null;
}


    public void DisableGuard()
    {
        if (_isDead) return;
        _isDead = true;
        isActive = false;

        // stop moving
        if (_agent != null)
            _agent.enabled = false;

        // play defeat sound safely
        if (takedownClip != null)
            AudioSource.PlayClipAtPoint(takedownClip, transform.position);

        // fall animation
        if (animator != null)
        {
            animator.CrossFade("FallDown", 0.1f);
            animator.SetBool("Loop", false);
        }

        // disable collider
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2.0f);
    }
    public void ForceRemoveNow()
{
    // stop any stun/vibrate logic
    _isStunned = false;
    _stunTimer = 0f;

    // stop navmesh movement
    if (_agent != null)
    {
        _agent.isStopped = true;
        _agent.enabled = false;
    }

    // stop audio (important if stun sound is looping on the AudioSource)
    if (audioSource != null)
    {
        audioSource.Stop();
    }

    // disable detection
    if (sightAlert != null)
        sightAlert.SetDetectEnabled(false);

    // disable collider
    var col = GetComponent<Collider>();
    if (col != null) col.enabled = false;

    // hide visuals (in case Destroy is delayed or something weird happens)
    foreach (var r in GetComponentsInChildren<Renderer>(true))
        r.enabled = false;

    // finally: delete the whole object
    Destroy(gameObject);
}

}
