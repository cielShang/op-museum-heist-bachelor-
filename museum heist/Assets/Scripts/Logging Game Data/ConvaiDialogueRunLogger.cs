using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Service;
using Convai.Scripts.Runtime.Core;

public class ConvaiDialogueRunLogger : MonoBehaviour
{
    [Header("Logging")]
    public string phaseName = "Dialogue";

    // ---- Player mic transcript buffering ----
    private readonly StringBuilder _playerBuffer = new StringBuilder();
    private string _playerLastNonEmptyPartial = "";
    private ConvaiGRPCAPI _api;
    private bool _npcManagerHooked = false;


    // ---- NPC transcript subscription ----
    private readonly HashSet<ConvaiNPC> _subscribedNPCs = new HashSet<ConvaiNPC>();
    private readonly Dictionary<ConvaiNPC, string> _npcLastLogged = new Dictionary<ConvaiNPC, string>();

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(InitWhenReady());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeApi();
        UnsubscribeAllNpcs();

            if (_npcManagerHooked && ConvaiNPCManager.Instance != null)
    {
        ConvaiNPCManager.Instance.OnActiveNPCChanged -= HandleActiveNpcChanged;
    }
    _npcManagerHooked = false;

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ConvaiGRPCAPI exists in multiple scenes in your project, so Instance swaps.
        StartCoroutine(InitWhenReady());
    }

    private IEnumerator InitWhenReady()
    {
        // Wait for current scene’s ConvaiGRPCAPI
        while (ConvaiGRPCAPI.Instance == null)
            yield return null;

        // Re-subscribe if Instance changed
        if (_api != ConvaiGRPCAPI.Instance)
        {
            UnsubscribeApi();
            _api = ConvaiGRPCAPI.Instance;
            _api.OnResultReceived += HandleResult;
            _api.OnPlayerSpeakingChanged += HandlePlayerSpeakingChanged;

            GameRunLogger.Instance?.Log("Debug", "ConvaiDialogueRunLogger", "SubscribedGRPCScene", SceneManager.GetActiveScene().name);

            // Reset buffers when API changes
            _playerBuffer.Clear();
            _playerLastNonEmptyPartial = "";
        }

        // Hook NPC transcript events in this scene
        SubscribeToAllNpcsInScene();

        StartCoroutine(RescanNpcsForAWhile());
        HookNpcManagerIfAvailable();

    }

            private IEnumerator RescanNpcsForAWhile()
        {
            // In case NPCs spawn a bit later (common in Game scene)
            for (int i = 0; i < 20; i++) // ~10 seconds if 0.5s delay
            {
                SubscribeToAllNpcsInScene();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void HookNpcManagerIfAvailable()
        {
            if (_npcManagerHooked) return;
            if (ConvaiNPCManager.Instance == null) return;

            ConvaiNPCManager.Instance.OnActiveNPCChanged += HandleActiveNpcChanged;
            _npcManagerHooked = true;

            GameRunLogger.Instance?.Log("Debug", "NPCManagerHooked", "Status", "OK");
        }

        private void HandleActiveNpcChanged(ConvaiNPC newActiveNpc)
        {
            // Whenever Convai switches active NPC, ensure it is hooked for transcript logging
            SubscribeNpc(newActiveNpc);
        }


    // ---------------------------
    // PLAYER: capture mic transcript
    // ---------------------------
    private void HandleResult(GetResponseResponse result)
    {
        if (GameRunLogger.Instance == null || result == null) return;

        if (result.UserQuery != null)
        {
            // Mic transcripts can arrive as partials. Buffer everything.
            if (!string.IsNullOrEmpty(result.UserQuery.TextData))
            {
                _playerLastNonEmptyPartial = result.UserQuery.TextData;

                // Append text always; we’ll trim later.
                _playerBuffer.Append(result.UserQuery.TextData);
            }

            // Log once per utterance when the server says it’s complete
            if (result.UserQuery.EndOfResponse)
            {
                FlushPlayerUtterance("EndOfResponse");
            }
        }
    }

    private void HandlePlayerSpeakingChanged(bool isSpeaking)
    {
        // Fallback: if the player stops talking but EndOfResponse didn’t come through,
        // log what we captured so far.
        if (!isSpeaking)
        {
            FlushPlayerUtterance("StopSpeakingFallback");
        }
    }

    private void FlushPlayerUtterance(string reason)
    {
        if (GameRunLogger.Instance == null) return;

        string text = _playerBuffer.ToString().Trim();

        // If buffering duplicated partials, sometimes text becomes messy.
        // As a fallback, if text is empty use last non-empty partial.
        if (string.IsNullOrWhiteSpace(text))
            text = _playerLastNonEmptyPartial.Trim();

        if (!string.IsNullOrWhiteSpace(text))
        {
            GameRunLogger.Instance.Log(phaseName, "Dialogue_Player_Spoken", "Player", text);
            GameRunLogger.Instance.Log("Debug", "PlayerTranscriptFlush", "Reason", reason);
        }

        _playerBuffer.Clear();
        _playerLastNonEmptyPartial = "";
    }

    // ---------------------------
    // NPC: capture transcript from ConvaiNPC AudioManager
    // ---------------------------
    private void SubscribeToAllNpcsInScene()
    {
        // Include inactive objects too (true)
        var npcs = Object.FindObjectsOfType<ConvaiNPC>(true);
        foreach (var npc in npcs)
            SubscribeNpc(npc);
    }

    private void SubscribeNpc(ConvaiNPC npc)
    {
        if (npc == null) return;
        if (_subscribedNPCs.Contains(npc)) return;

        // AudioManager exists because ConvaiNPC.InitializeComponents adds it.
        if (npc.AudioManager == null) return;

        npc.AudioManager.OnAudioTranscriptAvailable += (string transcript) => HandleNpcTranscript(npc, transcript);

        _subscribedNPCs.Add(npc);
        GameRunLogger.Instance?.Log("Debug", "NPCTranscriptHooked", "NPC", npc.characterName);
    }

    private void HandleNpcTranscript(ConvaiNPC npc, string transcript)
    {
        if (GameRunLogger.Instance == null) return;
        if (npc == null) return;
        if (string.IsNullOrWhiteSpace(transcript)) return;

        transcript = transcript.Trim();

        // Avoid logging the same line repeatedly
        if (_npcLastLogged.TryGetValue(npc, out var last) && last == transcript)
            return;

        _npcLastLogged[npc] = transcript;

        // Label as chosen condition (Sakura/Mila) OR characterName
        string npcLabel =
            NPCSelectionManager.Instance != null
                ? NPCSelectionManager.Instance.Selected.ToString()
                : npc.characterName;

        GameRunLogger.Instance.Log(phaseName, "Dialogue_NPC", npcLabel, transcript);
    }

    private void UnsubscribeApi()
    {
        if (_api != null)
        {
            _api.OnResultReceived -= HandleResult;
            _api.OnPlayerSpeakingChanged -= HandlePlayerSpeakingChanged;
            _api = null;
        }
    }

    private void UnsubscribeAllNpcs()
    {
        // We used lambdas above, so we can't easily unsubscribe per npc without storing delegates.
        // That's okay because this object is DontDestroyOnLoad and we guard against double-subscribe.
        _subscribedNPCs.Clear();
        _npcLastLogged.Clear();
    }
}
