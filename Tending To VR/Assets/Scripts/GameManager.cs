using System;
using UnityEngine;

/// <summary>
/// All stages in Tending To, in order.
/// </summary>
public enum Stage
{
    PendingToDo,
    CutTheGrass,
    ClearThePath,
    SaveTheHostas,
    FeedTheFlowers,
    DoTheWindowBox,
    BrokenWindowBox,
    BrokenFlowers,
    BrokenHostas,
    BrokenPath,
    BrokenGrass,
    Relax
}

/// <summary>
/// Central singleton that owns stage state and coordinates all systems.
///
/// HOW TO USE:
///   - Subscribe to GameManager.OnStageChanged to react to stage transitions.
///   - Call GameManager.Instance.ReportInteractionComplete() from interactable scripts
///     when the player finishes the physical task for the current stage.
///   - Call GameManager.Instance.ReportVerseComplete() from PoemPlayer
///     when an audio verse finishes playing.
///   - Call GameManager.Instance.AdvanceStageDirect() only from PendingToDo
///     (the note pickup), which bypasses the dual-signal system.
///
/// SOUNDSCAPE FADE TIMING:
///   AudioManager no longer listens to OnStageChanged directly.
///   Instead, GameManager calls AudioManager.FadeOutLayer() at the moment
///   the interaction is confirmed complete for the current stage — before
///   the stage advances. This ensures the fade reflects task completion
///   rather than task start.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static GameManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired whenever the stage changes. All systems subscribe to this.
    /// </summary>
    public static event Action<Stage> OnStageChanged;

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("One StageData asset per stage, ordered to match the Stage enum exactly.")]
    [SerializeField] private StageData[] stageDataArray;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public Stage CurrentStage { get; private set; } = Stage.PendingToDo;

    private bool _interactionComplete = false;
    private bool _verseComplete = false;

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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Broadcast the initial stage so all subscribers set their starting state.
        OnStageChanged?.Invoke(CurrentStage);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the StageData asset for the current stage.
    /// </summary>
    public StageData CurrentStageData => GetStageData(CurrentStage);

    /// <summary>
    /// Returns the StageData asset for any given stage.
    /// </summary>
    public StageData GetStageData(Stage stage)
    {
        int index = (int)stage;
        if (stageDataArray == null || index >= stageDataArray.Length)
        {
            Debug.LogError($"[GameManager] No StageData found for stage {stage} (index {index}). " +
                           $"Check the stageDataArray in the Inspector.");
            return null;
        }
        return stageDataArray[index];
    }

    /// <summary>
    /// Called by the interactable system when the player completes the
    /// physical task for the current action stage.
    ///
    /// This is the trigger point for soundscape layer removal — the layer
    /// defined in the current StageData is faded out here, at the moment
    /// the player finishes the task, before the stage advances.
    /// </summary>
    public void ReportInteractionComplete()
    {
        if (_interactionComplete)
        {
            Debug.LogWarning("[GameManager] ReportInteractionComplete called but already marked complete.");
            return;
        }

        Debug.Log($"[GameManager] Interaction complete for stage: {CurrentStage}");
        _interactionComplete = true;

        // Fade out the soundscape layer associated with this stage now that
        // the interaction is done. AudioManager handles the fade gracefully
        // if the layer is already gone or the index is -1.
        TriggerSoundscapeFadeForCurrentStage();

        TryAdvanceStage();
    }

    /// <summary>
    /// Called by PoemPlayer when the verse audio clip for the current stage
    /// finishes playing.
    /// </summary>
    public void ReportVerseComplete()
    {
        if (_verseComplete)
        {
            Debug.LogWarning("[GameManager] ReportVerseComplete called but already marked complete.");
            return;
        }

        Debug.Log($"[GameManager] Verse complete for stage: {CurrentStage}");
        _verseComplete = true;
        TryAdvanceStage();
    }

    /// <summary>
    /// Bypasses the dual-signal system. Use only for PendingToDo (note pickup).
    /// </summary>
    public void AdvanceStageDirect()
    {
        Debug.Log($"[GameManager] Direct advance from stage: {CurrentStage}");
        AdvanceStage();
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the current stage's StageData and tells AudioManager to fade
    /// out the associated soundscape layer. Called exactly once per stage,
    /// at the moment the interaction is confirmed complete.
    /// </summary>
    private void TriggerSoundscapeFadeForCurrentStage()
    {
        StageData data = CurrentStageData;
        if (data == null) return;

        if (data.soundscapeLayerToRemove >= 0)
        {
            Debug.Log($"[GameManager] Triggering soundscape fade for layer {data.soundscapeLayerToRemove} " +
                      $"(stage: {CurrentStage})");
            AudioManager.Instance?.FadeOutLayer(data.soundscapeLayerToRemove);
        }
    }

    /// <summary>
    /// Checks completion conditions and advances the stage if met.
    /// Action stages: need both interaction + verse.
    /// Broken stages: need verse only.
    /// </summary>
    private void TryAdvanceStage()
    {
        StageData data = CurrentStageData;
        if (data == null) return;

        bool conditionsMet = data.requiresInteraction
            ? (_interactionComplete && _verseComplete)
            : _verseComplete;

        if (conditionsMet)
        {
            AdvanceStage();
        }
    }

    private void AdvanceStage()
    {
        // Reset completion flags for the next stage.
        _interactionComplete = false;
        _verseComplete = false;

        int nextIndex = (int)CurrentStage + 1;
        int maxIndex = Enum.GetValues(typeof(Stage)).Length - 1;

        if (nextIndex > maxIndex)
        {
            Debug.Log("[GameManager] Already at final stage. No further advancement.");
            return;
        }

        CurrentStage = (Stage)nextIndex;
        Debug.Log($"[GameManager] Stage advanced to: {CurrentStage}");
        OnStageChanged?.Invoke(CurrentStage);
    }

    // -------------------------------------------------------------------------
    // Editor Helpers
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: jump directly to any stage for testing.
    /// Call from a custom inspector button or the Unity console.
    /// AudioManagerDebugHelper handles restoring the soundscape to the
    /// correct state for the target stage.
    /// </summary>
    public void DEBUG_JumpToStage(Stage targetStage)
    {
        Debug.Log($"[GameManager] DEBUG jump: {CurrentStage} -> {targetStage}");
        _interactionComplete = false;
        _verseComplete = false;
        CurrentStage = targetStage;
        OnStageChanged?.Invoke(CurrentStage);
    }
#endif
}