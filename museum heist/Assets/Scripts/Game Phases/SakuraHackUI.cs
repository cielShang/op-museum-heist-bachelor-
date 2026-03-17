using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SakuraHackUI : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;                 
    public TextMeshProUGUI labelText;       

    [Header("Debug")]
    public bool debugLogs = true;

    private Coroutine _progressRoutine;

    void Awake()
    {
        // Just reset values, DON'T hide the object automatically
        ResetUI();
    }

    public void BeginProgress(float duration, HackFailDefensePhase phase)
    {
        if (_progressRoutine != null)
            StopCoroutine(_progressRoutine);

        // make sure this object is on
        gameObject.SetActive(true);

        _progressRoutine = StartCoroutine(ProgressRoutine(duration, phase));
    }

    private IEnumerator ProgressRoutine(float duration, HackFailDefensePhase phase)
    {
        if (duration <= 0f) duration = 0.01f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float pct = Mathf.Clamp01(t / duration);

            if (fillImage != null)
                fillImage.fillAmount = pct;

            if (labelText != null)
            {
                int timeLeft = Mathf.CeilToInt(duration - t);
                labelText.text = $"FAILED HACK. SURVIVE WHILE UR PARTNER HACKS{timeLeft}s";
            }

            yield return null;
        }

        // Full
        if (fillImage != null)
            fillImage.fillAmount = 1f;

        if (labelText != null)
            labelText.text = "Override complete";

        if (debugLogs)
            Debug.Log("[SakuraHackUI] Hack progress complete, notifying phase.");

        if (phase != null)
            phase.OnDefenseCompleted();

        // auto-hide after 3 seconds
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);

        _progressRoutine = null;

    }

    public void ResetUI()
    {
        if (_progressRoutine != null)
        {
            StopCoroutine(_progressRoutine);
            _progressRoutine = null;
        }

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        if (labelText != null)
            labelText.text = "";
        
        // gameObject.SetActive(false);
    }
}
