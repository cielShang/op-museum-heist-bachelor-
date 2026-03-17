using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DemoCombatArea : MonoBehaviour
{
    public KeyCode attackKey = KeyCode.Q;
    public DemoEnemy[] enemies;

    bool _inside;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _inside = true;
        DemoManager.Instance.SetObjective("Press Q to knock enemies back.");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _inside = false;
    }

    void Update()
    {
        if (!_inside) return;

        if (Input.GetKeyDown(attackKey))
        {
            Vector3 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;

            foreach (var e in enemies)
                if (e != null)
                    e.TakeHit(playerPos);

            DemoManager.Instance.SetObjective("Good! Most enemies need more than 1 hit to fall.");
        }
    }
}
