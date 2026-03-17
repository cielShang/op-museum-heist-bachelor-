using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenController : MonoBehaviour
{
    public string carSceneSakura = "Car_Sakura";
    public string carSceneMila   = "Car_Mila";

    public void ChooseSakura()
    {
        StartNewGame(NPCPersonality.Sakura, carSceneSakura);
    }

    public void ChooseMila()
    {
        StartNewGame(NPCPersonality.Mila, carSceneMila);
    }

private void StartNewGame(NPCPersonality personality, string sceneName)
{
    NPCSelectionManager.EnsureExists();

    if (GameStateManager.Instance != null)
        GameStateManager.Instance.ResetForNewGame();

    NPCSelectionManager.Instance.Selected = personality;

    GameRunLogger.Instance?.Log("Menu", "NPC_Selected", "SelectedNPC", personality.ToString());
    GameRunLogger.Instance?.SceneStart($"Car_{personality}");
    GameRunLogger.Instance?.Log("Menu", "LoadScene", "CarScene", sceneName);

    SceneManager.LoadScene(sceneName);
}

}
