using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 8;
    public int currentLives;

    [Header("UI")]
    public TextMeshProUGUI livesText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hurtClip;
    public AudioClip deathClip;

void Start()
{
    // Only initialize if something hasn't already set lives this run.
    // (RespawnManager / GameState should set currentLives.)
    if (currentLives <= 0)
        currentLives = maxLives;

    UpdateLivesUI();
}


    public void TakeDamage(int amount)
    {
        currentLives -= amount;
        if (currentLives < 0) currentLives = 0;

        Debug.Log($"[PlayerHealth] Took {amount} damage. Lives = {currentLives}");

        if (audioSource != null && hurtClip != null)
            audioSource.PlayOneShot(hurtClip);

        UpdateLivesUI();

        if (currentLives <= 0)
            Die();
    }

 void Die()
{
    Debug.Log("[PlayerHealth] Player died → triggering respawn overlay.");

    if (audioSource != null && deathClip != null)
        audioSource.PlayOneShot(deathClip);

    // stop taking repeated damage while dead
    currentLives = 0;
    UpdateLivesUI();

    if (GameRunLogger.Instance != null)
    GameRunLogger.Instance.Log("Game", "Death", "PlayerDied", GameStateManager.Instance.currentCheckpoint.ToString());


    // call death screen
    if (DeathScreenController.Instance != null)
    {
        DeathScreenController.Instance.PlayDeathAndRespawn();
    }
    else
    {
        // fallback: reload without fade
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

public void SetLives(int value)
{
    currentLives = Mathf.Max(0, value);
    UpdateLivesUI();
}

public void AddLives(int amount)
{
    currentLives = Mathf.Clamp(currentLives + amount, 0, maxLives);
    UpdateLivesUI();
}

    void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = $"Lives: {currentLives}";
    }
}
