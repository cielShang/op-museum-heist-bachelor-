using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MusicManagerEnd : MonoBehaviour
{
    public static MusicManagerEnd Instance { get; private set; }

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip ambientClip;

    [Header("Fade")]
    public float fadeDuration = 1.0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        PlayAmbient();
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuScene")
        {
            StopAllCoroutines();

            if (musicSource != null)
                musicSource.Stop();

            // destroy whole manager so menu can use its own music cleanly
            Destroy(gameObject);
        }
    }

    public void PlayAmbient()
    {
        if (musicSource == null || ambientClip == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(ambientClip));
    }

    private IEnumerator FadeTo(AudioClip newClip)
    {
        if (musicSource.clip == newClip)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
            yield break;
        }

        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();

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
