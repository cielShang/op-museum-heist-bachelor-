using UnityEngine;

public class HeistMusicManager : MonoBehaviour
{
    public static HeistMusicManager Instance { get; private set; }

    [Header("Audio")]
    public AudioSource musicSource;     // single source for BGM
    public AudioClip ambientClip;       // normal background music
    public AudioClip dangerClip;        // fail-phase / combat music

    [Header("Fade")]
    public float fadeDuration = 1.0f;   // seconds

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayAmbient()
    {
        if (musicSource == null || ambientClip == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(ambientClip));
    }

    public void PlayDanger()
    {
        if (musicSource == null || dangerClip == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(dangerClip));
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    private System.Collections.IEnumerator FadeTo(AudioClip newClip)
    {
        if (musicSource.clip == newClip)
        {
            // Already playing this clip – just ensure it's playing
            if (!musicSource.isPlaying)
                musicSource.Play();
            yield break;
        }

        float startVolume = musicSource.volume;
        float t = 0f;

        // Fade out
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, t / fadeDuration);
            yield return null;
        }

        musicSource.volume = startVolume;
    }
}
