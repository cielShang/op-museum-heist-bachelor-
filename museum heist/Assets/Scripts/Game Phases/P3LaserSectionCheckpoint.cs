using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P3LaserSectionCheckpoint : MonoBehaviour
{
    public P3LaserGroupPatternController laserGroup;
    public float downSeconds = 10f;

    private bool _done;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_done) return;
        if (!other.CompareTag("Player")) return;
        if (laserGroup == null) return;

        _done = true;

        // Temporarily disable lasers, then restore
        laserGroup.DropForSeconds(downSeconds);
    }
}
