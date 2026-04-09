using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Wrist Menu canvas — the todo list on the player's left hand.
///
/// RESPONSIBILITIES:
///   - Hides the canvas on Start (shown only after PendingToDo note is picked up).
///   - Listens to GameManager.OnStageChanged and reveals the correct scribble sprite.
///   - Exposes ShowCanvas() for NoteOnTable to call when the note is picked up.
///
/// SPRITE SHEET SETUP (do this before wiring the Inspector):
///   1. Select your scribble PNG in the Project window.
///   2. In the Inspector set Texture Type → Sprite (2D and UI).
///   3. Set Sprite Mode → Multiple.
///   4. Open Sprite Editor → Slice → Grid By Cell Count, 
///      set columns to however many are in your sheet (e.g. 6 in a row).
///   5. Apply. Unity will generate 6 named sprite assets as children of the PNG.
///   6. Assign each generated sprite to the matching slot in the scribbleEntries
///      array below, alongside its corresponding Stage.
///
/// INSPECTOR SETUP:
///   - wristMenuRoot    → the 'Wrist Menu' XR Canvas GameObject
///   - scribbleEntries  → one entry per task stage:
///       Stage: CutTheGrass   Scribble Image: Image_Cut_Grass   Sprite: scribble_0
///       Stage: ClearThePath  Scribble Image: Image_Path        Sprite: scribble_1
///       ... etc.
///
/// HIERARCHY (expected structure under Wrist Menu):
///   Wrist Menu (Canvas)
///     └── PaperBG (Image)
///           ├── Text_Cut_Grass   (TextMeshPro)
///           ├── Image_Cut_Grass  (Image — scribble overlay, starts inactive)
///           ├── Text_Path        (TextMeshPro)
///           ├── Image_Path       (Image — scribble overlay, starts inactive)
///           └── ... (repeat for each task)
///
/// Add a transparent UI Image as a sibling or child of each TextMesh label
/// to act as the scribble overlay. Size and position it over the text in the
/// editor. Assign those Image components to the scribbleEntries below.
/// </summary>
public class WristCanvas : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static WristCanvas Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Canvas Root")]
    [Tooltip("The 'Wrist Menu' XR Canvas GameObject. Hidden until note is picked up.")]
    [SerializeField] private GameObject wristMenuRoot;

    [Header("Scribble Entries")]
    [Tooltip("One entry per task stage. Order does not matter — matched by Stage enum.")]
    [SerializeField] private ScribbleEntry[] scribbleEntries;

    [System.Serializable]
    public struct ScribbleEntry
    {
        [Tooltip("The stage that triggers this scribble.")]
        public Stage stage;

        [Tooltip("The UI Image component positioned over the task text label.")]
        public Image scribbleImage;
    }

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hide the canvas at start — shown when note is picked up in PendingToDo.
        if (wristMenuRoot != null)
            wristMenuRoot.SetActive(false);

        // Ensure all scribble images start hidden.
        foreach (var entry in scribbleEntries)
        {
            if (entry.scribbleImage != null)
            {
                entry.scribbleImage.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by NoteOnTable when the player picks up the note in PendingToDo.
    /// Reveals the Wrist Menu canvas.
    /// </summary>
    public void ShowCanvas()
    {
        if (wristMenuRoot != null)
        {
            wristMenuRoot.SetActive(true);
            Debug.Log("[WristCanvas] Canvas revealed.");
        }
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        StageData data = GameManager.Instance?.GetStageData(newStage);
        if (data == null) return;

        // -1 means no scribble change on this stage.
        if (data.wristCanvasScribbleIndex < 0) return;

        // Find the entry whose stage matches the PREVIOUS stage —
        // we mark off the task that was just completed, not the one starting.
        // The completed stage is one step behind newStage.
        Stage completedStage = (Stage)((int)newStage - 1);
        RevealScribbleForStage(completedStage);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RevealScribbleForStage(Stage completedStage)
    {
        foreach (var entry in scribbleEntries)
        {
            if (entry.stage == completedStage)
            {
                if (entry.scribbleImage != null)
                {
                    entry.scribbleImage.enabled = true;
                    Debug.Log($"[WristCanvas] Scribble revealed for stage: {completedStage}");
                }
                else
                {
                    Debug.LogWarning($"[WristCanvas] Scribble image is null for stage: {completedStage}");
                }
                return;
            }
        }

        Debug.LogWarning($"[WristCanvas] No scribble entry found for completed stage: {completedStage}");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Debug: reveals all scribbles instantly. Useful for checking layout.
    /// Call from the Unity console: 
    ///   FindObjectOfType{WristCanvas}().DEBUG_RevealAll()
    /// </summary>
    public void DEBUG_RevealAll()
    {
        ShowCanvas();
        foreach (var entry in scribbleEntries)
        {
            if (entry.scribbleImage != null)
                entry.scribbleImage.enabled = true;
        }
        Debug.Log("[WristCanvas] DEBUG: All scribbles revealed.");
    }

    /// <summary>
    /// Debug: hides all scribbles and the canvas. Resets to initial state.
    /// </summary>
    public void DEBUG_ResetAll()
    {
        if (wristMenuRoot != null)
            wristMenuRoot.SetActive(false);

        foreach (var entry in scribbleEntries)
        {
            if (entry.scribbleImage != null)
                entry.scribbleImage.enabled = false;
        }
        Debug.Log("[WristCanvas] DEBUG: Reset to initial state.");
    }
#endif
}
