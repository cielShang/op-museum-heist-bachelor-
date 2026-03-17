using UnityEngine;

public class EndScreenOpenButton : MonoBehaviour
{
    [Header("Input")]
    public KeyCode openKey = KeyCode.Escape;

    private bool hasOpened = false;

    private void Update()
    {
        if (!hasOpened && Input.GetKeyDown(openKey))
        {
            OpenEndScreen();
        }
    }

    public void OpenEndScreen()
    {
        if (hasOpened) return;

        hasOpened = true;

        if (EndSceneManager.Instance != null)
        {
            EndSceneManager.Instance.ForceShowEndScreen();
        }
        else
        {
            Debug.LogWarning("EndSceneManager.Instance is null!");
        }
    }
}
