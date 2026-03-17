using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaserDamageOnTouch : MonoBehaviour
{
    public int damage = 1;
    public float cooldown = 0.8f;

    [Header("Knockback")]
    public float knockbackForce = 3f;
    public float verticalLift = 0.5f;

    float _nextHit;

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < _nextHit) return;

        _nextHit = Time.time + cooldown;

        // Damage
        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);

        // Knockback (CharacterController-safe)
        var controller = other.GetComponent<CharacterController>();
        if (controller != null)
        {
            Vector3 dir = (other.transform.position - transform.position).normalized;
            dir.y = verticalLift;

            // small instant push
            controller.Move(dir * knockbackForce * Time.deltaTime);
        }
    }
}
