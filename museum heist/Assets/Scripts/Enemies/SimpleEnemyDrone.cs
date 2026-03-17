using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class SimpleEnemyDrone : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float attackRange = 1.5f;

    [Header("Combat")]
    public int health = 1;
    public int damageToPlayer = 1;
    public float attackInterval = 1.0f;
    public KeyCode playerAttackKey = KeyCode.Q;
    public float knockbackDistance = 1.5f;
    public float knockbackDuration = 0.15f;
    public bool IsStunned => _isStunned;

    [Header("Animation")]
    public Animator animator;
    public string attackAnimationName = "Attack";
    public string fallAnimationName = "FallDown";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip deathClip;
    public AudioClip stunClip;

    [Header("Stun")]
    [Tooltip("Prevents repeated ApplyStun calls from replaying sound / resetting constantly.")]
    public float stunRetriggerCooldown = 0.35f;
    [Tooltip("If a second stun comes in while stunned, extend to the longer duration instead of restarting.")]
    public bool extendStunIfAlreadyStunned = true;

    [Header("Debug")]
    public bool debugLogs = true;

    [HideInInspector] public DroneWaveManager owner;

    private Transform _target;
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private float _attackTimer;
    private bool _isDead = false;
    private bool _isKnockback = false;

    // stun
    private bool _isStunned = false;
    private float _stunTimer = 0f;
    private Vector3 _stunOrigin;
    private float _lastStunRequestTime = -999f;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        if (_agent != null)
        {
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = attackRange;
        }

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;
    }

    void Start()
    {
        if (_target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                _target = p.transform;
                if (debugLogs) Debug.Log("[SimpleEnemyDrone] Auto-assigned target by tag 'Player'.");
            }
            else if (debugLogs)
            {
                Debug.LogWarning("[SimpleEnemyDrone] No target set and no GameObject with tag 'Player' found.");
            }
        }

        if (_agent == null && debugLogs)
            Debug.LogError("[SimpleEnemyDrone] No NavMeshAgent found.");

        if (_agent != null && !_agent.isOnNavMesh && debugLogs)
            Debug.LogWarning("[SimpleEnemyDrone] Agent is NOT on NavMesh at start – it will not move.");
    }

    public void SetTarget(Transform t)
    {
        _target = t;
        if (debugLogs && _target != null)
            Debug.Log("[SimpleEnemyDrone] Target set by spawner: " + _target.name);
    }

    void Update()
    {
        if (_isDead) return;

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
            {
                _isStunned = false;
                transform.position = _stunOrigin;

                if (_agent != null)
                    _agent.isStopped = false;

                if (debugLogs)
                    Debug.Log("[SimpleEnemyDrone] Stun ended.");
            }

            // still killable while stunned -> do NOT return before checking player hit?
            // We DO return (as before) because your hit check happens later only if dist<=attackRange.
            // But player can still hit while stunned if they are in range because Update continues next frame.
            return;
        }

        if (_isKnockback) return;

        if (_agent == null || !_agent.enabled)
        {
            if (debugLogs) Debug.LogWarning("[SimpleEnemyDrone] NavMeshAgent missing or disabled.");
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            if (debugLogs) Debug.LogWarning("[SimpleEnemyDrone] Agent is not on NavMesh -> cannot move.");
            return;
        }

        if (_target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                _target = p.transform;
                if (debugLogs) Debug.Log("[SimpleEnemyDrone] Late-assigned target by tag 'Player'.");
            }
            else
            {
                return;
            }
        }

        // Chase
        _agent.SetDestination(_target.position);

        float dist = Vector3.Distance(transform.position, _target.position);

        // Attack
        if (dist <= attackRange)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                _attackTimer = attackInterval;
                DoAttack();
            }
        }

        
    }

    private void DoAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(attackAnimationName))
            animator.CrossFade(attackAnimationName, 0.1f);
        else if (animator == null && debugLogs)
            Debug.LogWarning("[SimpleEnemyDrone] No Animator assigned for attack.");

        if (audioSource != null && attackClip != null)
            audioSource.PlayOneShot(attackClip);

        if (_target != null)
        {
            PlayerHealth ph = _target.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damageToPlayer);
        }

        if (debugLogs) Debug.Log("[SimpleEnemyDrone] Attacked player.");
    }

    private void OnHitByPlayer()
    {
        if (_isDead) return;

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        if (_target != null)
        {
            Vector3 dir = (transform.position - _target.position).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackCoroutine(dir));
        }

        health -= 1;
        if (debugLogs) Debug.Log($"[SimpleEnemyDrone] Hit by player. Remaining health = {health}");

        if (health <= 0)
            Die();
    }
public void ForceDie()
{
    if (_isDead) return;
    health = 0;
    Die();
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

    
public void ApplyStun(float duration)
{
    if (_isDead) return;

    // If already stunned, just extend timer (no sound replay, no reset)
    if (_isStunned)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
        return;
    }

    _isStunned = true;
    _stunTimer = duration;
    _stunOrigin = transform.position;

    if (_agent != null)
        _agent.isStopped = true;

    if (audioSource != null && stunClip != null)
        audioSource.PlayOneShot(stunClip);

    if (debugLogs)
        Debug.Log("[SimpleEnemyDrone] Stunned for " + duration + "s.");
}


    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (debugLogs) Debug.Log("[SimpleEnemyDrone] Drone defeated.");

        if (owner != null)
            owner.NotifyDroneKilled(this);

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        if (animator != null && !string.IsNullOrEmpty(fallAnimationName))
            animator.CrossFade(fallAnimationName, 0.1f);

        // reliable death sound even if object gets destroyed
        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2.0f);
    }
    public void ForceDieOrKnockback()
{
    if (_isDead) return;

    health--;

    if (audioSource != null && hitClip != null)
        audioSource.PlayOneShot(hitClip);

    if (health > 0)
    {
        // knockback only
        if (_target != null)
        {
            Vector3 dir = (transform.position - _target.position).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackCoroutine(dir));
        }
    }
    else
    {
        Die();
    }
}

}
