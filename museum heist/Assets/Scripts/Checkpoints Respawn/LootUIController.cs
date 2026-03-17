using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LootUIController : MonoBehaviour
{
    public RawImage lootIcon;
    public TMP_Text lootText;

    void Update()
    {
        if (GameStateManager.Instance == null) return;
        if (lootText != null)
            lootText.text = GameStateManager.Instance.lootCount.ToString();
    }
}
