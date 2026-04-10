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
///      Bucket Fertiliser Target — the 'bucket fertiliser' child Transform (proximity centre for filling)
///      Bucket Fill Radius       — how close the scoop must be to fill (default 0.2 m)
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
///      Pour Tilt Threshold   — how much the scoop must be tilted to pour (default 0.5 = ~45°)
///      Min Pour Height       — minimum world Y position to pour (default 1.5, ensures above soil)
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
/// INTERACTION FLOW
/// ================
///   Ray + Select  →  Lid hides, scoop snaps to hand
///   Scoop into bucket  →  scoopfertiliser mesh appears (filled)
///   Hold scoop above soil (Y > 1.5) and tilt downward  →  pour particles play, mesh fades, sparkle on soil
///   Completion  →  sound plays, GameManager notified, everything resets
/// </summary>
public class FertiliserController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Bucket Parts")]
    [Tooltip("The lid mesh object to hide when the interaction starts.")]
    public GameObject lid;

    [Tooltip("The '---scoop---' Transform — the whole scoop assembly.")]
    public Transform scoopRoot;

    [Tooltip("The 'bucket fertiliser' child Transform — used as the proximity centre for filling.")]
    public Transform bucketFertiliserTarget;

    [Tooltip("How close (metres) the scoop must be to the bucket fertiliser to fill up.")]
    public float bucketFillRadius = 0.2f;

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
    [Tooltip("The scoop must be tilted forward/downward this much to trigger the pour. Lower = more tilt required. 0.0 = completely inverted, -0.3 = tilted forward ~70°, -0.5 = ~60° forward.")]
    [Range(-1f, 0.5f)]
    public float pourTiltThreshold = -0.3f;

    [Tooltip("Minimum world Y position the scoop must be at to pour (ensures it's above the soil level).")]
    public float minPourHeight = 1.5f;

    [Tooltip("How long (seconds) the scoop must be held in pour position before pouring starts. Prevents accidental pours.")]
    public float sustainedTiltDuration = 0.4f;

    [Tooltip("Looping sparkle/shimmer ParticleSystem placed on the soil that plays after fertiliser is applied.")]
    public ParticleSystem sparkleParticles;

    [Header("Completion")]
    [Tooltip("AudioSource containing the success/completion sound clip.")]
    public AudioSource completionAudio;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool scoopEquipped        = false;
    private bool scoopFilled          = false;
    private bool isPouringOrComplete  = false;

    private float tiltTimer           = 0f;

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

        if (!scoopFilled)
        {
            // Step 2: dip scoop into bucket fertiliser to fill it
            if (bucketFertiliserTarget != null &&
                Vector3.Distance(scoopRoot.position, bucketFertiliserTarget.position) < bucketFillRadius)
            {
                FillScoop();
            }
        }
        else
        {
            // Step 3: tilt the filled scoop to pour
            // Check if the scoop is tilted downward intentionally AND above the minimum height
            
            // More strict tilt check: scoop must be tilted forward/downward
            // Check both up.y (should be negative or very small) AND forward.y (should be negative = pointing down)
            bool isTiltedDown = scoopRoot.up.y < pourTiltThreshold && scoopRoot.forward.y < -0.2f;
            bool isAboveSoil = scoopRoot.position.y >= minPourHeight;
            
            if (isTiltedDown && isAboveSoil)
            {
                // Accumulate time in pour position
                tiltTimer += Time.deltaTime;
                
                // Only pour after sustained tilt
                if (tiltTimer >= sustainedTiltDuration)
                {
                    StartCoroutine(PourAndComplete());
                }
            }
            else
            {
                // Reset timer if not in pour position
                tiltTimer = 0f;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Step 1 — Select: lid hides, scoop snaps to hand
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
    }

    // -------------------------------------------------------------------------
    // Step 2 — Fill: scoop enters bucket proximity
    // -------------------------------------------------------------------------

    private void FillScoop()
    {
        scoopFilled = true;
        if (scoopFertiliserMesh != null) scoopFertiliserMesh.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Step 3 — Pour & Complete: scoop tilted downward
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

        // Notify the stage system
        if (GameManager.Instance != null) GameManager.Instance.ReportInteractionComplete();

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
        scoopFilled         = false;
        isPouringOrComplete = false;
        tiltTimer           = 0f;
    }
}
