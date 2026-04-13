using UnityEngine;

/// <summary>
/// Stage interactable for CutTheGrass.
/// The player clicks the mower to start it. The stage completes immediately on click
/// and the mower continues mowing autonomously.
/// 
/// SETUP:
///   - Add this script to a GameObject in your scene (e.g., "MowerInteraction")
///   - Assign the RobotMower reference in the Inspector
///   - On the mower's XR Interactable, wire SelectEntered (or Activated) → MowerInteractable.OnMowerClicked
///   - In StageSequencer's Interactable Mappings:
///       Stage: CutTheGrass
///       Interactable: This GameObject
/// </summary>
public class MowerInteractable : BaseStageInteractable
{
    private void Awake()
    {
        myStage = Stage.CutTheGrass;
    }

    [Header("References")]
    [Tooltip("The RobotMower script that controls the mowing behavior.")]
    [SerializeField] private RobotMower mower;

    [Tooltip("Optional: AudioSource for completion sound.")]
    [SerializeField] private AudioSource completionAudio;

    protected override void OnActivated()
    {
        Debug.Log("[MowerInteractable] Mower stage activated — waiting for player to click the mower.");

        if (mower == null)
            Debug.LogError("[MowerInteractable] RobotMower reference is null! Assign it in the Inspector.");
    }

    /// <summary>
    /// Wire this to the mower's XR Interactable SelectEntered (or Activated) event in the Inspector.
    /// Starts the mower and immediately marks the stage as complete so the player can move on.
    /// Signals interaction start before completion.
    /// </summary>
    public void OnMowerClicked()
    {
        // Signal that interaction has started (triggers poem playback)
        SignalInteractionStarted();

        if (mower != null)
            mower.StartMowing();

        if (completionAudio != null)
            completionAudio.Play();

        CompleteInteraction();
    }

    protected override void OnCompleted()
    {
        Debug.Log("[MowerInteractable] Mower started — stage complete, mower running autonomously.");
    }
}
