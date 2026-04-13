using UnityEngine;

/// <summary>
/// Stage interactable for ClearThePath.
/// Manages the weedkiller/dandelion clearing interaction and reports completion to GameManager.
/// 
/// SETUP:
///   - Add this script to a GameObject in your scene (e.g., "PathInteraction")
///   - Assign the DandelionController reference in the Inspector
///   - In StageSequencer's Interactable Mappings:
///       Stage: ClearThePath
///       Interactable: This GameObject
/// </summary>
public class PathClearingInteractable : BaseStageInteractable
{
    private void Awake()
    {
        myStage = Stage.ClearThePath;
    }

    [Header("References")]
    [Tooltip("The DandelionController script that manages the weed spraying task.")]
    [SerializeField] private DandelionController dandelionController;

    protected override void OnActivated()
    {
        Debug.Log("[PathClearingInteractable] Path clearing stage activated!");
        
        if (dandelionController != null)
        {
            // Get reference to weedkiller controller to track when it's equipped
            WeedkillerController weedkillerController = dandelionController.weedkillScript;
            if (weedkillerController != null)
            {
                weedkillerController.OnNozzleSelected += OnWeedkillerEquipped;
            }
            
            // Subscribe to completion event
            dandelionController.OnPathCleared += OnPathCleared;
        }
        else
        {
            Debug.LogError("[PathClearingInteractable] DandelionController reference is null! Assign it in the Inspector.");
        }
    }

    protected override void OnCompleted()
    {
        Debug.Log("[PathClearingInteractable] Path cleared!");
        
        if (dandelionController != null)
        {
            WeedkillerController weedkillerController = dandelionController.weedkillScript;
            if (weedkillerController != null)
            {
                weedkillerController.OnNozzleSelected -= OnWeedkillerEquipped;
            }
            
            dandelionController.OnPathCleared -= OnPathCleared;
        }
    }

    private void OnWeedkillerEquipped()
    {
        Debug.Log("[PathClearingInteractable] Weedkiller equipped! Signaling interaction start.");
        SignalInteractionStarted();
    }

    private void OnPathCleared()
    {
        Debug.Log("[PathClearingInteractable] All dandelions cleared! Reporting to GameManager.");
        
        // Report to GameManager via base class
        CompleteInteraction();
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (dandelionController != null)
        {
            WeedkillerController weedkillerController = dandelionController.weedkillScript;
            if (weedkillerController != null)
            {
                weedkillerController.OnNozzleSelected -= OnWeedkillerEquipped;
            }
            
            dandelionController.OnPathCleared -= OnPathCleared;
        }
    }
}
