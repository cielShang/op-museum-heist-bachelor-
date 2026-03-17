using UnityEngine;

public class DemoEnemy : MonoBehaviour
{
    [Header("Combat")]
    public int hitsToDie = 2;
    public float knockbackForce = 4f;

    [Header("Animation")]
    public Animator animator;
    public string hitAnim = "Hit";
    public string deathAnim = "Die";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitClip;
    public AudioClip deathClip;

    int _hitsTaken;
    bool _dead;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void TakeHit(Vector3 fromDirection)
    {
        if (_dead) return;

        _hitsTaken++;

        if (_hitsTaken < hitsToDie)
        {
            // HIT 1 knockback
            if (animator != null)
                animator.Play(hitAnim);

            if (audioSource != null && hitClip != null)
                audioSource.PlayOneShot(hitClip);

            if (_rb != null)
            {
                Vector3 dir = (transform.position - fromDirection).normalized;
                _rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        _dead = true;

        if (animator != null)
            animator.Play(deathAnim);

        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);

        Destroy(gameObject, 1.5f);
    }
}
