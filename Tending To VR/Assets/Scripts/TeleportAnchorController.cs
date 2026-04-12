using UnityEngine;


/// <summary>
/// Attach to a GameObject that also has an XRI TeleportationAnchor component.
///
/// RESPONSIBILITIES:
///   - Activates/deactivates itself by listening to GameManager.OnStageChanged.
///   - When the player teleports to this anchor, begins the stage sequence:
///       1. Disables its own glow/collider immediately so it can't be re-triggered.
///       2. Notifies StageSequencer to begin the interaction for this stage.
///
/// SCENE SETUP PER ANCHOR:
///   - Add this script alongside an XRI TeleportationAnchor component.
///   - Set 'myStage' to the Stage enum value this anchor belongs to.
///   - Assign 'anchorVisual' to the child GameObject that holds the glow shader/mesh.
///   - The TeleportationAnchor's Teleporting event should point to nothing —
///     this script hooks it in Awake via code.
///
/// HIERARCHY EXAMPLE:
///   Anchor_CutTheGrass
///     ├── TeleportationAnchor (XRI built-in)
///     ├── TeleportAnchorController (this script)
///     └── AnchorVisual (glowing mesh / shader)
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor))]
public class TeleportAnchorController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("The stage during which this anchor should be active and glowing.")]
    [SerializeField] private Stage myStage;

    [Tooltip("Optional: a second stage that reactivates this same anchor (e.g. BrokenGrass reuses CutTheGrass anchor).")]
    [SerializeField] private bool hasSecondStage = false;
    [SerializeField] private Stage mySecondStage;

    [Tooltip("The child GameObject containing the glow shader/mesh. " +
             "Enabled when active, disabled after teleport.")]
    [SerializeField] private GameObject anchorVisual;

    [Header("Audio")]
    [Tooltip("Optional: AudioSource for 3D spatial sound when anchor is active. " +
             "Will be auto-configured for 3D spatial audio if assigned.")]
    [SerializeField] private AudioSource teleportAudioSource;

    [Tooltip("Optional: Audio clip to play when the anchor is active. " +
             "If left empty, will just control the AudioSource.")]
    [SerializeField] private AudioClip teleportActiveSound;

    [Tooltip("Should the teleport sound loop while the anchor is active?")]
    [SerializeField] private bool loopSound = true;

    [Tooltip("Duration in seconds for audio fade in/out transitions.")]
    [SerializeField] private float fadeDuration = 1f;

    // -------------------------------------------------------------------------
    // Private refs
    // -------------------------------------------------------------------------

    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor _teleportationAnchor;
    private Collider _collider;
    private Coroutine _audioFadeCoroutine;
    private float _targetVolume = 0.5f; // Store the intended volume when fully faded in

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _teleportationAnchor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor>();
        _collider = GetComponent<Collider>();

        // Hook into XRI's teleport event so we know when the player arrives.
        _teleportationAnchor.teleporting.AddListener(OnPlayerTeleported);

        // Configure AudioSource for 3D spatial audio if assigned.
        if (teleportAudioSource != null)
        {
            teleportAudioSource.spatialBlend = 1f; // Full 3D
            _targetVolume = 0.5f; // Store the target volume
            teleportAudioSource.volume = 0f; // Start at 0, will fade in
            teleportAudioSource.loop = loopSound;
            teleportAudioSource.playOnAwake = false;
            teleportAudioSource.rolloffMode = AudioRolloffMode.Linear;
            teleportAudioSource.minDistance = 1f;
            teleportAudioSource.maxDistance = 20f;
            
            if (teleportActiveSound != null)
            {
                teleportAudioSource.clip = teleportActiveSound;
            }
        }
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    private void OnDestroy()
    {
        if (_teleportationAnchor != null)
            _teleportationAnchor.teleporting.RemoveListener(OnPlayerTeleported);
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        bool isMyStage = newStage == myStage
            || (hasSecondStage && newStage == mySecondStage);

        // Only activate if this is our stage AND the stage actually wants a teleporter.
        if (isMyStage)
        {
            StageData data = GameManager.Instance?.GetStageData(newStage);
            bool stageWantsTeleporter = data != null && data.hasTeleportAnchor;
            SetAnchorActive(stageWantsTeleporter);
        }
        else
        {
            SetAnchorActive(false);
        }
    }

    // -------------------------------------------------------------------------
    // Teleport Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by XRI's TeleportationAnchor.teleporting event the moment
    /// the player is teleported to this anchor's position.
    /// </summary>
    private void OnPlayerTeleported(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportingEventArgs args)
    {
        // Determine which stage we're currently in (important for reused anchors).
        Stage currentStage = GetCurrentActiveStage();
        Debug.Log($"[TeleportAnchorController] Player teleported to anchor for stage: {currentStage}");

        // Immediately hide the glow and disable interaction so it
        // can't be triggered again this stage.
        SetAnchorActive(false);

        // Tell the StageSequencer the player has arrived and
        // the interaction phase for this stage can begin.
        StageSequencer.Instance?.OnPlayerArrivedAtAnchor(currentStage);
    }

    /// <summary>
    /// Returns the stage this anchor is currently active for.
    /// Checks if we're in the second stage first, otherwise returns the primary stage.
    /// </summary>
    private Stage GetCurrentActiveStage()
    {
        Stage currentStage = GameManager.Instance.CurrentStage;
        
        // If we have a second stage configured and we're currently in it, return that.
        if (hasSecondStage && currentStage == mySecondStage)
        {
            return mySecondStage;
        }
        
        // Otherwise return the primary stage.
        return myStage;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetAnchorActive(bool active)
    {
        // Show/hide the glow visual.
        if (anchorVisual != null)
            anchorVisual.SetActive(active);

        // Enable/disable the collider so the ray interactor
        // can only hit it when it should be active.
        if (_collider != null)
            _collider.enabled = active;

        // Enable/disable XRI interaction.
        _teleportationAnchor.enabled = active;

        // Fade audio in or out.
        if (teleportAudioSource != null)
        {
            // Stop any currently running fade coroutine.
            if (_audioFadeCoroutine != null)
            {
                StopCoroutine(_audioFadeCoroutine);
            }

            if (active)
            {
                // Fade in the audio.
                _audioFadeCoroutine = StartCoroutine(FadeAudioIn());
            }
            else
            {
                // Fade out the audio.
                _audioFadeCoroutine = StartCoroutine(FadeAudioOut());
            }
        }
    }

    // -------------------------------------------------------------------------
    // Audio Fading
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gradually increases audio volume from 0 to the target volume.
    /// </summary>
    private System.Collections.IEnumerator FadeAudioIn()
    {
        if (teleportAudioSource == null) yield break;

        // Start playing if not already playing.
        if (!teleportAudioSource.isPlaying)
        {
            teleportAudioSource.volume = 0f;
            teleportAudioSource.Play();
        }

        float startVolume = teleportAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            teleportAudioSource.volume = Mathf.Lerp(startVolume, _targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        teleportAudioSource.volume = _targetVolume;
    }

    /// <summary>
    /// Gradually decreases audio volume from current volume to 0, then stops playback.
    /// </summary>
    private System.Collections.IEnumerator FadeAudioOut()
    {
        if (teleportAudioSource == null) yield break;

        float startVolume = teleportAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            teleportAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        teleportAudioSource.volume = 0f;
        
        if (teleportAudioSource.isPlaying)
        {
            teleportAudioSource.Stop();
        }
    }
}
