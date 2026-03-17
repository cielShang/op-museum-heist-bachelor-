using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreenController : MonoBehaviour
{
    public static DeathScreenController Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup canvasGroup;    // black overlay
    public TMP_Text messageText;

    [Header("Timing")]
    public float fadeOutSeconds = 0.35f;
    public float holdSeconds = 1.8f;

    [Header("Message")]
    public string deathMessage = "You got caught... relocating.";

    bool _busy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayDeathAndRespawn()
    {
        if (_busy) return;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        _busy = true;

        if (messageText != null) messageText.text = deathMessage;

        // fade to black + block input clicks
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            yield return Fade(canvasGroup, canvasGroup.alpha, 1f, fadeOutSeconds);
        }

        yield return new WaitForSeconds(holdSeconds);

        // reload current scene (RespawnManager will place player + NPC)
        var scene = SceneManager.GetActiveScene().name;
        yield return SceneManager.LoadSceneAsync(scene);

        // small safety wait (so RespawnManager has run)
        yield return null;

        // fade back in
        if (canvasGroup != null)
        {
            yield return Fade(canvasGroup, 1f, 0f, fadeOutSeconds);
            canvasGroup.blocksRaycasts = false;
        }

        _busy = false;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float time)
    {
        if (cg == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, time);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        cg.alpha = to;
    }
}
