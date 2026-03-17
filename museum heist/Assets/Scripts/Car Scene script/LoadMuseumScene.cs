using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMuseumScene : MonoBehaviour
{
    public string museumSceneName = "GameScene";
    public void LoadMuseum() => SceneManager.LoadScene(museumSceneName);
}
