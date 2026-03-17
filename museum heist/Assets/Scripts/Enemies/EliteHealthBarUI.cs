using UnityEngine;
using UnityEngine.UI;

public class EliteHealthBarUI : MonoBehaviour
{
    public EliteEnemy enemy;
    public Image fillImage;

    void Update()
    {
        if (enemy == null || fillImage == null) return;
        fillImage.fillAmount = enemy.HealthPercent;
    }
}
