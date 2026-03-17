using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Convai.Scripts.Runtime.Core;

public class HackMinigame : MonoBehaviour
{
    private enum HackPhase
    {
        None = 0,
        ArrowSequence = 1,
        PathPuzzle = 2
    }

    private enum TileType
    {
        Empty,
        Straight,
        Corner,
        TJunction,
        Source,
        Target
    }

    private class PathTile
    {
        public TileType type;
        public int rotation; // 0..3
    }

    private enum ArrowDir
    {
        Up,
        Down,
        Left,
        Right
    }
[Header("On hack success: clear these wave managers")]
public DroneWaveManager[] waveManagersToClear;

    [Header("UI")]
    public GameObject panelRoot;             // HackPanel
    public TextMeshProUGUI sequenceText;
    public TextMeshProUGUI titleText;

    [Header("Guards controlled by this terminal")]
    public GuardDronePatrol[] guardsToDisable;

    [Header("Phase 1: Inverted Arrow Settings")]
    [Tooltip("How many arrows must be answered correctly in a row.")]
    public int arrowCount = 6;

    [Tooltip("Time (seconds) allowed per arrow.")]
    public float timePerArrow = 1.2f;

    [Tooltip("Short delay before the first arrow so the E key to interact isn't counted.")]
    public float phase1InputDelay = 0.15f;

    [Header("Phase 2: Path Puzzle Settings")]
    [Tooltip("Time limit for the second-stage path puzzle.")]
    public float pathPhaseTime = 20f;

    [Tooltip("Use keys 1-9 to rotate tiles in a 3x3 grid.")]
    public int gridRows = 3;
    public int gridCols = 3;

    [Header("Phase 2: Visuals")]
    [Tooltip("Root GameObject of the 3x3 tile grid (e.g. 'Grid 3x3').")]
    public GameObject pathGridRoot;

    [Tooltip("Visual tiles in row-major order: index 0..8 corresponds to keys 1..9.")]
    public HackPathTileVisual[] tileVisuals;

    [Header("Convai (optional)")]
    public ConvaiNPC convaiNPC;

    public TerminalInteractable currentTerminal;   // set by the terminal that started the hack


    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip intermediateSuccessClip;   // after Phase 1

    private LaserGateController _gate;

    // Phase management
    private HackPhase _phase = HackPhase.None;
    private bool _active;
    private bool _completed;

    // Phase 1: arrow sequence data
    private List<ArrowDir> _arrowSequence = new();
    private int _arrowIndex;
    private float _arrowTimeLeft;
    private float _phase1DelayTimer;

    // Phase 2: path puzzle data
    private PathTile[,] _grid;
    private float _pathPhaseTimeLeft;
    private bool[,] _lastVisitedGrid;
    private bool _pathCurrentlySolved;

    // Expose state for TerminalInteractable
    public bool IsActive    => _active;
    public bool IsCompleted => _completed;

