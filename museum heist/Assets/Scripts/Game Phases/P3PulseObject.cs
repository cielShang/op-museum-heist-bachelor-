using UnityEngine;

public class P3PulseObject : MonoBehaviour
{
    [Header("Pulse")]
    public bool pulse = false;
    public float speed = 2.0f;
    public float scaleAmount = 0.08f;

    private Vector3 _baseScale;

    void Awake()
    {
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // reset when enabled
        _baseScale = transform.localScale;
    }

    void Update()
    {
        if (!pulse) return;

        float s = 1f + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = _baseScale * s;
    }

    public void SetPulse(bool on)
    {
        pulse = on;

        if (!on)
            transform.localScale = _baseScale;
    }
}
