using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem; // ADD THIS

public class WeedkillerController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when the weedkiller nozzle is picked up/selected.
    /// </summary>
    public event System.Action OnNozzleSelected;

    // -------------------------------------------------------------------------
    // Inspector References
    // -------------------------------------------------------------------------

    [Header("References")]
    public GameObject containerAndHose;
    public Transform nozzleVisuals;
    public ParticleSystem mistParticles;
    public AudioSource sprayAudioSource; // Drag an AudioSource component here
    
    [Header("VR Hand References")]
    // Drag your RightHand Controller (the object with the Ray) here
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor handRayInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual handLineVisual;

    [Header("Input")]
    public InputActionProperty triggerAction; // ADD THIS - Wire up the trigger input in Inspector

    [Header("Alignment")]
    public Transform gripPoint; // Drag your new "GripPoint" empty here
    public float nozzleRotationOffset = 180f; // Z-axis rotation when equipped

    public bool IsCurrentlyEquipped => isEquipped; // This lets other scripts "read" the value

    private bool isEquipped = false;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform originalParent;

    void Start() {
        originalPos = nozzleVisuals.localPosition;
        originalRot = nozzleVisuals.localRotation;
        originalParent = nozzleVisuals.parent;
        
        // Ensure particles are off at start
        mistParticles.Stop();
        var emission = mistParticles.emission;
        emission.enabled = false;
        
        // Ensure audio is stopped at start
        if (sprayAudioSource != null) {
            sprayAudioSource.Stop();
        }
    }

    // ADD THIS UPDATE METHOD
    void Update() {
        if (!isEquipped) return; // Only check trigger when equipped
        
        float triggerValue = triggerAction.action.ReadValue<float>();
        bool isTriggerPressed = triggerValue > 0.1f; // Threshold for trigger press
        
        ToggleSpray(isTriggerPressed);
    }

    // Assign this to 'Select Entered' in the XR Simple Interactable
    public void PickupNozzle(SelectEnterEventArgs args) {
        if (isEquipped) return;

        // Notify listeners that nozzle has been selected/grabbed
        OnNozzleSelected?.Invoke();

        // Disable Ray Visuals
        if (handRayInteractor != null) handRayInteractor.enabled = false;
        if (handLineVisual != null) handLineVisual.enabled = false;

        containerAndHose.SetActive(false);
        
        // 1. Parent the nozzle to the hand
        nozzleVisuals.SetParent(args.interactorObject.transform);

        // 2. Alignment Logic:
        // First set position and rotation relative to hand
        nozzleVisuals.localPosition = Vector3.zero;
        nozzleVisuals.localRotation = Quaternion.identity;
        
        // Adjust rotation so the nozzle aims forward
        // You might need to tweak this depending on your model's export rotation
        nozzleVisuals.forward = args.interactorObject.transform.forward;
        
        // Apply Z-axis rotation offset to correct nozzle orientation
        nozzleVisuals.Rotate(0, 0, nozzleRotationOffset, Space.Self);

        // Apply the offset based on where your GripPoint is relative to the wand mesh
        // Calculate this AFTER rotation so the offset is correct
        Vector3 offset = nozzleVisuals.position - gripPoint.position;
        nozzleVisuals.position += offset;

        isEquipped = true;
    }

    public void ResetTool() {
        // Bring the Ray back
        if (handRayInteractor != null) handRayInteractor.enabled = true;
        if (handLineVisual != null) handLineVisual.enabled = true;

        isEquipped = false;
        mistParticles.Stop();
        if (sprayAudioSource != null) sprayAudioSource.Stop();
        
        nozzleVisuals.SetParent(originalParent);
        nozzleVisuals.localPosition = originalPos;
        nozzleVisuals.localRotation = originalRot;
        
        containerAndHose.SetActive(true);
    }

    public void ToggleSpray(bool isSpraying) {
        // Safety: If not equipped, always stay off
        if (!isEquipped) {
            var emission = mistParticles.emission;
            emission.enabled = false;
            mistParticles.Stop();
            if (sprayAudioSource != null) sprayAudioSource.Stop();
            return;
        }

        var mainEmission = mistParticles.emission;
        mainEmission.enabled = isSpraying;

        if (isSpraying) {
            if (!mistParticles.isPlaying) mistParticles.Play();
            if (sprayAudioSource != null && !sprayAudioSource.isPlaying) {
                sprayAudioSource.Play();
            }
        } else {
            mistParticles.Stop(); // Force stops the jet immediately
            if (sprayAudioSource != null) {
                sprayAudioSource.Stop();
            }
        }
    }
}