    void Start()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pathGridRoot != null)
            pathGridRoot.SetActive(false);
    }

    void Update()
    {
        if (!_active) return;

        switch (_phase)
        {
            case HackPhase.ArrowSequence:
                UpdateArrowPhase();
                break;

            case HackPhase.PathPuzzle:
                UpdatePathPuzzlePhase();
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    void Speak(string message)
    {
        if (convaiNPC == null) return;

        convaiNPC.InterruptCharacterSpeech();
        convaiNPC.TriggerSpeech(message);
    }

    public void StartHack(LaserGateController gate)
    {
        if (_completed) return;

        _gate   = gate;
        _active = true;

        StartPhase1Arrows();
    }

    // -------------------------------------------------------------------------
    // PHASE 1: Inverted arrow reaction test (using ARROW KEYS)
    // -------------------------------------------------------------------------

    private void StartPhase1Arrows()
    {
        _phase = HackPhase.ArrowSequence;
        _arrowSequence.Clear();

        for (int i = 0; i < arrowCount; i++)
        {
            ArrowDir dir = (ArrowDir)Random.Range(0, 4);
            _arrowSequence.Add(dir);
        }

        _arrowIndex = 0;
        _phase1DelayTimer = phase1InputDelay;

        if (pathGridRoot != null)
            pathGridRoot.SetActive(false);

        UpdateArrowText();

        if (titleText != null)
            titleText.text = "TERMINAL OVERRIDE - HUMAN CHECK";

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Speak("[HACK] Security challenge. Watch the arrow and press the opposite arrow key. Don't mess it up.");
    }

private void UpdateArrowPhase()
{
    // ignore input briefly so the E-press doesn't count
    if (_phase1DelayTimer > 0f)
    {
        _phase1DelayTimer -= Time.deltaTime;
        return;
    }

    // 🔹 make sure UI reflects the current time EVERY frame
    UpdateArrowText();

    // listen for ARROW KEYS only
    if (Input.anyKeyDown)
    {
        KeyCode pressed = KeyCode.None;

        if (Input.GetKeyDown(KeyCode.UpArrow))    pressed = KeyCode.UpArrow;
        if (Input.GetKeyDown(KeyCode.DownArrow))  pressed = KeyCode.DownArrow;
        if (Input.GetKeyDown(KeyCode.LeftArrow))  pressed = KeyCode.LeftArrow;
        if (Input.GetKeyDown(KeyCode.RightArrow)) pressed = KeyCode.RightArrow;

        if (pressed != KeyCode.None)
        {
            HandleArrowInput(pressed);
        }
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
        CancelHack();
    }
}


    private void HandleArrowInput(KeyCode key)
    {
        if (_arrowIndex < 0 || _arrowIndex >= _arrowSequence.Count)
            return;

        ArrowDir currentArrow = _arrowSequence[_arrowIndex];
        KeyCode required = GetOppositeKeyForArrow(currentArrow);

        if (key == required)
        {
            // correct response
            _arrowIndex++;

            if (_arrowIndex >= _arrowSequence.Count)
            {
                Phase1Success();
                return;
            }

            // next arrow
            
            UpdateArrowText();
        }
        else
        {
            FailHack("Wrong response on the security prompt");
        }
    }

    private KeyCode GetOppositeKeyForArrow(ArrowDir dir)
    {
        // We show arrow ↑ ↓ ← →, want opposite ARROW key
        switch (dir)
        {
            case ArrowDir.Up:    return KeyCode.DownArrow;  // press down
            case ArrowDir.Down:  return KeyCode.UpArrow;    // press up
            case ArrowDir.Left:  return KeyCode.RightArrow; // press right
            case ArrowDir.Right: return KeyCode.LeftArrow;  // press left
        }
        return KeyCode.None;
    }

    private string GetArrowChar(ArrowDir dir)
    {
        switch (dir)
        {
            case ArrowDir.Up:    return "↑";
            case ArrowDir.Down:  return "↓";
            case ArrowDir.Left:  return "←";
            case ArrowDir.Right: return "→";
        }
        return "?";
    }

private void UpdateArrowText()
{
    if (sequenceText == null) return;

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<b>PHASE 1 – SECURITY CHECK</b>");
    sb.AppendLine();
   // sb.AppendLine("Watch the arrow.");
    sb.AppendLine("Press the <b>OPPOSITE</b> arrow key:");
    sb.AppendLine("Example: ↑ press ↓   |   ← press →");
    sb.AppendLine();

    ArrowDir current = _arrowSequence[_arrowIndex];
    string arrowChar = GetArrowChar(current);

    // ORANGE arrow (strong visual cue)
    sb.AppendLine(
        $"Arrow: <size=150%><b><color=#FFA500>{arrowChar}</color></b></size>"
    );

    sb.AppendLine();
    sb.AppendLine($"Step {(_arrowIndex + 1)} / {_arrowSequence.Count}");
    sb.AppendLine();
    sb.AppendLine("Press <b>ESC</b> to cancel hacking.");

    sequenceText.text = sb.ToString();
}



    private void Phase1Success()
    {
        if (audioSource != null && intermediateSuccessClip != null)
            audioSource.PlayOneShot(intermediateSuccessClip);

        StartPhase2PathPuzzle();
    }

    // -------------------------------------------------------------------------
    // PHASE 2: Path puzzle with multiple patterns
    // -------------------------------------------------------------------------

    private void StartPhase2PathPuzzle()
    {
        _phase = HackPhase.PathPuzzle;

        InitPathGrid(); // picks random pattern + random rotations

        if (pathGridRoot != null)
            pathGridRoot.SetActive(true);

        _pathCurrentlySolved = CheckPathSolved();
        UpdatePathVisuals();
        UpdatePathText();

        if (titleText != null)
            titleText.text = "TERMINAL OVERRIDE - ROUTE POWER";

        Speak("[HACK] Phase 1 cleared. Now route power from S to T. Rotate nodes with 1–9, then confirm with E.");
    }

    private void InitPathGrid()
    {
        gridRows = 3;
        gridCols = 3;
        _grid = new PathTile[gridRows, gridCols];

        int patternIndex = Random.Range(0, 3); // 0,1,2

        switch (patternIndex)
        {
            case 0:
                InitPatternA();
                break;
            case 1:
                InitPatternB();
                break;
            case 2:
                InitPatternC();
                break;
        }

        RandomizeRotations();
    }

    // Pattern A – simple left-to-right through middle
    private void InitPatternA()
    {
        // Row 0
        _grid[0, 0] = MakeTile(TileType.Corner, 0);
        _grid[0, 1] = MakeTile(TileType.Straight, 1);
        _grid[0, 2] = MakeTile(TileType.Corner, 2);

        // Row 1
        _grid[1, 0] = MakeTile(TileType.Source, 1);
        _grid[1, 1] = MakeTile(TileType.TJunction, 0);
        _grid[1, 2] = MakeTile(TileType.Target, 3);

        // Row 2
        _grid[2, 0] = MakeTile(TileType.Corner, 3);
        _grid[2, 1] = MakeTile(TileType.Straight, 0);
        _grid[2, 2] = MakeTile(TileType.Corner, 1);
    }

    // Pattern B – snake from top-left down then right
    private void InitPatternB()
    {
        // Row 0
        _grid[0, 0] = MakeTile(TileType.Source, 2);       // source top-left
        _grid[0, 1] = MakeTile(TileType.Straight, 1);     // goes down
        _grid[0, 2] = MakeTile(TileType.Corner, 1);       // corner down->right

        // Row 1
        _grid[1, 0] = MakeTile(TileType.Corner, 0);       // from up->right
        _grid[1, 1] = MakeTile(TileType.TJunction, 1);
        _grid[1, 2] = MakeTile(TileType.Straight, 1);     // vertical

        // Row 2
        _grid[2, 0] = MakeTile(TileType.Empty, 0);
        _grid[2, 1] = MakeTile(TileType.Corner, 3);       // from left->up
        _grid[2, 2] = MakeTile(TileType.Target, 0);       // target bottom-right
    }

    // Pattern C – source bottom-left, target top-right diagonal snake
    private void InitPatternC()
    {
        // Row 0
        _grid[0, 0] = MakeTile(TileType.Empty, 0);
        _grid[0, 1] = MakeTile(TileType.Corner, 0);       // up->right
        _grid[0, 2] = MakeTile(TileType.Target, 2);       // target top-right

        // Row 1
        _grid[1, 0] = MakeTile(TileType.Straight, 1);     // vertical
        _grid[1, 1] = MakeTile(TileType.TJunction, 2);
        _grid[1, 2] = MakeTile(TileType.Straight, 1);     // vertical

        // Row 2
        _grid[2, 0] = MakeTile(TileType.Source, 0);       // bottom-left source
        _grid[2, 1] = MakeTile(TileType.Corner, 2);       // from down->right
        _grid[2, 2] = MakeTile(TileType.Straight, 0);     // horizontal
    }

    private PathTile MakeTile(TileType type, int rotation)
    {
        return new PathTile { type = type, rotation = rotation % 4 };
    }

    private void RandomizeRotations()
    {
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                var t = _grid[r, c];
                if (t == null) continue;
                t.rotation = Random.Range(0, 4);
            }
        }
    }

