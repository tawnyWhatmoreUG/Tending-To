using UnityEngine;


/// <summary>
/// Attach to a GameObject that also has an XRI TeleportationAnchor component.
///
/// RESPONSIBILITIES:
///   - Activates/deactivates itself by listening to GameManager.OnStageChanged.
///   - When the player teleports to this anchor, begins the stage sequence:
///       1. Disables its own glow/collider immediately so it can't be re-triggered.
///       2. Notifies StageSequencer to begin the interaction for this stage.
///
/// SCENE SETUP PER ANCHOR:
///   - Add this script alongside an XRI TeleportationAnchor component.
///   - Set 'myStage' to the Stage enum value this anchor belongs to.
///   - Assign 'anchorVisual' to the child GameObject that holds the glow shader/mesh.
///   - The TeleportationAnchor's Teleporting event should point to nothing —
///     this script hooks it in Awake via code.
///
/// HIERARCHY EXAMPLE:
///   Anchor_CutTheGrass
///     ├── TeleportationAnchor (XRI built-in)
///     ├── TeleportAnchorController (this script)
///     └── AnchorVisual (glowing mesh / shader)
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor))]
public class TeleportAnchorController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("The stage during which this anchor should be active and glowing.")]
    [SerializeField] private Stage myStage;

    [Tooltip("Optional: a second stage that reactivates this same anchor (e.g. BrokenGrass reuses CutTheGrass anchor).")]
    [SerializeField] private bool hasSecondStage = false;
    [SerializeField] private Stage mySecondStage;

    [Tooltip("The child GameObject containing the glow shader/mesh. " +
             "Enabled when active, disabled after teleport.")]
    [SerializeField] private GameObject anchorVisual;

    // -------------------------------------------------------------------------
    // Private refs
    // -------------------------------------------------------------------------

    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor _teleportationAnchor;
    private Collider _collider;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _teleportationAnchor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor>();
        _collider = GetComponent<Collider>();

        // Hook into XRI's teleport event so we know when the player arrives.
        _teleportationAnchor.teleporting.AddListener(OnPlayerTeleported);
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    private void OnDestroy()
    {
        if (_teleportationAnchor != null)
            _teleportationAnchor.teleporting.RemoveListener(OnPlayerTeleported);
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

private void OnStageChanged(Stage newStage)
{
    bool isMyStage = newStage == myStage
        || (hasSecondStage && newStage == mySecondStage);

    SetAnchorActive(isMyStage);
}

    // -------------------------------------------------------------------------
    // Teleport Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by XRI's TeleportationAnchor.teleporting event the moment
    /// the player is teleported to this anchor's position.
    /// </summary>
    private void OnPlayerTeleported(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportingEventArgs args)
    {
        Debug.Log($"[TeleportAnchorController] Player teleported to anchor: {myStage}");

        // Immediately hide the glow and disable interaction so it
        // can't be triggered again this stage.
        SetAnchorActive(false);

        // Tell the StageSequencer the player has arrived and
        // the interaction phase for this stage can begin.
        StageSequencer.Instance?.OnPlayerArrivedAtAnchor(myStage);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetAnchorActive(bool active)
    {
        // Show/hide the glow visual.
        if (anchorVisual != null)
            anchorVisual.SetActive(active);

        // Enable/disable the collider so the ray interactor
        // can only hit it when it should be active.
        if (_collider != null)
            _collider.enabled = active;

        // Enable/disable XRI interaction.
        _teleportationAnchor.enabled = active;
    }
}
