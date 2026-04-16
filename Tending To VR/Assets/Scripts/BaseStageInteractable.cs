using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Abstract base class for all stage interactables (Mower, Weedkiller, etc.).
///
/// Each concrete stage interactable (e.g. MowerInteractable, WeedkillerInteractable)
/// inherits from this and implements:
///   - OnActivated()  — set up the interactable when the player arrives
///   - OnCompleted()  — clean up and report back to GameManager
///
/// USAGE:
///   When the player finishes the task, the subclass calls CompleteInteraction(),
///   which notifies GameManager automatically.
///
/// INTERACTION START SIGNAL:
///   When the player actually begins interacting (e.g., grabs object, clicks button),
///   the subclass calls SignalInteractionStarted() to notify listeners (e.g., StageSequencer)
///   that interaction has begun. This is separate from Activate() which is setup only.
/// </summary>
public abstract class BaseStageInteractable : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when the player actually begins interacting with this stage.
    /// Stage is passed as parameter for identification.
    /// </summary>
    public event System.Action<Stage> OnInteractionStarted;

    // -------------------------------------------------------------------------
    // Inspector & State
    // -------------------------------------------------------------------------

    [Tooltip("The stage this interactable belongs to. Used for safety checks.")]
    [SerializeField] protected Stage myStage;

    [Tooltip("XRSimpleInteractable components that should only be raycast-selectable while this stage is active. " +
             "Disabled at Start, enabled on Activate(), disabled again on CompleteInteraction(). " +
             "Must be set to Enabled in the Inspector initially — the script controls state at runtime.")]
    [SerializeField] private XRBaseInteractable[] managedInteractables;

    private bool _hasCompleted = false;
    private bool _interactionStarted = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    protected virtual void Start()
    {
        // Start all managed interactables disabled so they can't be ray-selected
        // until the player arrives at this stage's anchor.
        SetManagedInteractablesEnabled(false);
    }

    // -------------------------------------------------------------------------
    // Public API (called by StageSequencer)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by StageSequencer when the player arrives at this stage's anchor.
    /// Enables interaction and runs any setup logic.
    /// Does NOT trigger the OnInteractionStarted event; that comes when the player
    /// actually interacts (e.g., grabs object or clicks button).
    /// </summary>
    public void Activate()
    {
        _hasCompleted = false;
        _interactionStarted = false;
        Debug.Log($"[{GetType().Name}] Activated for stage: {myStage}");
        SetManagedInteractablesEnabled(true);
        OnActivated();
    }

    // -------------------------------------------------------------------------
    // Abstract — implement in each stage's interactable subclass
    // -------------------------------------------------------------------------

    /// <summary>
    /// Override to set up this stage's interaction (enable colliders,
    /// show objects, start listening for input, etc.)
    /// </summary>
    protected abstract void OnActivated();

    /// <summary>
    /// Override to clean up after the task is done (remove objects from
    /// player hands, reset positions, disable colliders, etc.)
    /// </summary>
    protected abstract void OnCompleted();

    // -------------------------------------------------------------------------
    // Protected — call this from subclass when task is finished
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this from the subclass once the player has finished the task.
    /// Runs cleanup and reports to GameManager.
    /// </summary>
    protected void CompleteInteraction()
    {
        if (_hasCompleted)
        {
            Debug.LogWarning($"[{GetType().Name}] CompleteInteraction called more than once. Ignoring.");
            return;
        }

        _hasCompleted = true;
        Debug.Log($"[{GetType().Name}] Interaction complete for stage: {myStage}");

        OnCompleted();
        SetManagedInteractablesEnabled(false);
        GameManager.Instance?.ReportInteractionComplete();
    }

    /// <summary>
    /// Call this from the subclass when the player actually begins interacting
    /// with this stage (e.g., when they grab an object, click a button, start using a tool).
    /// This is separate from Activate() which is just setup.
    /// Signals once and prevents duplicate events.
    /// </summary>
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetManagedInteractablesEnabled(bool state)
    {
        if (managedInteractables == null) return;
        foreach (var interactable in managedInteractables)
        {
            if (interactable != null)
                interactable.enabled = state;
        }
    }

    protected void SignalInteractionStarted()
    {
        if (_interactionStarted)
            return; // Already signaled, ignore duplicates

        _interactionStarted = true;
        Debug.Log($"[{GetType().Name}] Interaction started for stage: {myStage}");
        OnInteractionStarted?.Invoke(myStage);
    }
}
