using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Stage interactable for Relax.
/// The player ray-selects the wine glass via an XRSimpleInteractable — it snaps to their VR hand
/// (same pattern as SlugPelletController). After holding for holdDuration seconds, fires
/// OnWineGlassHeld to begin the credits transition.
///
/// SETUP:
///   - Add this script directly to the wine glass GameObject
///   - Also add an XRSimpleInteractable to the wine glass GameObject
///   - Add a Collider to the wine glass GameObject (no Rigidbody needed)
///   - Wire the XRSimpleInteractable's SelectEntered UnityEvent → WineGlassInteractable.OnSelected
///   - Add the XRSimpleInteractable to the inherited managedInteractables list
///     so it is disabled until the Relax stage becomes active
///   - Assign handRayInteractor and handLineVisual (the right hand's ray interactor)
///   - Optionally assign gripPoint for fine-tuned hold position
///   - In StageSequencer's Interactable Mappings:
///       Stage: Relax
///       Interactable: This GameObject
/// </summary>
public class WineGlassInteractable : BaseStageInteractable
{
    /// <summary>
    /// Fired when the player has held the wine glass for the required duration.
    /// CreditsTransitionController subscribes to this to begin the fade.
    /// </summary>
    public static event System.Action OnWineGlassHeld;

    private void Awake()
    {
        myStage = Stage.Relax;
    }

    [Header("VR Hand References")]
    [Tooltip("The XRRayInteractor on the hand that holds the glass. Disabled while holding, re-enabled after.")]
    public XRRayInteractor handRayInteractor;
    [Tooltip("The line visual on the same hand. Disabled while holding, re-enabled after.")]
    public XRInteractorLineVisual handLineVisual;

    [Header("Alignment")]
    [Tooltip("The child Transform that holds the wine glass mesh. This is what gets parented to the hand " +
             "(the XRSimpleInteractable must stay on the parent object, not move with the hand).")]
    public Transform glassRoot;
    [Tooltip("Optional child transform on the glass marking where the hand grip centre should be.")]
    public Transform gripPoint;

    [Header("Settings")]
    [Tooltip("How long the player must hold the wine glass before the credits transition begins.")]
    [SerializeField] private float holdDuration = 3f;

    private bool _isEquipped = false;
    private Coroutine _holdCoroutine;

    protected override void OnActivated()
    {
        Debug.Log("[WineGlassInteractable] Relax stage activated — waiting for player to pick up the wine glass.");
    }

    protected override void OnCompleted()
    {
        Debug.Log("[WineGlassInteractable] Wine glass held — credits transition beginning.");
    }

    /// <summary>
    /// Wire this to the wine glass XRSimpleInteractable's SelectEntered event in the Inspector.
    /// Snaps the glass to the player's hand and starts the hold timer.
    /// </summary>
    public void OnSelected(SelectEnterEventArgs args)
    {
        if (_isEquipped) return;

        Debug.Log($"[WineGlassInteractable] OnSelected fired. Interactor: {args.interactorObject?.transform.gameObject.name ?? "NULL"}");

        SignalInteractionStarted();

        // Disable the ray line while holding
        if (handRayInteractor != null) handRayInteractor.enabled = false;
        if (handLineVisual != null)    handLineVisual.enabled    = false;

        // Determine what to attach to the hand.
        // IMPORTANT: we must NOT parent the XRSimpleInteractable's own GameObject to the
        // interactor — XR Toolkit will cancel the selection when it detects the hierarchy
        // change. Instead we parent the child glassRoot (mesh only), exactly like
        // FertiliserController parents scoopRoot while the interactable stays on the bucket.
        Transform attachTarget = glassRoot != null ? glassRoot : transform;

        // Snap to hand
        attachTarget.SetParent(args.interactorObject.transform);
        attachTarget.localPosition = Vector3.zero;
        attachTarget.localRotation = Quaternion.identity;

        Debug.Log($"[WineGlassInteractable] Parented {attachTarget.name} to: {attachTarget.parent?.name ?? "NULL"}, world pos: {attachTarget.position}");

        if (gripPoint != null)
        {
            Vector3 offset = attachTarget.position - gripPoint.position;
            attachTarget.position += offset;
        }

        _isEquipped = true;
        _holdCoroutine = StartCoroutine(HoldTimer());
    }

    private IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(holdDuration);
        _holdCoroutine = null;
        OnWineGlassHeld?.Invoke();
        CompleteInteraction();

        // Re-enable the ray after the hold completes
        StartCoroutine(RestoreInteractorNextFrame());
    }

    private IEnumerator RestoreInteractorNextFrame()
    {
        yield return null;
        if (handRayInteractor != null) handRayInteractor.enabled = true;
        if (handLineVisual != null)    handLineVisual.enabled    = true;
    }
}
