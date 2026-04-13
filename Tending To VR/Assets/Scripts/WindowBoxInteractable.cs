using UnityEngine;

/// <summary>
/// Stage interactable for DoTheWindowBox.
/// The WindowBoxPlantingController already handles completion and calls GameManager.ReportInteractionComplete().
/// This wrapper activates the stage and ensures proper integration with BaseStageInteractable.
/// 
/// SETUP:
///   - Add this script to a GameObject in your scene (e.g., "WindowBoxInteraction")
///   - Assign the WindowBoxPlantingController reference in the Inspector
///   - In StageSequencer's Interactable Mappings:
///       Stage: DoTheWindowBox
///       Interactable: This GameObject
/// </summary>
public class WindowBoxInteractable : BaseStageInteractable
{
    private void Awake()
    {
        myStage = Stage.DoTheWindowBox;
    }

    [Header("References")]
    [Tooltip("The WindowBoxPlantingController script that manages the planting task.")]
    [SerializeField] private WindowBoxPlantingController plantingController;

    protected override void OnActivated()
    {
        Debug.Log("[WindowBoxInteractable] Window box stage activated!");
        
        if (plantingController != null)
        {
            // Subscribe to first plant selection event to signal interaction start
            plantingController.OnFirstPlantSelected += OnFirstPlantGrabbed;
            
            // Subscribe to completion event
            plantingController.OnPlantingComplete += OnWindowBoxComplete;
        }
        else
        {
            Debug.LogError("[WindowBoxInteractable] WindowBoxPlantingController reference is null! Assign it in the Inspector.");
        }
    }

    protected override void OnCompleted()
    {
        Debug.Log("[WindowBoxInteractable] Window box task complete!");
        
        if (plantingController != null)
        {
            plantingController.OnFirstPlantSelected -= OnFirstPlantGrabbed;
            plantingController.OnPlantingComplete -= OnWindowBoxComplete;
        }
    }

    private void OnFirstPlantGrabbed()
    {
        Debug.Log("[WindowBoxInteractable] First plant grabbed! Signaling interaction start.");
        SignalInteractionStarted();
    }

    private void OnWindowBoxComplete()
    {
        Debug.Log("[WindowBoxInteractable] Window box planted and moved! Reporting to GameManager.");
        
        // Report to GameManager via base class
        CompleteInteraction();
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (plantingController != null)
        {
            plantingController.OnFirstPlantSelected -= OnFirstPlantGrabbed;
            plantingController.OnPlantingComplete -= OnWindowBoxComplete;
        }
    }
}
