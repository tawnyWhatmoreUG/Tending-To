using UnityEngine;

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
/// </summary>
public abstract class BaseStageInteractable : MonoBehaviour
{
    [Tooltip("The stage this interactable belongs to. Used for safety checks.")]
    [SerializeField] protected Stage myStage;

    private bool _hasCompleted = false;

    // -------------------------------------------------------------------------
    // Public API (called by StageSequencer)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by StageSequencer when the player arrives at this stage's anchor.
    /// Enables interaction and runs any setup logic.
    /// </summary>
    public void Activate()
    {
        _hasCompleted = false;
        Debug.Log($"[{GetType().Name}] Activated for stage: {myStage}");
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
        GameManager.Instance?.ReportInteractionComplete();
    }
}
