using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class P3LaserGroupPatternController : MonoBehaviour
{
    public enum Pattern { Dense, GapsLeft, GapsRight, VerticalS, HorizontalS }

    [Header("Laser Objects (enable/disable or animate)")]
    public GameObject[] denseLasers;
    public GameObject[] gapsLeftLasers;
    public GameObject[] gapsRightLasers;

    [Tooltip("Objects that should be enabled when Vertical sweep is active (may include the animated object).")]
    public GameObject[] verticalSweep;

    [Tooltip("Objects that should be enabled when Horizontal sweep is active (may include the animated object).")]
    public GameObject[] horizontalSweep;

    [Header("Optional: Animators for sweep patterns")]
    public Animator verticalSweepAnimator;
    public string verticalAnimState = "vertical";
    public Animator horizontalSweepAnimator;
    public string horizontalAnimState = "horizontal";

    [Header("Timing")]
    public float downSecondsDefault = 10f;

    [Header("Runtime")]
    public Pattern currentPattern = Pattern.Dense;
    public bool permanentlyCleared = false;

    private readonly List<Pattern> _available = new List<Pattern>();
    private int _idx = 0;
    private Coroutine _downRoutine;

    void Start()
    {
        RebuildAvailablePatterns();
        ApplyCurrent();
    }

    /// <summary>
    /// SAFE ROUTE: no correctness logic. Always true.
    /// Keeps older scripts compiling.
    /// </summary>
    public bool IsCurrentlyCorrect() => true;

    /// <summary>
    /// Build list of patterns that are actually configured in the Inspector.
    /// Only those will be cycled.
    /// </summary>
    public void RebuildAvailablePatterns()
    {
        _available.Clear();

        if (HasAny(denseLasers)) _available.Add(Pattern.Dense);
        if (HasAny(gapsLeftLasers)) _available.Add(Pattern.GapsLeft);
        if (HasAny(gapsRightLasers)) _available.Add(Pattern.GapsRight);
        if (HasAny(verticalSweep)) _available.Add(Pattern.VerticalS);
        if (HasAny(horizontalSweep)) _available.Add(Pattern.HorizontalS);

        // fallback so it never becomes empty (prevents "no lasers at all")
        if (_available.Count == 0)
            _available.Add(Pattern.Dense);

        // ensure currentPattern is valid
        _idx = Mathf.Max(0, _available.IndexOf(currentPattern));
        if (_idx < 0) _idx = 0;
        currentPattern = _available[_idx];
    }

    /// <summary>
    /// Called by the bypass switch. Cycles ONLY through available patterns.
    /// No auto-drop here.
    /// </summary>
    public void CyclePatternNoAutoDrop()
    {
        if (permanentlyCleared) return;

        if (_available.Count == 0)
            RebuildAvailablePatterns();

        _idx = (_idx + 1) % _available.Count;
        currentPattern = _available[_idx];
        ApplyCurrent();
    }

    /// <summary>
    /// Checkpoints use this: lasers drop temporarily, then come back in the CURRENT pattern.
    /// </summary>
    public void DropForSeconds(float seconds)
    {
        if (permanentlyCleared) return;

        if (_downRoutine != null) StopCoroutine(_downRoutine);
        _downRoutine = StartCoroutine(DropRoutine(seconds));
    }

    IEnumerator DropRoutine(float seconds)
    {
        SetAllLasers(false);
        yield return new WaitForSeconds(seconds);

        if (!permanentlyCleared)
            ApplyCurrent();
    }

    public void MarkCleared()
    {
        permanentlyCleared = true;
        SetAllLasers(false);
    }

    private void ApplyCurrent()
    {
        SetAllLasers(false);

        switch (currentPattern)
        {
            case Pattern.Dense:
                SetArray(denseLasers, true);
                break;

            case Pattern.GapsLeft:
                SetArray(gapsLeftLasers, true);
                break;

            case Pattern.GapsRight:
                SetArray(gapsRightLasers, true);
                break;

            case Pattern.VerticalS:
                SetArray(verticalSweep, true);
                PlaySweep(verticalSweepAnimator, verticalAnimState);
                break;

            case Pattern.HorizontalS:
                SetArray(horizontalSweep, true);
                PlaySweep(horizontalSweepAnimator, horizontalAnimState);
                break;
        }
    }

    private void PlaySweep(Animator anim, string state)
    {
        if (anim == null) return;
        if (string.IsNullOrEmpty(state)) return;

        // restart animation so it plays every time you switch to it
        anim.Play(state, 0, 0f);
        anim.Update(0f);
    }

    private void SetAllLasers(bool on)
    {
        SetArray(denseLasers, on);
        SetArray(gapsLeftLasers, on);
        SetArray(gapsRightLasers, on);
        SetArray(verticalSweep, on);
        SetArray(horizontalSweep, on);
    }

    private void SetArray(GameObject[] arr, bool on)
    {
        if (arr == null) return;
        foreach (var go in arr)
            if (go != null) go.SetActive(on);
    }

    private bool HasAny(GameObject[] arr)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) return true;
        return false;
    }
    public void SetSectionEnabled(bool enabled)
{
    // Enables/disables the whole laser section GameObject
    // (and therefore all patterns under it)
    gameObject.SetActive(enabled);
}

}
