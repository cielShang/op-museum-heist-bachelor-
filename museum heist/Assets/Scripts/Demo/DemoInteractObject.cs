using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DemoInteractObject : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;

    [Header("Visual Feedback")]
    public Light areaLight;        // child light
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;

    [Header("Progression")]
    public GameObject wallToDisable;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip activateClip;

    private bool _inside;
    private bool _activated;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (areaLight != null)
            areaLight.color = inactiveColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _inside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _inside = false;
    }

    void Update()
    {
        if (_activated || !_inside) return;

        if (Input.GetKeyDown(interactKey))
            ActivatePanel();
    }

    void ActivatePanel()
    {
        _activated = true;

        if (areaLight != null)
            areaLight.color = activeColor;

        if (wallToDisable != null)
            wallToDisable.SetActive(false);

        audioSource?.PlayOneShot(activateClip);
    }
}
