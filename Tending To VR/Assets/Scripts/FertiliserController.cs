using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Manages the fertiliser bucket interaction.
///
/// SETUP SUMMARY
/// =============
/// 1. Place this script on the root 'fertiliser' GameObject in the scene.
///
/// 2. In the Inspector, assign:
///    Bucket Parts
///      Lid             — the 'lid' child mesh GameObject
///      Scoop Root      — the '---scoop---' child Transform
///
///    Scoop Parts
///      Scoop Fertiliser Mesh — the 'scoopfertiliser' child GameObject (starts hidden)
///      Pour Particles        — a ParticleSystem on/near the scoop (granular pour effect)
///      Pour Duration         — seconds the pour effect lasts (default 1.5 s)
///
///    VR Hand
///      Hand Ray Interactor   — the XRRayInteractor on the right hand
///      Hand Line Visual      — the XRInteractorLineVisual on the right hand
///
///    Scoop Alignment
///      Scoop Grip Point        — empty child of ---scoop--- that marks the natural hold point
///      Scoop X Rotation Offset — X-axis tilt when equipped (default 70° for forward tilt)
///      Scoop Rotation Offset   — Z-axis tweak if the scoop faces the wrong way in hand
///
///    Pouring
///      Sparkle Particles     — a ParticleSystem placed on/above the soil (looping sparkle)
///
///    Completion
///      Completion Audio      — AudioSource with the success sound clip
///
/// 3. On the 'fertiliser' GameObject, add an XRSimpleInteractable.
///    In its "Select Entered (SelectEnterEventArgs)" UnityEvent, wire up:
///      Target  → this FertiliserController component
///      Method  → FertiliserController.OnFertiliserSelected
///
/// 4. Add a trigger collider to the scoop that can detect when it enters the flower bed zone.
///    The flower bed must have a trigger collider tagged "FlowerBed" that extends upward
///    in Y space to capture the scoop when positioned above the bed.
///
/// 5. Add the ScoopTriggerDetector script to the scoop GameObject and wire it to this controller.
///
/// 6. IMPORTANT: Add a Rigidbody component to the scoop (can be kinematic with Is Kinematic checked).
///    Unity requires at least one object in a trigger collision to have a Rigidbody.
///
/// INTERACTION FLOW
/// ================
///   Ray + Select  →  Lid hides, scoop snaps to hand and auto-fills with fertiliser
///   Move scoop into flower bed trigger zone and tilt upside down  →  pour particles play, mesh fades, sparkle on soil
///   Completion  →  sound plays, GameManager notified, everything resets
/// </summary>
public class FertiliserController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when the fertilising task is complete.
    /// </summary>
    public event Action OnFertilisingComplete;

    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Bucket Parts")]
    [Tooltip("The lid mesh object to hide when the interaction starts.")]
    public GameObject lid;

    [Tooltip("The '---scoop---' Transform — the whole scoop assembly.")]
    public Transform scoopRoot;

    [Header("Scoop Parts")]
    [Tooltip("The 'scoopfertiliser' child GameObject — hidden until the scoop is filled.")]
    public GameObject scoopFertiliserMesh;

    [Tooltip("Particle system on/near the scoop that plays during the pour (granular scatter effect).")]
    public ParticleSystem pourParticles;

    [Tooltip("Total duration (seconds) of the pour animation before the task completes.")]
    public float pourDuration = 1.5f;

    [Header("VR Hand")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor handRayInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual handLineVisual;

    [Header("Scoop Alignment")]
    [Tooltip("Empty child of ---scoop--- that marks the natural grip/hold point of the scoop.")]
    public Transform scoopGripPoint;

    [Tooltip("X-axis rotation (degrees) applied when equipped — use this to tilt the scoop forward/backward.")]
    public float scoopXRotationOffset = 70f;

    [Tooltip("Z-axis rotation (degrees) applied after parenting to the hand — tweak until the scoop feels right.")]
    public float scoopRotationOffset = 0f;

    [Header("Pouring")]
    [Tooltip("How upside down the scoop must be to pour. -1.0 = completely upside down, -0.5 = 60° tilt, 0.0 = horizontal (90° tilt). Lower values = stricter requirement.")]
    [Range(-1f, 0f)]
    public float upsideDownThreshold = -0.7f;

    [Tooltip("Looping sparkle/shimmer ParticleSystem placed on the soil that plays after fertiliser is applied.")]
    public ParticleSystem sparkleParticles;

    [Header("Completion")]
    [Tooltip("AudioSource containing the success/completion sound clip.")]
    public AudioSource completionAudio;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool scoopEquipped        = false;
    private bool isPouringOrComplete  = false;
    private bool isInBedZone          = false;

    private Vector3    scoopOriginalLocalPos;
    private Quaternion scoopOriginalLocalRot;
    private Transform  scoopOriginalParent;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        if (scoopRoot != null)
        {
            scoopOriginalLocalPos = scoopRoot.localPosition;
            scoopOriginalLocalRot = scoopRoot.localRotation;
            scoopOriginalParent   = scoopRoot.parent;
        }

        if (scoopFertiliserMesh != null) scoopFertiliserMesh.SetActive(false);
        if (pourParticles       != null) pourParticles.Stop();
        if (sparkleParticles    != null) sparkleParticles.Stop();
    }

    void Update()
    {
        if (!scoopEquipped || isPouringOrComplete) return;

        // Check if scoop is in flower bed zone and tilted upside down
        if (isInBedZone)
        {
            // Check tilt: scoop is upside down when its up vector points downward (negative Y)
            float tiltValue = scoopRoot.up.y;
            bool isTiltedUpsideDown = tiltValue < upsideDownThreshold;
            
            // Debug logging every frame when in bed zone
            Debug.Log($"Fertiliser: In bed zone. Tilt Y = {tiltValue:F3}, Threshold = {upsideDownThreshold:F3}, Upside down = {isTiltedUpsideDown}");
            
            if (isTiltedUpsideDown)
            {
                Debug.Log("Fertiliser: POUR TRIGGERED!");
                StartCoroutine(PourAndComplete());
            }
        }
    }

    // -------------------------------------------------------------------------
    // Step 1 — Select: lid hides, scoop snaps to hand and auto-fills
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wire this to the XRSimpleInteractable's "Select Entered (SelectEnterEventArgs)" event
    /// on the fertiliser bucket GameObject.
    /// </summary>
    public void OnFertiliserSelected(SelectEnterEventArgs args)
    {
        if (scoopEquipped || isPouringOrComplete) return;

        // Hide the bucket lid
        if (lid != null) lid.SetActive(false);

        // Disable the ray so the hand is 'holding' the scoop
        if (handRayInteractor != null) handRayInteractor.enabled = false;
        if (handLineVisual    != null) handLineVisual.enabled    = false;

        // Parent the scoop assembly to the interactor (hand) transform
        Transform handTransform = args.interactorObject.transform;
        scoopRoot.SetParent(handTransform);
        scoopRoot.localPosition = Vector3.zero;
        scoopRoot.localRotation = Quaternion.identity;

        // Align scoop forward to match hand forward direction
        scoopRoot.forward = handTransform.forward;

        // Apply rotation offsets to correct model orientation
        scoopRoot.Rotate(scoopXRotationOffset, 0f, 0f, Space.Self);
        scoopRoot.Rotate(0f, 0f, scoopRotationOffset, Space.Self);

        // Shift so the grip point aligns with the hand centre
        if (scoopGripPoint != null)
        {
            Vector3 offset = scoopRoot.position - scoopGripPoint.position;
            scoopRoot.position += offset;
        }

        scoopEquipped = true;

        // Auto-fill the scoop with fertiliser when equipped
        if (scoopFertiliserMesh != null) scoopFertiliserMesh.SetActive(true);
        
        Debug.Log("Fertiliser: Scoop equipped and filled");
    }

    // -------------------------------------------------------------------------
    // Bed Zone Detection (called by ScoopTriggerDetector on the scoop)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by ScoopTriggerDetector when the scoop enters a trigger collider.
    /// The flower bed must have a trigger collider tagged "FlowerBed" that extends
    /// upward in Y space to capture the scoop when positioned above the bed.
    /// </summary>
    public void OnScoopTriggerEnter(Collider other)
    {
        Debug.Log($"Fertiliser: OnScoopTriggerEnter with '{other.gameObject.name}' (tag: '{other.tag}')");
        
        if (other.CompareTag("FlowerBed"))
        {
            isInBedZone = true;
            Debug.Log("Fertiliser: ENTERED flower bed zone!");
        }
    }

    /// <summary>
    /// Called by ScoopTriggerDetector when the scoop exits a trigger collider.
    /// </summary>
    public void OnScoopTriggerExit(Collider other)
    {
        Debug.Log($"Fertiliser: OnScoopTriggerExit with '{other.gameObject.name}' (tag: '{other.tag}')");
        
        if (other.CompareTag("FlowerBed"))
        {
            isInBedZone = false;
            Debug.Log("Fertiliser: EXITED flower bed zone!");
        }
    }

    // -------------------------------------------------------------------------
    // Step 2 — Pour & Complete: scoop tilted upside down in bed zone
    // -------------------------------------------------------------------------

    private IEnumerator PourAndComplete()
    {
        isPouringOrComplete = true;

        // Start the granular pour particle effect on the scoop
        if (pourParticles != null) pourParticles.Play();

        // Halfway through the pour, hide the fertiliser mesh (it's "fallen out")
        yield return new WaitForSeconds(pourDuration * 0.5f);
        if (scoopFertiliserMesh != null) scoopFertiliserMesh.SetActive(false);

        // Finish the pour duration then stop particles
        yield return new WaitForSeconds(pourDuration * 0.5f);
        if (pourParticles != null) pourParticles.Stop();

        // Sparkle on the soil to show fertiliser has been applied
        if (sparkleParticles != null) sparkleParticles.Play();

        // Play completion sound
        if (completionAudio != null) completionAudio.Play();

        // Notify listeners that fertilising is complete
        OnFertilisingComplete?.Invoke();

        // Wait for the audio clip to finish before resetting the scene
        float waitTime = (completionAudio != null && completionAudio.clip != null)
            ? completionAudio.clip.length
            : 2f;
        yield return new WaitForSeconds(waitTime);

        ResetAll();
    }

    // -------------------------------------------------------------------------
    // Reset — everything returns to its original state
    // -------------------------------------------------------------------------

    private void ResetAll()
    {
        // Restore the ray interactor
        if (handRayInteractor != null) handRayInteractor.enabled = true;
        if (handLineVisual    != null) handLineVisual.enabled    = true;

        // Return scoop to its original world position and parent
        if (scoopRoot != null)
        {
            scoopRoot.SetParent(scoopOriginalParent);
            scoopRoot.localPosition = scoopOriginalLocalPos;
            scoopRoot.localRotation = scoopOriginalLocalRot;
        }

        // Restore the lid
        if (lid != null) lid.SetActive(true);

        // Clean up meshes and particles
        if (scoopFertiliserMesh != null) scoopFertiliserMesh.SetActive(false);
        if (pourParticles       != null) pourParticles.Stop();
        // Leave sparkleParticles running — they stay on the soil to show it's been fertilised

        scoopEquipped       = false;
        isPouringOrComplete = false;
        isInBedZone         = false;
    }
}
