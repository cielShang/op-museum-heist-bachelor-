using UnityEngine;

public class EndScreenController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Root GameObject of the end screen panel")]
    public GameObject endScreenRoot;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    private bool isOpen = false;

    private void Start()
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(false);

        // Ensure game is running normally
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;

        if (endScreenRoot != null)
            endScreenRoot.SetActive(true);

        Time.timeScale = 0f; 
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        isOpen = false;

        if (endScreenRoot != null)
            endScreenRoot.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
