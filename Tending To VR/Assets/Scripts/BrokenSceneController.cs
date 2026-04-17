using System;
using UnityEngine;

/// <summary>
/// Manages all visual state changes during the broken phase of Tending To.
///
/// TWO RESPONSIBILITIES:
///   1. MODEL SWAPS — fired once when the player first teleports to BrokenWindowBox.
///      Hides normal-world objects and activates their broken replacements all at once.
///      Accumulates — nothing is ever hidden again once the broken phase starts.
///
///   2. STAGE EFFECTS — fired per-stage when the player arrives at an anchor,
///      called directly from StageSequencer.OnPlayerArrivedAtAnchor.
///      These also accumulate — nothing is turned off once activated.
///
/// SETUP:
///   - Add to a single GameObject in the scene.
///   - modelSwapsRoot: parent GO whose children are all broken replacement models.
///     Leave disabled in the editor — this script manages its activation.
///   - objectsToHide: the normal-world objects the replacements are replacing.
///     These are disabled the same moment modelSwapsRoot is enabled.
///   - effectMappings: one entry per broken stage that has an arrival effect.
///     Safe to leave empty for stages that have no arrival effect.
///
/// SCENE HIERARCHY SUGGESTION:
///   BrokenStageVisuals/               (disabled by default)
///     ├── ModelSwaps/                 (activated all at once at BrokenWindowBox)
///     │   ├── FlowerBed_Broken/
///     │   ├── Hostas_Broken/
///     │   ├── DeadAnimals/
///     │   ├── WeeksBrowned/
///     │   └── GrassOversaturated/
///     └── Effects/                   (children activated per-stage on arrival)
///         ├── BeePathAnimation/
///         ├── BogWindowEffect/
///         ├── BlueMist/
///         ├── PavingEmission/
///         └── HandVeinEffect/
/// </summary>
public class BrokenSceneController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static BrokenSceneController Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Model Swaps — all fire at once when BrokenWindowBox begins")]
    [Tooltip("Parent GO containing all broken replacement models. " +
             "Leave disabled in the editor — activated automatically.")]
    [SerializeField] private GameObject modelSwapsRoot;

    [Tooltip("Normal-world objects to hide when broken models are revealed. " +
             "Disabled at the same moment modelSwapsRoot is enabled.")]
    [SerializeField] private GameObject[] objectsToHide;

    [Header("Per-stage Effects — fire on anchor arrival, accumulate")]
    [Tooltip("One entry per broken stage that has an arrival effect. " +
             "Stages with no effect need no entry here.")]
    [SerializeField] private StageEffectMapping[] effectMappings;

    [Tooltip("Desaturation effects to notify on anchor arrival (e.g. BrokenGrass lawn).")]

    [System.Serializable]
    public struct StageEffectMapping
    {
        [Tooltip("The broken stage this effect belongs to.")]
        public Stage stage;

        [Tooltip("GameObjects to activate when the player arrives at this stage. " +
                 "Particle systems, animators, shader-driven GOs, etc.")]
        public GameObject[] effectObjects;
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private bool _brokenPhaseStarted = false;

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
        Debug.Log("[BrokenSceneController] Awake — instance initialised."); 
        // Safety — ensure broken visuals are hidden on startup regardless
        // of how they are left in the editor.
        if (modelSwapsRoot != null)
            modelSwapsRoot.SetActive(false);
    }

    private void OnEnable() { }

    private void OnDisable() { }

    // -------------------------------------------------------------------------
    // Model Swaps — triggered by anchor arrival
    // -------------------------------------------------------------------------

    private void ActivateModelSwaps()
    {
        // Hide normal-world objects first.
        foreach (var obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Reveal all broken replacements.
        if (modelSwapsRoot != null)
            modelSwapsRoot.SetActive(true);

        Debug.Log("[BrokenSceneController] Model swaps activated.");
    }

    // -------------------------------------------------------------------------
    // Per-stage Effects — called directly from StageSequencer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called directly by StageSequencer.OnPlayerArrivedAtAnchor.
    /// The stage value is already resolved to the correct broken stage by
    /// TeleportAnchorController.GetCurrentActiveStage() before this fires,
    /// so shared anchors between normal and broken stages are handled correctly.
    /// Also handles the one-time model swap when the player first arrives at
    /// BrokenWindowBox. Intentionally silent for all other stages with no mapping.
    /// </summary>
    public void OnPlayerArrivedAtStage(Stage stage)
    {
        if (stage == Stage.BrokenWindowBox && !_brokenPhaseStarted)
        {
            _brokenPhaseStarted = true;
            ActivateModelSwaps();
        }

        foreach (var mapping in effectMappings)
        {
            if (mapping.stage == stage)
            {
                foreach (var fx in mapping.effectObjects)
                {
                    if (fx != null)
                        fx.SetActive(true);
                }
                Debug.Log($"[BrokenSceneController] Effects activated for stage: {stage}");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Editor Helpers
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: force the broken phase to activate immediately for testing.
    /// Call from a custom inspector button or the Unity console.
    /// </summary>
    public void DEBUG_ForceActivateBrokenPhase()
    {
        Debug.Log("[BrokenSceneController] DEBUG: Forcing broken phase activation.");
        _brokenPhaseStarted = true;
        ActivateModelSwaps();
    }
#endif
}