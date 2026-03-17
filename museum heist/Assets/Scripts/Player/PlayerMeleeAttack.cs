using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    public KeyCode attackKey = KeyCode.Q;
    public float attackRange = 1.6f;
    public LayerMask enemyMask;

    [Header("Optional Feedback")]
    public AudioSource audioSource;
    public AudioClip attackWhoosh;

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            DoAttack();
        }
    }

    void DoAttack()
    {
        if (audioSource != null && attackWhoosh != null)
            audioSource.PlayOneShot(attackWhoosh);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0) return;

        // hit closest enemy
        float bestDist = float.MaxValue;
        SimpleEnemyDrone best = null;

        foreach (var h in hits)
        {
            var drone = h.GetComponentInParent<SimpleEnemyDrone>();
            if (drone == null) continue;

            float d = Vector3.Distance(transform.position, drone.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = drone;
            }
        }

        if (best != null)
            best.ForceDieOrKnockback(); // explained below
    }
}
