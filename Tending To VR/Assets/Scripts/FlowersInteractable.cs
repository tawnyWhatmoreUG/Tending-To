using UnityEngine;

/// <summary>
/// Stage interactable for FeedTheFlowers.
/// The FertiliserController already handles completion and calls GameManager.ReportInteractionComplete().
/// This wrapper just activates the stage and reports completion properly through BaseStageInteractable.
/// 
/// SETUP:
///   - Add this script to a GameObject in your scene (e.g., "FlowersInteraction")
///   - Assign the FertiliserController reference in the Inspector
///   - In StageSequencer's Interactable Mappings:
///       Stage: FeedTheFlowers
///       Interactable: This GameObject
/// </summary>
public class FlowersInteractable : BaseStageInteractable
{
    private void Awake()
    {
        myStage = Stage.FeedTheFlowers;
    }

    [Header("References")]
    [Tooltip("The FertiliserController script that manages the fertilising task.")]
    [SerializeField] private FertiliserController fertiliserController;

    protected override void OnActivated()
    {
        Debug.Log("[FlowersInteractable] Flowers stage activated!");
        
        if (fertiliserController != null)
        {
            // Subscribe to completion event
            fertiliserController.OnFertilisingComplete += OnFlowersFed;
        }
        else
        {
            Debug.LogError("[FlowersInteractable] FertiliserController reference is null! Assign it in the Inspector.");
        }
    }

    protected override void OnCompleted()
    {
        Debug.Log("[FlowersInteractable] Flowers fed!");
        
        if (fertiliserController != null)
        {
            fertiliserController.OnFertilisingComplete -= OnFlowersFed;
        }
    }

    private void OnFlowersFed()
    {
        Debug.Log("[FlowersInteractable] Fertilising complete! Reporting to GameManager.");
        
        // Report to GameManager via base class
        CompleteInteraction();
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (fertiliserController != null)
        {
            fertiliserController.OnFertilisingComplete -= OnFlowersFed;
        }
    }
}
