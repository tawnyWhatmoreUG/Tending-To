using UnityEngine;

/// <summary>
/// Sits between TeleportAnchorController and the rest of the stage systems.
/// When the player arrives at an anchor, StageSequencer activates the correct
/// interactable for that stage and triggers poem playback.
///
/// SETUP:
///   - Add to a single GameObject in the scene (alongside GameManager or its own GO).
///   - Assign one BaseStageInteractable per action stage in the Inspector array.
///     Broken stages and PendingToDo do not need entries here.
///
/// DESIGN NOTE:
///   StageSequencer does not advance the stage itself. That remains
///   GameManager's responsibility via ReportInteractionComplete()
///   and ReportVerseComplete().
/// </summary>
public class StageSequencer : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static StageSequencer Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("One entry per action stage that has a physical interaction. " +
             "Each element maps a stage to its interactable controller.")]
    [SerializeField] private StageInteractableMapping[] interactableMappings;

    [System.Serializable]
    public struct StageInteractableMapping
    {
        public Stage stage;
        public BaseStageInteractable interactable;
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
    /// Called by TeleportAnchorController when the player arrives at an anchor.
    /// Activates the interactable for that stage and starts the poem.
    /// </summary>
    public void OnPlayerArrivedAtAnchor(Stage stage)
    {
        Debug.Log($"[StageSequencer] Player arrived at anchor for stage: {stage}");

        // Activate this stage's interactable (if it has one).
        BaseStageInteractable interactable = GetInteractableForStage(stage);
        if (interactable != null)
        {
            interactable.Activate();
        }

        // Start the poem verse for this stage.
       // PoemPlayer.Instance?.PlayVerseForStage(stage);
    }

    // -------------------------------------------------------------------------
    // Stage Change — handles Broken stages (no anchor arrival, verse-only)
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        StageData data = GameManager.Instance.GetStageData(newStage);
        if (data == null) return;

        // Broken stages have no interactable and no anchor click —
        // the poem starts automatically when the stage begins.
        if (!data.requiresInteraction && data.poemVerse != null)
        {
           // PoemPlayer.Instance?.PlayVerseForStage(newStage);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private BaseStageInteractable GetInteractableForStage(Stage stage)
    {
        foreach (var mapping in interactableMappings)
        {
            if (mapping.stage == stage)
                return mapping.interactable;
        }

        Debug.LogWarning($"[StageSequencer] No interactable mapping found for stage: {stage}");
        return null;
    }
}
