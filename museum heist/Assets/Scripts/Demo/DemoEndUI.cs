using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoEndUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root; // whole canvas or panel

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private bool _active;

    void Start()
    {
        root.SetActive(false);
    }

    void Update()
    {
        if (!_active) return;

        // 1 = confirm
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ConfirmStart();
        }

        // 2 = cancel
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Cancel();
        }
    }

    public void Show()
    {
        root.SetActive(true);
        _active = true;
        Time.timeScale = 0f;
    }

    private void ConfirmStart()
    {
        _active = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void Cancel()
    {
        _active = false;
        root.SetActive(false);
        Time.timeScale = 1f;
    }
}
