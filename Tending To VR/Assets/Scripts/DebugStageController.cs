#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>
/// EDITOR ONLY — stripped from builds entirely.
///
/// Provides two ways to jump between stages during Play Mode testing:
///
///   KEYBOARD SHORTCUTS (hold Left Shift):
///     Shift + ] (Right Bracket)  →  Advance one stage forward
///     Shift + [ (Left Bracket)   →  Go one stage backward
///     Shift + 0-9                →  Jump directly to that stage index
///
///   ON-SCREEN GUI PANEL:
///     Press Shift + D            →  Toggle the debug panel on/off
///     Panel shows current stage and one button per stage to jump directly.
///
/// SETUP:
///   Add this script to any GameObject in the scene.
///   It has no dependencies and will find GameManager.Instance automatically.
/// </summary>
public class DebugStageController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Behavior")]
    [Tooltip("If false, this entire script is disabled (no hotkeys, no panel).")]
    [SerializeField] private bool enableDebugScript = true;

    [Header("GUI Panel")]
    [SerializeField] private bool showPanelOnStart = true;
    [SerializeField] private int panelWidth = 280;
    [SerializeField] private int buttonHeight = 32;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool _showPanel;
    private Vector2 _scrollPosition;

    private readonly Stage[] _allStages = (Stage[])Enum.GetValues(typeof(Stage));

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        _showPanel = showPanelOnStart;
    }

    private void Update()
    {
        if (!enableDebugScript) return;

        if (!Input.GetKey(KeyCode.LeftShift)) return;

        // Toggle panel
        if (Input.GetKeyDown(KeyCode.D))
        {
            _showPanel = !_showPanel;
            return;
        }

        // Advance one stage
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            StepStage(1);
            return;
        }

        // Go back one stage
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            StepStage(-1);
            return;
        }

        // Jump to stage by index via number keys (0-9)
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                JumpToIndex(i);
                return;
            }
        }
    }

    // -------------------------------------------------------------------------
    // On-Screen GUI
    // -------------------------------------------------------------------------

    private void OnGUI()
    {
        if (!enableDebugScript) return;
        if (!_showPanel) return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            GUI.Label(new Rect(10, 10, 300, 20), "[DebugStageController] GameManager not found.");
            return;
        }

        int panelHeight = 60 + (_allStages.Length * (buttonHeight + 4)) + 20;
        Rect panelRect = new Rect(10, 10, panelWidth, panelHeight);

        // Background
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(14, 14, panelWidth - 8, panelHeight - 8));

        // Header
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            normal = { textColor = Color.yellow }
        };
        GUILayout.Label("TENDING TO  —  Stage Debug", headerStyle);

        // Current stage
        GUIStyle currentStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.cyan }
        };
        GUILayout.Label($"Current: [{(int)gm.CurrentStage}] {gm.CurrentStage}", currentStyle);
        GUILayout.Space(6);

        // One button per stage
        foreach (Stage stage in _allStages)
        {
            bool isCurrent = stage == gm.CurrentStage;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal,
                normal =
                {
                    textColor = isCurrent ? Color.black : Color.white,
                    background = isCurrent
                        ? MakeTex(1, 1, new Color(0.4f, 0.9f, 0.4f))
                        : MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f))
                }
            };

            string label = $"[{(int)stage}]  {stage}";
            if (GUILayout.Button(label, btnStyle, GUILayout.Height(buttonHeight)))
            {
                gm.DEBUG_JumpToStage(stage);
            }
        }

        // Keyboard hint
        GUILayout.Space(6);
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        GUILayout.Label("Shift+]  fwd    Shift+[  back    Shift+0-9  index", hintStyle);
        GUILayout.Label("Shift+D  toggle panel", hintStyle);

        GUILayout.EndArea();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void StepStage(int direction)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int current = (int)gm.CurrentStage;
        int next = Mathf.Clamp(current + direction, 0, _allStages.Length - 1);

        if (next != current)
            gm.DEBUG_JumpToStage((Stage)next);
    }

    private void JumpToIndex(int index)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (index < _allStages.Length)
            gm.DEBUG_JumpToStage((Stage)index);
        else
            Debug.LogWarning($"[DebugStageController] Stage index {index} out of range.");
    }

    /// <summary>
    /// Creates a 1x1 texture of a given colour for GUI button backgrounds.
    /// </summary>
    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
}
#endif