private void UpdatePathPuzzlePhase()
{

    //  update UI every frame so the countdown is live
    UpdatePathText();

    // rotate tiles with keys 1–9
    for (int i = 0; i < 9; i++)
    {
        KeyCode key = KeyCode.Alpha1 + i;
        if (Input.GetKeyDown(key))
        {
            int index = i;
            int row = index / gridCols;
            int col = index % gridCols;

            RotateTile(row, col);

            _pathCurrentlySolved = CheckPathSolved();
            UpdatePathVisuals();
            // UpdatePathText();   // <- this is now optional, we already call it above
        }
    }

    // confirm with E
    if (Input.GetKeyDown(KeyCode.E))
    {
        if (_pathCurrentlySolved)
        {
            FinalSuccessHack();
        }
        else
        {
           Speak("[HACK] The route isn’t complete yet. S must connect to T.");

        }
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
        CancelHack();
    }
}


private void UpdatePathText()
{
    if (sequenceText == null) return;

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<b>PHASE 2 – POWER ROUTING</b>");
    sb.AppendLine();
    //sb.AppendLine("Rotate tiles to connect <b>S</b> to <b>T</b>.");
    //sb.AppendLine();
    sb.AppendLine("Controls:");
    sb.AppendLine("• Press <b>1–9</b> to rotate tiles");
    sb.AppendLine("• Confirm with <b>E</b> when S and T are blue");
    sb.AppendLine();

    sequenceText.text = sb.ToString();
}


    private void RotateTile(int row, int col)
    {
        if (row < 0 || row >= gridRows || col < 0 || col >= gridCols) return;
        var tile = _grid[row, col];
        if (tile == null) return;

        tile.rotation = (tile.rotation + 1) % 4;

        int index = row * gridCols + col;
        if (tileVisuals != null && index >= 0 && index < tileVisuals.Length)
        {
            var vis = tileVisuals[index];
            if (vis != null)
                vis.AnimateRotate90();
        }
    }

    // -------------------------------------------------------------------------
    // Path solving & visuals
    // -------------------------------------------------------------------------

    private bool CheckPathSolved()
    {
        if (_grid == null) return false;

        int startRow = -1, startCol = -1;
        int targetRow = -1, targetCol = -1;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                var t = _grid[r, c];
                if (t == null) continue;

                if (t.type == TileType.Source)
                {
                    startRow = r;
                    startCol = c;
                }
                else if (t.type == TileType.Target)
                {
                    targetRow = r;
                    targetCol = c;
                }
            }
        }

        if (startRow == -1 || targetRow == -1) return false;

        bool[,] visited = new bool[gridRows, gridCols];
        var stack = new Stack<(int r, int c)>();
        stack.Push((startRow, startCol));
        visited[startRow, startCol] = true;

        while (stack.Count > 0)
        {
            var (r, c) = stack.Pop();
            var tile = _grid[r, c];
            if (tile == null) continue;

            TryExploreNeighbor(r, c, r - 1, c, 0, visited, stack); // up
            TryExploreNeighbor(r, c, r + 1, c, 1, visited, stack); // down
            TryExploreNeighbor(r, c, r, c - 1, 2, visited, stack); // left
            TryExploreNeighbor(r, c, r, c + 1, 3, visited, stack); // right
        }

        _lastVisitedGrid = visited;
        return visited[targetRow, targetCol];
    }

    private void TryExploreNeighbor(int r, int c, int nr, int nc, int dirIndex,
                                    bool[,] visited, Stack<(int,int)> stack)
    {
        if (nr < 0 || nr >= gridRows || nc < 0 || nc >= gridCols) return;
        if (visited[nr, nc]) return;

        var fromTile = _grid[r, c];
        var toTile = _grid[nr, nc];
        if (fromTile == null || toTile == null) return;

        if (HasConnection(fromTile, dirIndex) &&
            HasConnection(toTile, OppositeDirection(dirIndex)))
        {
            visited[nr, nc] = true;
            stack.Push((nr, nc));
        }
    }

    private int OppositeDirection(int dir)
    {
        switch (dir)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 3;
            case 3: return 2;
        }
        return -1;
    }

    private bool HasConnection(PathTile tile, int dirIndex)
    {
        if (tile == null) return false;

        int rot = tile.rotation % 4;

        switch (tile.type)
        {
            case TileType.Source:
            case TileType.Target:
                return HasConnectionsForStraight(rot, dirIndex);
            case TileType.Straight:
                return HasConnectionsForStraight(rot, dirIndex);
            case TileType.Corner:
                return HasConnectionsForCorner(rot, dirIndex);
            case TileType.TJunction:
                return HasConnectionsForTJunction(rot, dirIndex);
            default:
                return false;
        }
    }

    private bool HasConnectionsForStraight(int rot, int dirIndex)
    {
        bool horizontal = (rot % 2 == 0);
        if (horizontal)      // left-right
            return (dirIndex == 2 || dirIndex == 3);
        else                 // up-down
            return (dirIndex == 0 || dirIndex == 1);
    }

    private bool HasConnectionsForCorner(int rot, int dirIndex)
    {
        switch (rot % 4)
        {
            case 0: return (dirIndex == 0 || dirIndex == 3); // up + right
            case 1: return (dirIndex == 3 || dirIndex == 1); // right + down
            case 2: return (dirIndex == 1 || dirIndex == 2); // down + left
            case 3: return (dirIndex == 2 || dirIndex == 0); // left + up
        }
        return false;
    }

    private bool HasConnectionsForTJunction(int rot, int dirIndex)
    {
        switch (rot % 4)
        {
            case 0: return (dirIndex == 0 || dirIndex == 2 || dirIndex == 3); // up, left, right
            case 1: return (dirIndex == 3 || dirIndex == 0 || dirIndex == 1); // right, up, down
            case 2: return (dirIndex == 1 || dirIndex == 2 || dirIndex == 3); // down, left, right
            case 3: return (dirIndex == 2 || dirIndex == 0 || dirIndex == 1); // left, up, down
        }
        return false;
    }

    private string GetTileSymbolForTile(PathTile tile)
    {
        if (tile == null) return "";

        switch (tile.type)
        {
            case TileType.Source:  return "S";
            case TileType.Target:  return "T";
            case TileType.Straight:
                return (tile.rotation % 2 == 0) ? "│" : "─";
            case TileType.Corner:
                switch (tile.rotation % 4)
                {
                    case 0: return "┌";
                    case 1: return "┐";
                    case 2: return "┘";
                    case 3: return "└";
                }
                break;
            case TileType.TJunction:
                return "┼";
        }
        return "";
    }

    private void UpdatePathVisuals()
    {
        if (tileVisuals == null || _lastVisitedGrid == null)
            return;

        int totalTiles = gridRows * gridCols;
        if (tileVisuals.Length < totalTiles) return;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                int index = r * gridCols + c;
                var vis = tileVisuals[index];
                if (vis == null) continue;

                var tile = _grid[r, c];

                bool powered = _lastVisitedGrid[r, c];
                vis.SetPowered(powered);

                string symbol = GetTileSymbolForTile(tile);
                vis.SetSymbol(symbol);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Final success / fail / cancel
    // -------------------------------------------------------------------------

    private void FinalSuccessHack()
    {
        _active    = false;
        _completed = true;
        _phase = HackPhase.None;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pathGridRoot != null)
            pathGridRoot.SetActive(false);

        if (audioSource != null && successClip != null)
            audioSource.PlayOneShot(successClip);

        if (_gate != null)
            _gate.DisableForSeconds(_gate.hackDisableSeconds, "Terminal hack success");

          // Clear enemies guarding THIS terminal area (patrol + spawned drones)
        if (currentTerminal != null && currentTerminal.localWaveManager != null)
        {
            currentTerminal.localWaveManager.ForceClearAllEnemies(playDeath: true);
        }
        else
        {
            Debug.LogWarning("[HackMinigame] No localWaveManager assigned on currentTerminal.");
        }

        GameStateManager.Instance.p2TerminalCleared = true;
        //  Re-enable NPC following (Mila / Sakura)
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc != null)
        {
            var follow = npc.GetComponent<SakuraFollowPlayer>();
            if (follow != null)
                follow.SetFollowEnabled(true);
                
                // NPC reacts happily to successful hack

            npc.TriggerEvent("P2_HACK_SUCCESS");

        }

        // --- UI: force override ---
        if (ChoiceUIManager.Instance != null)
        {
            ChoiceUIManager.Instance.Show(
                "Successful hack! Proceed down the stairs.",
                ""
            );
        }
        GameRunLogger.Instance.PhaseComplete("Phase2", "HackSuccess");



        // Speak("[HACK] Override complete. Lasers are down—go!");

        DisableTerminalGuards();
        ClearWaveManagers();
        // NPCFollowController.ResumeFollowing();

    }

    private void ClearWaveManagers()
{
    if (waveManagersToClear == null || waveManagersToClear.Length == 0)
    {
        Debug.LogWarning("[HackMinigame] No wave managers assigned.");
        return;
    }

    foreach (DroneWaveManager wm in waveManagersToClear)
    {
        if (wm == null) continue;

        Debug.Log("[HackMinigame] Clearing wave manager: " + wm.name);

        wm.ForceClearAllEnemies(playDeath: true);
    }
        var guards = FindObjectsByType<GuardDronePatrol>(FindObjectsSortMode.None);
    foreach (var g in guards)
    {
        if (g != null && g.isActive)
            g.ForceRemoveNow();
    }

}

            private void DisableTerminalGuards()
    {
        if (guardsToDisable == null || guardsToDisable.Length == 0)
            return;

        foreach (var guard in guardsToDisable)
        {
            if (guard == null) continue;

            Debug.Log("[Terminal] Disabling patrol guard: " + guard.name);
            guard.ForceRemoveNow(); // OR guard.DisableGuard() if you want animation
        }
    }

void FailHack(string reason)
{
    _active = false;

    if (panelRoot != null)
        panelRoot.SetActive(false);

    // Find defense phase script more robustly
    if (currentTerminal != null)
    {
        var defense =
            currentTerminal.GetComponent<HackFailDefensePhase>() ??
            currentTerminal.GetComponentInParent<HackFailDefensePhase>() ??
            currentTerminal.GetComponentInChildren<HackFailDefensePhase>(true);

        if (defense != null)
        {
            defense.OnHackFailed(reason);
            return;
        }
    }

    // fallback
    Speak($"[HACK] Hack failed: {reason}. Press the terminal again to try another sequence.");
}



    void CancelHack()
    {
        _active = false;
        _phase = HackPhase.None;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pathGridRoot != null)
            pathGridRoot.SetActive(false);

        Speak("[HACK] Hack canceled.");
    }
}
