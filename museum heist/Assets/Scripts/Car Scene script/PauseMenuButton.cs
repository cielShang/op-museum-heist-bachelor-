using UnityEngine;

public class PauseMenuButton : MonoBehaviour
{
    public void ToggleMenu()
    {
        if (PauseMenuController.Instance != null)
            PauseMenuController.Instance.Toggle();
        else
            Debug.LogWarning("PauseMenuController.Instance is null!");
    }
}
