using UnityEngine;
using Convai.Scripts.Runtime.Core;

public class LaserGateController : MonoBehaviour
{
    [Header("Visuals (any number of laser beams, particle effects, etc.)")]
    public GameObject[] laserVisuals;     // multiple visuals

    [Header("Physical Colliders (barriers that block player/NPC)")]
    public Collider[] solidBarriers;      // multiple barriers

    [Header("Convai Linking (optional)")]
    public ConvaiNPC convaiNPC;

    [Header("State")]
    public bool lasersOn = true;

    [Header("Hack Settings")]
    public float hackDisableSeconds = 60f;

    private float _hackTimer = 0f;

    void Start()
    {
        Apply();
    }

    void Update()
    {
        if (_hackTimer > 0f)
        {
            _hackTimer -= Time.deltaTime;
            if (_hackTimer <= 0f)
                SetLasers(true, reason: "Hack expired");
        }
    }

    public void SetLasers(bool on, string reason = "")
    {
        lasersOn = on;
        Apply();

        if (convaiNPC != null)
        {
            string msg = on
                ? $"[SECURITY] Lasers re-enabled. ({reason})"
                : $"[SECURITY] Lasers disabled. ({reason})";

            convaiNPC.TriggerSpeech(msg);
        }
    }

    public void DisableForSeconds(float seconds, string reason = "Terminal hack")
    {
        _hackTimer = seconds;
        SetLasers(false, reason);
    }

    private void Apply()
    {
        // Toggle all visuals
        if (laserVisuals != null)
        {
            foreach (var vis in laserVisuals)
            {
                if (vis != null)
                    vis.SetActive(lasersOn);
            }
        }

        // Toggle all physical colliders
        if (solidBarriers != null)
        {
            foreach (var col in solidBarriers)
            {
                if (col != null)
                    col.enabled = lasersOn;
            }
        }
    }
}
