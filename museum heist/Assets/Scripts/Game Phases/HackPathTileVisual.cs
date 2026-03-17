using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HackPathTileVisual : MonoBehaviour
{
    [Header("References")]
    public RectTransform rectTransform;   // tile rect
    public Image image;                   // tile background
    public TextMeshProUGUI symbolText;    // text overlay for ASCII symbol

    [Header("Visual Settings")]
    public float rotateDuration = 0.15f;
    public Color normalColor = Color.gray;
    public Color poweredColor = Color.cyan;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (symbolText == null)
            symbolText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (image != null)
            image.color = normalColor;
    }

    public void AnimateRotate90()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        StartCoroutine(RotateRoutine());
    }

    private IEnumerator RotateRoutine()
    {
        Quaternion start = rectTransform.localRotation;
        Quaternion end   = start * Quaternion.Euler(0f, 0f, -90f); // clockwise

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateDuration;
            rectTransform.localRotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        rectTransform.localRotation = end;
    }

    public void SetPowered(bool powered)
    {
        if (image == null) return;
        image.color = powered ? poweredColor : normalColor;
    }

    public void SetSymbol(string s)
    {
        if (symbolText == null) return;
        symbolText.text = s;
    }
}
