using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameRunLogger : MonoBehaviour
{
    public static GameRunLogger Instance;

    [Header("Session")]
    public string sessionId; // generated on Awake

    private List<string> rows = new List<string>();
    private float runStartTime;

    // phase timing
    private Dictionary<string, float> phaseStartTimes = new Dictionary<string, float>();

    // scene timing
    private Dictionary<string, float> sceneStartTimes = new Dictionary<string, float>();

    // convenience
    float T => Time.time - runStartTime;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        runStartTime = Time.time;

        // Session ID: timestamp + random suffix
        sessionId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}";

        // CSV header
        rows.Add("SessionId,Timestamp,Phase,EventType,Detail,Value");
        Log("Game", "SessionStart", "SessionId", sessionId);
    }

    // ---------- Core logging ----------

    public void Log(string phase, string eventType, string detail, string value = "")
    {
        string CsvSafe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\"", "\"\"");                 // escape quotes
            s = s.Replace("\r", " ").Replace("\n", " "); // keep it one line per row
            return s;
        }

        detail = CsvSafe(detail);
        value = CsvSafe(value);

        string line = $"{sessionId},{T:F2},{phase},{eventType},\"{detail}\",\"{value}\"";
        rows.Add(line);
        Debug.Log("[LOG] " + line);
    }

    // ---------- Phase timing helpers ----------

    public void PhaseStart(string phase, string detail = "")
    {
        phaseStartTimes[phase] = T;
        Log(phase, "PhaseStart", string.IsNullOrEmpty(detail) ? "Started" : detail, "");
    }

    public void PhaseComplete(string phase, string detail = "Completed")
    {
        float duration = -1f;

        if (phaseStartTimes.TryGetValue(phase, out float start))
            duration = T - start;

        Log(phase, "PhaseComplete", detail, duration >= 0 ? duration.ToString("F2") : "N/A");
    }

    // ---------- Scene timing helpers ----------

    public void SceneStart(string sceneKey)
    {
        sceneStartTimes[sceneKey] = T;
        Log("Scene", "SceneStart", sceneKey, "");
    }

    public void SceneEnd(string sceneKey, string detail = "")
    {
        float duration = -1f;
        if (sceneStartTimes.TryGetValue(sceneKey, out float start))
            duration = T - start;

        Log("Scene", "SceneEnd", sceneKey, duration >= 0 ? duration.ToString("F2") : "N/A");
        if (!string.IsNullOrEmpty(detail))
            Log("Scene", "SceneEndDetail", sceneKey, detail);
    }

    // ---------- Loot helpers ----------

    public void LootCollected(string lootId, int value, string phase = "Game")
    {
        Log(phase, "Loot", "Collected", $"{lootId}:{value}");
    }

    // ---------- Respawn helpers ----------

// Call once when the level/game starts and you place the player initially.
// This will NOT be counted as a death respawn in the summary.
public void PlayerSpawned(string spawnPointName, Vector3 position)
{
    Log("Game", "Spawn", spawnPointName, $"{position.x:F2},{position.y:F2},{position.z:F2}");
}

