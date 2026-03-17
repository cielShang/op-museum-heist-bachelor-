using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CarEnterPromptPretty : MonoBehaviour
{
    [Header("Scene")]
    public string museumSceneName = "GameScene";
    public string demoSceneName = "Test_Map";

    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("UI")]
    public GameObject panelRoot;          // CarPromptPanel
    public TextMeshProUGUI titleText;     // TitleText
    public TextMeshProUGUI bodyText;      // BodyText

    [Header("Timing")]
    public float questionsHintSeconds = 2.0f;

public NPCPersonality npcForThisCarScene;


    private bool _inside;
    private bool _readyToEnter;
    private float _hintTimer;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        HidePanel();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _inside = true;
        _readyToEnter = false;
        _hintTimer = 0f;

        ShowChoicePanel();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _inside = false;
        _readyToEnter = false;
        _hintTimer = 0f;

        HidePanel();
    }

    void Update()
    {
        if (!_inside) return;

        // If player chose "questions", show hint briefly, then return to choice panel.
        if (_hintTimer > 0f)
        {
            _hintTimer -= Time.deltaTime;
            if (_hintTimer <= 0f)
                ShowChoicePanel();
            return;
        }

        // Not ready yet -> accept 1/2 input
        if (!_readyToEnter)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                ShowQuestionsHint();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                _readyToEnter = true;
                ShowEnterPanel();
            }
        }
        // Ready -> accept E
        else
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                NPCSelectionManager.EnsureExists();
                NPCSelectionManager.Instance.Selected = npcForThisCarScene;

                var selected = NPCSelectionManager.Instance.Selected;
                GameRunLogger.Instance?.SceneEnd($"Car_{selected}");


               // SceneManager.LoadScene(museumSceneName);
                SceneManager.LoadScene(demoSceneName);
            }

            else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                // allow changing mind
                _readyToEnter = false;
                ShowQuestionsHint();
            }
        }
    }

    // ---------- UI States ----------

    void ShowChoicePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "Before we go…";

        if (bodyText != null)
        {
            bodyText.text =
                "<b>[1]</b> I still have questions\n" +
                "<b>[2]</b> I'm ready";
        }
    }

    void ShowQuestionsHint()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "No problem.";

        if (bodyText != null)
            bodyText.text = "Then talk with your partner.";

        _hintTimer = questionsHintSeconds;
    }

    void ShowEnterPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "Alright.";

        if (bodyText != null)
            bodyText.text = "Press <b>[E]</b> to start the demo.";
    }

    void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
