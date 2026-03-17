using System.Collections;
using UnityEngine;
using TMPro;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(Collider))]
public class P3VitrineInteractable : MonoBehaviour
{
    [Header("Objects to Hide On Open")]
    public GameObject vitrineVisualRoot; // drag the actual mesh root here (child)

    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [Tooltip("Shown when player is inside vitrine trigger (e.g. 'Claim your prize! (E)')")]
    public CanvasGroup claimUiGroup;
    public TMP_Text claimText;

    [Tooltip("Your Phase 3 objective UI group (hide it when vitrine becomes available)")]
    public CanvasGroup objectiveUiGroup;

    [Header("Highlight Blink")]
    [Tooltip("Any renderers on the vitrine that should blink")]
    public Renderer[] vitrineRenderers;

    [Tooltip("If your material supports emission, enable this.")]
    public bool useEmission = true;

    [Tooltip("Fallback if emission doesn't work: changes base color.")]
    public bool useBaseColorFallback = true;

    public Color blinkColor = Color.yellow;
    public float blinkInterval = 0.35f;

    [Header("Open + Reward")]
    public AudioSource audioSource;
    public AudioClip openClip;

    [Tooltip("Spawned when the vitrine is opened. (Treasure prefab)")]
    public GameObject treasurePrefab;

    [Tooltip("Optional: if you already placed treasure in scene disabled, reference it here instead.")]
    public GameObject treasureExisting;

    [Tooltip("Where treasure appears. If null, uses this transform.")]
    public Transform treasureSpawnPoint;

    [Header("Narrative Design (Convai Events)")]
    public string ndOnPrizeClaimed = "P3_WIN_PRIZE";   // Mila/Sakura happy line
    public string ndLetsDriveOff   = "P3_WIN_ESCAPE";  

    [Header("Exit Portal (spawn after prize)")]
    public P3ExitPortalSpawner exitPortalSpawner;      
    public float portalSpawnDelay = 1.5f;              

    private bool _playerInside;
    private bool _opened;
    private Coroutine _blinkRoutine;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        SetClaimUI(false);

        if (vitrineRenderers == null || vitrineRenderers.Length == 0)
            vitrineRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        if (_opened) return;
        if (!_playerInside) return;

        if (Input.GetKeyDown(interactKey))
        {
            OpenVitrine();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_opened) return;
        if (!other.CompareTag(playerTag)) return;

        _playerInside = true;

        // Hide objective UI now (so we don't show both)
        SetCanvasGroup(objectiveUiGroup, false);

        // Show claim UI
        SetClaimUI(true);

        // Start blinking
        StartBlink();
    }

    void OnTriggerExit(Collider other)
    {
        if (_opened) return;
        if (!other.CompareTag(playerTag)) return;

        _playerInside = false;
        SetClaimUI(false);
        StopBlink();
    }

    private void OpenVitrine()
    {
        _opened = true;
        _playerInside = false;

        SetClaimUI(false);
        StopBlink();

        // sound
        if (audioSource != null && openClip != null)
            audioSource.PlayOneShot(openClip);

        // spawn / enable treasure
        Vector3 pos = treasureSpawnPoint != null ? treasureSpawnPoint.position : transform.position;
        Quaternion rot = treasureSpawnPoint != null ? treasureSpawnPoint.rotation : transform.rotation;

        if (treasureExisting != null)
        {
            treasureExisting.transform.SetPositionAndRotation(pos, rot);
            treasureExisting.SetActive(true);
        }
        else if (treasurePrefab != null)
        {
            Instantiate(treasurePrefab, pos, rot);
        }

        // hard-disable the whole vitrine visuals (recommended)
        if (vitrineVisualRoot != null)
            vitrineVisualRoot.SetActive(false);
        else
        {
            // fallback: disable renderers (only works if assigned)
            foreach (var r in vitrineRenderers)
                if (r != null) r.enabled = false;
        }

        GameRunLogger.Instance.PhaseComplete("Phase3", "VitrineOpened");


        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Trigger ND (happy line)
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null)
        {
            npc.InterruptCharacterSpeech();
            npc.TriggerEvent(ndOnPrizeClaimed);

            // optional 2nd ND line after a short beat
            if (!string.IsNullOrEmpty(ndLetsDriveOff))
                StartCoroutine(TriggerSecondEventAfterDelay(npc, ndLetsDriveOff, 1.2f));
        }

        //  NEW: spawn the exit portal after a delay
        if (exitPortalSpawner != null)
            StartCoroutine(SpawnPortalAfterDelay());
        else
            Debug.LogWarning("[P3VitrineInteractable] exitPortalSpawner not assigned.", this);
    }

    private IEnumerator SpawnPortalAfterDelay() //  NEW
    {
        yield return new WaitForSeconds(portalSpawnDelay);
        exitPortalSpawner.SpawnPortalAfterWin();
    }

    private IEnumerator TriggerSecondEventAfterDelay(ConvaiNPC npc, string evt, float delay)
    {
        if (npc == null) yield break;
        if (string.IsNullOrEmpty(evt)) yield break;
        yield return new WaitForSeconds(delay);
        npc.TriggerEvent(evt);
    }

    private void SetClaimUI(bool on)
    {
        if (claimText != null)
            claimText.text = "Claim your prize! (E)";

        SetCanvasGroup(claimUiGroup, on);
    }

    private void SetCanvasGroup(CanvasGroup g, bool on)
    {
        if (g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    private void StartBlink()
    {
        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }
        SetHighlight(false);
    }

    private IEnumerator BlinkRoutine()
    {
        bool on = false;

        while (!_opened && _playerInside)
        {
            on = !on;
            SetHighlight(on);
            yield return new WaitForSeconds(blinkInterval);
        }

        SetHighlight(false);
    }

    private void SetHighlight(bool on)
    {
        if (vitrineRenderers == null) return;

        foreach (var r in vitrineRenderers)
        {
            if (r == null) continue;

            var mat = r.material;
            if (mat == null) continue;

            if (useEmission && mat.HasProperty("_EmissionColor"))
            {
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", blinkColor);
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
            else if (useBaseColorFallback)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", on ? blinkColor : Color.white);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", on ? blinkColor : Color.white);
            }
        }
    }
}