// Call ONLY when the player died and is being brought back.
public void PlayerRespawnedAfterDeath(string spawnPointName, Vector3 position)
{
    Log("Game", "RespawnAfterDeath", spawnPointName, $"{position.x:F2},{position.y:F2},{position.z:F2}");
}


    // -------- Summary generation --------

    private static List<string> SplitCsv(string line)
    {
        // Splits one CSV line into columns, respecting quotes.
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Handle escaped quotes ("")
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    private static bool IsSafeOutcome(string outcome)
    {
        // SAFE outcomes
        return outcome == "CorridorPassed"
            || outcome == "Key"
            || outcome == "KeycardUsed";
    }

    private static bool IsRiskyOutcome(string outcome)
    {
        // RISKY outcomes
        return outcome == "JumpLanded"
            || outcome == "Hack"
            || outcome == "Hacked"
            || outcome == "HackComplete";
    }

    private List<string> BuildSummaryLinesFromRows()
    {
        // Columns: SessionId,Timestamp,Phase,EventType,Detail,Value
        string selectedNpc = null;

        // scene durations: key -> seconds
        var sceneDurations = new Dictionary<string, string>();

        // Phase completions: list of (phase, outcome/detail, seconds)
        var phaseCompletions = new List<(string phase, string outcome, string seconds)>();

        // Choice logs (like Phase3 Choice PathChosen Safe)
        var choices = new List<(string phase, string what, string value)>();
        var respawnWhere = new List<string>();

        int lootTotal = 0;
        int safeCount = 0;
        int riskyCount = 0;
        int respawnCount = 0;

        foreach (var line in rows)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("SessionId,")) continue; // header

            var cols = SplitCsv(line);
            if (cols.Count < 6) continue;

            string phase = cols[2];
            string eventType = cols[3];
            string detail = cols[4];
            string value = cols[5];

            // NPC selected in menu
            if (phase == "Menu" && eventType == "NPC_Selected" && detail == "SelectedNPC")
            {
                selectedNpc = value;
            }

            // Scene timing
            if (phase == "Scene" && eventType == "SceneEnd")
            {
                // detail is scene key (e.g., Car_Sakura, Demo_Mila)
                sceneDurations[detail] = value;
            }

            // Respawn count
            // Respawn means AFTER death only
            if (phase == "Game" && eventType == "RespawnAfterDeath")
            {
                respawnCount++;

                // detail = spawnPointName, value = "x,y,z"
                respawnWhere.Add($"{detail} ({value})");
            }



            // Phase completion timing + SAFE/RISKY counts for Phase1/Phase2
            if (eventType == "PhaseComplete")
            {
                phaseCompletions.Add((phase, detail, value));

                if (IsSafeOutcome(detail)) safeCount++;
                else if (IsRiskyOutcome(detail)) riskyCount++;
            }

            // Phase choices (Phase3 PathChosen Safe/Risky)
            if (eventType == "Choice")
            {
                choices.Add((phase, detail, value));

                // Count only the main path choice once (3 paths total)
                if (detail == "PathChosen")
                {
                    if (value == "Safe") safeCount++;
                    else if (value == "Risky") riskyCount++;
                }
            }

            // Loot total (Value looks like Loot_11:1)
            if (phase == "Game" && eventType == "Loot" && detail == "Collected")
            {
                int colon = value.LastIndexOf(':');
                if (colon >= 0 && colon + 1 < value.Length)
                {
                    if (int.TryParse(value.Substring(colon + 1), out int amount))
                        lootTotal += amount;
                }
            }
        }

        // ---- Interpretations ----
        string InterpretOutcome(string outcome)
        {
            // Mapping you gave:
            // corridor passed = sad choice (safe)
            // jump landed = risky choice
            // hack = risky
            // key = safe
            switch (outcome)
            {
                case "CorridorPassed":
                    return "Corridor passed (sad choice / SAFE)";
                case "JumpLanded":
                    return "Jump landed (RISKY)";
                case "Hack":
                case "Hacked":
                case "HackComplete":
                    return "Hack (RISKY)";
                case "Key":
                case "KeycardUsed":
                    return "Key / keycard (SAFE)";
                default:
                    return outcome;
            }
        }

        string FormatSeconds(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "N/A") return "N/A";
            return s + "s";
        }

        // ---- Build human summary lines ----
        var summary = new List<string>();

        if (!string.IsNullOrEmpty(selectedNpc))
            summary.Add($"The player chose {selectedNpc}.");

        // Car duration
        if (!string.IsNullOrEmpty(selectedNpc))
        {
            string carKey = $"Car_{selectedNpc}";
            if (sceneDurations.TryGetValue(carKey, out string carSecs))
                summary.Add($"The car scene took {FormatSeconds(carSecs)}.");
            else
                summary.Add("The car scene duration was not recorded.");
        }

        // Demo duration
        if (!string.IsNullOrEmpty(selectedNpc))
        {
            string demoKey = $"Demo_{selectedNpc}";
            if (sceneDurations.TryGetValue(demoKey, out string demoSecs))
                summary.Add($"The demo took {FormatSeconds(demoSecs)}.");
            else
                summary.Add("The demo duration was not recorded.");
        }

        // Phase completions (with SAFE/RISKY meanings)
        foreach (var pc in phaseCompletions)
        {
            string interpreted = InterpretOutcome(pc.outcome);

            if (pc.seconds == "N/A" || string.IsNullOrWhiteSpace(pc.seconds))
                summary.Add($"{pc.phase} completed: {interpreted}. (Duration: N/A)");
            else
                summary.Add($"{pc.phase} completed: {interpreted}. It took {FormatSeconds(pc.seconds)}.");
        }

        // Explicit choice logs (Phase3 PathChosen Safe/Risky)
        foreach (var c in choices)
        {
            string label = c.value == "Safe" ? "SAFE" : (c.value == "Risky" ? "RISKY" : c.value);
            summary.Add($"{c.phase}: {c.what} = {label}.");
        }

        // Total loot
        summary.Add($"Total loot collected: {lootTotal}.");

        // SAFE/RISKY totals across 3 paths (Phase1 + Phase2 + Phase3)
        summary.Add($"Choices summary (3 paths): SAFE = {safeCount}, RISKY = {riskyCount}.");

        // Respawns
        summary.Add($"Respawns after death: {respawnCount}.");

if (respawnWhere.Count > 0)
{
    summary.Add("Respawn locations: " + string.Join(" | ", respawnWhere) + ".");
}


        return summary;
    }

    // ---------- File writing ----------

    public string WriteFile()
{
    string fileName = $"HeistRun_{sessionId}.csv";

    // Unity-safe Documents folder
    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    string folder = Path.Combine(documentsPath, "OperationMuseumHeist_Logs");

    //  CREATES the folder automatically if it doesn't exist
    if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

    string path = Path.Combine(folder, fileName);

    // Build output
    var output = new List<string>(rows);

    output.Add("");
    output.Add("SUMMARY");
    output.AddRange(BuildSummaryLinesFromRows());

    File.WriteAllLines(path, output);
    Debug.Log($"[GameRunLogger] Log written to: {path}");

// #if UNITY_STANDALONE_OSX
//     //  Open the folder automatically so you see it
//     Application.OpenURL("file://" + folder);
// #endif

    return path;
}

}
