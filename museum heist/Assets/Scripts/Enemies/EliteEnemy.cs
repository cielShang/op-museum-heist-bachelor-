using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EliteEnemy : MonoBehaviour
{
    [HideInInspector] public EliteWaveManager waveManager;

    [Header("Health")]
    public int maxHealth = 5;
    private int currentHealth;

    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    [Header("Stats")]
    public int damageToPlayer = 2;
    public float moveSpeed = 5.5f;
    public float attackRange = 2.2f;
    public float attackInterval = 1.5f;

    [Header("Spacing / Behavior")]
    [Tooltip("Preferred minimum distance to the player. If closer, enemy tends to backstep.")]
    public float preferredMinDistance = 1.3f;
    public float backstepDistance = 2.0f;
    [Range(0f, 1f)]
    public float backstepChanceAfterAttack = 0.4f;

    [Tooltip("Who this elite should chase (optional – will be set by spawner).")]
    public Transform target;     // exposed for debugging / inspector

    [Header("Player Test Attack")]
    [Tooltip("Key the player uses to hit the elite (for now).")]
    public KeyCode playerAttackKey = KeyCode.Q;
    public float playerAttackRange = 2.5f;
    public int damageFromPlayer = 1;
    public float knockbackDistanceFromPlayer = 1.2f;
    public float knockbackDurationFromPlayer = 0.1f;

    [Header("Animations (state names in Animator)")]
    public Animator animator;
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string tauntState = "Taunt";
    public string deathState = "Death";
    public string backstepState = "Backstep";

    [Tooltip("Attack states that can be randomly picked each swing.")]
    public string[] attackStates = {
        "Attack1",
        "Attack_Jump",
        "Attack_Stomp",
        "Attack_TripleCombo",
        "Attack_Stabs"
    };

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip deathClip;
    public AudioClip tauntClip;

    [Header("Death Timing")]
    [Tooltip("Seconds after starting death animation before destroying the enemy.")]
    public float deathDestroyDelay = 1.3f;

    [Header("Debug")]
    public bool debugLogs = true;

    private Transform _player;          // internal reference used by AI
    private NavMeshAgent _agent;
    private Rigidbody _rb;

    private float _attackTimer;
    private bool _isDead;
    private bool _isBackstepping;
    private bool _isTaunting;
    private bool _isKnockbackFromPlayer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        currentHealth = maxHealth;

        if (_agent != null)
        {
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = attackRange * 0.8f;
        }

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // If a target was already assigned by the spawner, use that.
        if (target != null)
        {
            _player = target;
        }
        else
        {
            // Fallback: find player by tag
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Called by EliteWaveManager after spawning to tell this elite who to chase.
    /// </summary>
    public void SetTarget(Transform t)
    {
        target = t;
        _player = t;

        if (debugLogs && _player != null)
            Debug.Log("[EliteEnemy] Target set to: " + _player.name);
    }

    void Start()
    {
        if (_agent != null && !_agent.isOnNavMesh && debugLogs)
            Debug.LogWarning("[EliteEnemy] Agent is not on NavMesh.");

        // Taunt once when spawned
        PlaySpawnTaunt();
    }

    void Update()
    {
        if (_isDead || _isBackstepping || _isKnockbackFromPlayer) return;
        if (_agent == null || !_agent.enabled) return;
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // ==== BASIC CHASE ====
        if (dist > attackRange)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
            PlayMoveAnimation();
        }
        else
        {
            // ==== IN ATTACK ZONE ====
            _agent.isStopped = true;
            FacePlayer();
            HandleAttack(dist);
        }

        // ==== SIMPLE PLAYER TEST ATTACK (P KEY) ====
        if (!_isDead && Input.GetKeyDown(playerAttackKey) && dist <= playerAttackRange)
        {
            StartCoroutine(KnockbackFromPlayerCoroutine());
            TakeHit(damageFromPlayer);
        }
    }

    void PlayMoveAnimation()
    {
        if (animator == null) return;
        animator.CrossFade(walkState, 0.1f);
    }

    void FacePlayer()
    {
        if (_player == null) return;

        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    void HandleAttack(float currentDistance)
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f)
        {
            return;
        }

        _attackTimer = attackInterval;

        // choose random attack state
        if (attackStates != null && attackStates.Length > 0 && animator != null)
        {
            string state = attackStates[Random.Range(0, attackStates.Length)];
            animator.CrossFade(state, 0.1f);
        }

        // play attack sound
        if (audioSource != null && attackClip != null)
            audioSource.PlayOneShot(attackClip);

        // deal damage to player
        if (_player != null)
        {
            PlayerHealth ph = _player.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damageToPlayer);
        }

        if (debugLogs)
            Debug.Log("[EliteEnemy] Attacked player.");

        // decide if we should backstep to avoid face-hugging
        bool tooClose = currentDistance < preferredMinDistance;
        if (tooClose || Random.value < backstepChanceAfterAttack)
        {
            StartCoroutine(BackstepCoroutine());
        }
    }

    IEnumerator BackstepCoroutine()
    {
        if (_isDead || _isBackstepping) yield break;

        _isBackstepping = true;

        if (_agent != null)
            _agent.isStopped = true;

        if (animator != null && !string.IsNullOrEmpty(backstepState))
            animator.CrossFade(backstepState, 0.1f);

        Vector3 start = transform.position;
        Vector3 dir = -transform.forward;
        Vector3 end = start + dir.normalized * backstepDistance;

        float duration = 0.3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (!_isDead && _agent != null)
            _agent.isStopped = false;

        _isBackstepping = false;
    }

    IEnumerator KnockbackFromPlayerCoroutine()
    {
        if (_isDead || _isKnockbackFromPlayer) yield break;

        _isKnockbackFromPlayer = true;

        if (_agent != null)
            _agent.isStopped = true;

        Vector3 start = transform.position;
        Vector3 dir = (transform.position - _player.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = -transform.forward;
        Vector3 end = start + dir.normalized * knockbackDistanceFromPlayer;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / knockbackDurationFromPlayer;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (!_isDead && _agent != null)
            _agent.isStopped = false;

        _isKnockbackFromPlayer = false;
    }

    public void TakeHit(int damage = 1)
    {
        if (_isDead) return;

        currentHealth -= damage;

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        if (debugLogs)
            Debug.Log("[EliteEnemy] Hit, hp now: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (debugLogs)
            Debug.Log("[EliteEnemy] Drone defeated.");

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null && !string.IsNullOrEmpty(deathState))
        {
            animator.CrossFade(deathState, 0.1f);
        }

        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        if (waveManager != null)
        {
            waveManager.OnEliteDied(this);
        }

        Destroy(gameObject, deathDestroyDelay);
    }

    // --- SPAWN TAUNT ---

    public void PlaySpawnTaunt()
    {
        if (_isDead || _isTaunting) return;
        StartCoroutine(TauntCoroutine());
    }

    IEnumerator TauntCoroutine()
    {
        _isTaunting = true;

        if (_agent != null)
            _agent.isStopped = true;

        if (animator != null && !string.IsNullOrEmpty(tauntState))
            animator.CrossFade(tauntState, 0.1f);

        if (audioSource != null && tauntClip != null)
            audioSource.PlayOneShot(tauntClip);

        yield return new WaitForSeconds(1.2f);

        if (animator != null && !string.IsNullOrEmpty(idleState))
            animator.CrossFade(idleState, 0.1f);

        if (!_isDead && _agent != null)
            _agent.isStopped = false;

        _isTaunting = false;
    }
}
