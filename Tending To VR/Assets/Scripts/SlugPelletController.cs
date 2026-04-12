using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SlugPelletController : MonoBehaviour
{
    [Header("References")]
    public GameObject capModel;
    public Transform sprayAnchor;       // Empty at the bottle mouth — pellets spawn here
    public GameObject blueCapsulePrefab;
    public AudioSource sprayAudioSource;

    [Header("Effects")]
    public ParticleSystem pelletParticles;  // Assign the particle system on the bottle mouth

    [Header("Spawn Settings")]
    public float spawnInterval = 0.1f;  // Seconds between each pellet spawn
    public float spawnSpeed = 3f;       // How fast pellets shoot out
    public float spawnSpread = 30f;     // Cone spread angle in degrees

    [Header("VR Hand References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor handRayInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual handLineVisual;

    [Header("Spray Settings")]
    [Range(0f, 1f)]
    public float upsideDownThreshold = 0.5f;    // transform.up.y must be below -this to count as upside-down
    public float shakeVelocityThreshold = 1.5f; // World-space speed (m/s) required to trigger a spawn

    [Header("Alignment")]
    public Transform gripPoint;             // Where the bottle should be held in the hand
    public float bottleRotationOffset = 0f; // Adjust if needed for correct orientation

    [Header("Audio Timing")]
    public float minSprayDuration = 0.5f;   // Minimum time spray audio plays before it can stop (seconds)

    public bool IsCurrentlyEquipped => isEquipped;
    public bool IsSpraying => isSpraying;

    private bool isEquipped = false;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform originalParent;
    private bool capWasActive;
    private float nextSpawnTime;
    private bool isSpraying;
    private float sprayStartTime;

    // Velocity tracking
    private Vector3 previousWorldPos;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
        originalParent = transform.parent;
        capWasActive = capModel != null && capModel.activeSelf;

        if (sprayAudioSource != null)
            sprayAudioSource.Stop();

        previousWorldPos = transform.position;
    }

    void Update()
    {
        if (!isEquipped)
        {
            previousWorldPos = transform.position;
            return;
        }

        // Compute world-space velocity from position delta
        float speed = (transform.position - previousWorldPos).magnitude / Time.deltaTime;
        previousWorldPos = transform.position;

        bool isUpsideDown = transform.up.y < -upsideDownThreshold;
        bool isShaking    = speed >= shakeVelocityThreshold;
        bool shouldSpray  = isUpsideDown && isShaking;

        ToggleSpray(shouldSpray);

        if (isSpraying && Time.time >= nextSpawnTime)
        {
            SpawnPellet();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    // Assign this to 'Select Entered' in the XR Simple Interactable
    public void PickupBottle(SelectEnterEventArgs args)
    {
        if (isEquipped) return;

        if (handRayInteractor != null) handRayInteractor.enabled = false;
        if (handLineVisual != null)    handLineVisual.enabled    = false;

        if (capModel != null) capModel.SetActive(false);

        transform.SetParent(args.interactorObject.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.forward       = args.interactorObject.transform.forward;
        transform.Rotate(0, 0, bottleRotationOffset, Space.Self);

        if (gripPoint != null)
        {
            Vector3 offset = transform.position - gripPoint.position;
            transform.position += offset;
        }

        previousWorldPos = transform.position;
        isEquipped = true;
    }

    public void ResetTool()
    {
        isEquipped = false;

        // Bug fix #2: force-stop audio regardless of minSprayDuration
        isSpraying = false;
        if (pelletParticles != null) pelletParticles.Stop();
        if (sprayAudioSource != null) sprayAudioSource.Stop();

        transform.SetParent(originalParent);
        transform.localPosition = originalPos;
        transform.localRotation = originalRot;

        if (capModel != null) capModel.SetActive(capWasActive);

        // Bug fix #3: wait one frame so XRIT clears selection state before re-enabling
        StartCoroutine(RestoreInteractorNextFrame());
    }

    private IEnumerator RestoreInteractorNextFrame()
    {
        yield return null;  // wait one frame
        if (handRayInteractor != null) handRayInteractor.enabled = true;
        if (handLineVisual != null)    handLineVisual.enabled    = true;
    }

    public void ToggleSpray(bool active)
    {
        if (isSpraying == active) return;

        isSpraying = active;

        if (pelletParticles != null)
        {
            if (active) pelletParticles.Play();
            else        pelletParticles.Stop();
        }

        if (sprayAudioSource != null)
        {
            if (active && !sprayAudioSource.isPlaying)
            {
                sprayAudioSource.Play();
                sprayStartTime = Time.time;
            }
            else if (!active)
            {
                // Only stop if minimum duration has passed
                float elapsed = Time.time - sprayStartTime;
                if (elapsed >= minSprayDuration)
                {
                    sprayAudioSource.Stop();
                }
                else
                {
                    // Keep spraying until minimum duration is met
                    isSpraying = true;
                }
            }
        }
    }

    private void SpawnPellet()
    {
        if (blueCapsulePrefab == null) return;

        Transform origin = sprayAnchor != null ? sprayAnchor : transform;

        // Direction: downward from the bottle mouth, with a random cone spread
        Vector3 baseDir = -origin.up;
        Vector3 randomDir = Random.insideUnitSphere * Mathf.Tan(spawnSpread * Mathf.Deg2Rad);
        Vector3 spawnDir = (baseDir + randomDir).normalized;

        GameObject pellet = Object.Instantiate(blueCapsulePrefab, origin.position, Random.rotation);

        Rigidbody rb = pellet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = spawnDir * spawnSpeed;
        }
    }
}
