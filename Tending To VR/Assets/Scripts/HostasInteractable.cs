using UnityEngine;

/// <summary>
/// Stage interactable for SaveTheHostas.
/// Manages the slug pellet spreading interaction and reports completion to GameManager.
/// 
/// SETUP:
///   - Add this script to a GameObject in your scene (e.g., "HostasInteraction")
///   - Assign all SlugPelletBedController references in the Inspector (one per hosta bed)
///   - In StageSequencer's Interactable Mappings:
///       Stage: SaveTheHostas
///       Interactable: This GameObject
/// </summary>
public class HostasInteractable : BaseStageInteractable
{
    private void Awake()
    {
        myStage = Stage.SaveTheHostas;
    }

    [Header("References")]
    [Tooltip("Array of all hosta beds that need slug pellets (assign all beds in order).")]
    [SerializeField] private SlugPelletBedController[] hostaBeds;

    private int completedBeds = 0;

    protected override void OnActivated()
    {
        Debug.Log("[HostasInteractable] Hostas stage activated!");
        
        completedBeds = 0;
        
        if (hostaBeds == null || hostaBeds.Length == 0)
        {
            Debug.LogError("[HostasInteractable] No hosta beds assigned! Assign them in the Inspector.");
            return;
        }
        
        // Get reference to slug pellet controller to track when it's first equipped
        if (hostaBeds.Length > 0 && hostaBeds[0] != null)
        {
            SlugPelletController slugPelletController = hostaBeds[0].slugPelletController;
            if (slugPelletController != null)
            {
                slugPelletController.OnBottleSelected += OnSlugPelletEquipped;
            }
        }
        
        // Subscribe to completion events for all beds
        foreach (var bed in hostaBeds)
        {
            if (bed != null)
            {
                bed.OnBedComplete += OnBedCompleted;
            }
        }
    }

    protected override void OnCompleted()
    {
        Debug.Log("[HostasInteractable] All hostas saved!");
        
        // Unsubscribe from slug pellet selection
        if (hostaBeds != null && hostaBeds.Length > 0 && hostaBeds[0] != null)
        {
            SlugPelletController slugPelletController = hostaBeds[0].slugPelletController;
            if (slugPelletController != null)
            {
                slugPelletController.OnBottleSelected -= OnSlugPelletEquipped;
            }
        }
        
        // Unsubscribe from all beds
        if (hostaBeds != null)
        {
            foreach (var bed in hostaBeds)
            {
                if (bed != null)
                {
                    bed.OnBedComplete -= OnBedCompleted;
                }
            }
        }
    }

    private void OnSlugPelletEquipped()
    {
        Debug.Log("[HostasInteractable] Slug pellet bottle equipped! Signaling interaction start.");
        SignalInteractionStarted();
    }

    private void OnBedCompleted()
    {
        completedBeds++;
        Debug.Log($"[HostasInteractable] Bed completed! ({completedBeds}/{hostaBeds.Length})");
        
        // Check if all beds are done
        if (completedBeds >= hostaBeds.Length)
        {
            Debug.Log("[HostasInteractable] All hosta beds complete! Reporting to GameManager.");
            CompleteInteraction();
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (hostaBeds != null && hostaBeds.Length > 0 && hostaBeds[0] != null)
        {
            SlugPelletController slugPelletController = hostaBeds[0].slugPelletController;
            if (slugPelletController != null)
            {
                slugPelletController.OnBottleSelected -= OnSlugPelletEquipped;
            }
        }
        
        if (hostaBeds != null)
        {
            foreach (var bed in hostaBeds)
            {
                if (bed != null)
                {
                    bed.OnBedComplete -= OnBedCompleted;
                }
            }
        }
    }
}
