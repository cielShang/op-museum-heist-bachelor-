using System.Collections;
using UnityEngine;

public class P3MillenniumPuzzleController : MonoBehaviour
{
    [Header("Wiring")]
    public Collider lockTargetCollider;
    public P3LaserEmitterStepped[] emitters;

    [Header("Unlock")]
    public Animator unlockAnimator;
    public string unlockStateName = "Unlock";
    public AudioSource audioSource;
    public AudioClip unlockClip;

    [Header("Cleanup")]
    [Tooltip("Millennium mesh root, laser barrier, buttons, etc.")]
    public GameObject[] objectsToDisableOnUnlock;

    [Tooltip("Wait so animation/sound can play before objects vanish.")]
    public float disableDelaySeconds = 2f;

    [Header("Debug")]
    public bool debugLogs = true;

    private bool _unlocked;

    public void EvaluateSolved()
    {
        if (_unlocked) return;

        if (lockTargetCollider == null || emitters == null || emitters.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[MillenniumPuzzle] Missing wiring (lockTarget or emitters).", this);
            return;
        }

        for (int i = 0; i < emitters.Length; i++)
        {
            if (emitters[i] == null)
            {
                if (debugLogs) Debug.LogWarning("[MillenniumPuzzle] Null emitter slot in array.", this);
                return;
            }

            if (!emitters[i].IsAligned)
                return; // not solved yet
        }

        Unlock();
    }

    private void Unlock()
    {
        if (_unlocked) return;
        _unlocked = true;

        if (debugLogs) Debug.Log("[MillenniumPuzzle] UNLOCKED ✅", this);

        // Lock all emitters so nothing can be moved anymore
        if (emitters != null)
        {
            foreach (var e in emitters)
            {
                if (e == null) continue;
                e.SetLocked(true);

                // Turn off beam visuals right away (optional)
                e.DisableLaserVisual();
            }
        }

        // audio
        if (audioSource != null && unlockClip != null)
            audioSource.PlayOneShot(unlockClip);

        // animation
        if (unlockAnimator != null && !string.IsNullOrEmpty(unlockStateName))
            unlockAnimator.Play(unlockStateName, 0, 0f);

        // Delay cleanup so animation/sound are visible
        StartCoroutine(DisableObjectsAfterDelay());
    }

    private IEnumerator DisableObjectsAfterDelay()
    {
        yield return new WaitForSeconds(disableDelaySeconds);

        if (objectsToDisableOnUnlock != null)
        {
            foreach (var go in objectsToDisableOnUnlock)
                if (go != null) go.SetActive(false);
        }
    }
}
