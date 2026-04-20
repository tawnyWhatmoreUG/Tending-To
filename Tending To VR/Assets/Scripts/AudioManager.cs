using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the spatial soundscape — a set of looping AudioSources that
/// each represent one layer of the garden ambience. Layers are faded out
/// one by one as stages complete, leaving progressive silence by the end.
///
/// SOUNDSCAPE LAYER ORDER (matches StageData.soundscapeLayerToRemove index):
///   0 — Cricket sound       (removed: CutTheGrass)
///   1 — Bee sound           (removed: ClearThePath)
///   2 — Bird song           (removed: SaveTheHostas)
///   3 — Rustling sounds     (removed: FeedTheFlowers)
///   4 — Low insect drone    (removed: DoTheWindowBox)
///
/// FADE TIMING:
///   AudioManager does NOT listen to OnStageChanged. Fades are driven
///   externally by GameManager.TriggerSoundscapeFadeForCurrentStage(),
///   which is called at the moment the player completes each interaction —
///   not at the start of the next stage.
///
/// SETUP:
///   1. Add this script to a GameObject named "AudioManager".
///   2. Add one child GameObject per soundscape layer, each with an AudioSource:
///        AudioManager
///          ├── Layer_Crickets       (AudioSource, loop=true, playOnAwake=false)
///          ├── Layer_Bees           (AudioSource, loop=true, playOnAwake=false)
///          ├── Layer_Birdsong       (AudioSource, loop=true, playOnAwake=false)
///          ├── Layer_Rustling       (AudioSource, loop=true, playOnAwake=false)
///          └── Layer_InsectDrone    (AudioSource, loop=true, playOnAwake=false)
///   3. Assign each AudioSource to the soundscapeLayers array in the Inspector,
///      in the order listed above.
///   4. Assign your AudioClips to each AudioSource's clip field.
///   5. AudioManager starts all layers playing on Start() at full volume.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static AudioManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Soundscape Layers")]
    [Tooltip("Looping AudioSources in order: Crickets, Bees, Birdsong, Rustling, Drone. " +
             "Index must match StageData.soundscapeLayerToRemove.")]
    [SerializeField] private AudioSource[] soundscapeLayers;

    [Header("Fade Settings")]
    [Tooltip("How long in seconds each layer takes to fade out when removed.")]
    [SerializeField] private float fadeOutDuration = 3f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    // Tracks the target volume for each layer so concurrent fades don't conflict.
    private float[] _targetVolumes;
    private Coroutine[] _fadeCoroutines;

    // Tracks which layers have been permanently faded out and should never return to active.
    private bool[] _permanentlyFadedOut;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitialiseLayers();
    }

    // NOTE: AudioManager intentionally does NOT subscribe to GameManager.OnStageChanged.
    // Soundscape fade timing is controlled by GameManager.TriggerSoundscapeFadeForCurrentStage(),
    // called at interaction completion. This prevents layers from fading at stage start.

    private void Start()
    {
        StartAllLayers();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fades out a specific layer by index. Safe to call if already faded.
    /// Once faded out, the layer will never return to active state.
    /// Called by GameManager when the player completes an interaction.
    /// </summary>
    public void FadeOutLayer(int index)
    {
        if (!IsValidIndex(index)) return;
        if (_permanentlyFadedOut[index])
        {
            Log($"Layer {index} is already permanently faded out — skipping.");
            return;
        }

        Log($"Fading out layer {index} ({soundscapeLayers[index].name}) over {fadeOutDuration}s.");

        if (_fadeCoroutines[index] != null)
            StopCoroutine(_fadeCoroutines[index]);

        _targetVolumes[index] = 0f;
        _permanentlyFadedOut[index] = true;
        _fadeCoroutines[index] = StartCoroutine(FadeLayer(index, 0f, fadeOutDuration));
    }

    /// <summary>
    /// Immediately silences all layers. Used for hard resets.
    /// Marks all layers as permanently faded out.
    /// </summary>
    public void SilenceAll()
    {
        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            if (_fadeCoroutines[i] != null)
                StopCoroutine(_fadeCoroutines[i]);

            _targetVolumes[i] = 0f;
            _permanentlyFadedOut[i] = true;
            soundscapeLayers[i].volume = 0f;

            if (soundscapeLayers[i].isPlaying)
                soundscapeLayers[i].Stop();
        }

        Log("All soundscape layers silenced.");
    }

    /// <summary>
    /// Restores all layers to full volume and restarts playback.
    /// Useful for debug resets when jumping back to early stages.
    /// Clears the permanent fade-out tracking so layers can be reactivated.
    /// Only call this from AudioManagerDebugHelper — never during normal gameplay.
    /// </summary>
    public void RestoreAll()
    {
        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            if (_fadeCoroutines[i] != null)
                StopCoroutine(_fadeCoroutines[i]);

            _targetVolumes[i] = 1f;
            _permanentlyFadedOut[i] = false;
            soundscapeLayers[i].volume = 1f;

            if (!soundscapeLayers[i].isPlaying)
                soundscapeLayers[i].Play();
        }

        Log("All soundscape layers restored to full volume.");
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    private void InitialiseLayers()
    {
        if (soundscapeLayers == null || soundscapeLayers.Length == 0)
        {
            Debug.LogError("[AudioManager] No soundscape layers assigned in Inspector.");
            return;
        }

        _targetVolumes = new float[soundscapeLayers.Length];
        _fadeCoroutines = new Coroutine[soundscapeLayers.Length];
        _permanentlyFadedOut = new bool[soundscapeLayers.Length];

        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            if (soundscapeLayers[i] == null)
            {
                Debug.LogError($"[AudioManager] Soundscape layer {i} is null. Check Inspector assignments.");
                continue;
            }

            soundscapeLayers[i].loop = true;
            soundscapeLayers[i].playOnAwake = false;
            soundscapeLayers[i].spatialBlend = 0f; // 2D ambient
            // Respect the volume set in the Inspector as the starting value.
            _targetVolumes[i] = soundscapeLayers[i].volume;
            _permanentlyFadedOut[i] = false;
        }
    }

    private void StartAllLayers()
    {
        if (soundscapeLayers == null)
        {
            Debug.LogError("[AudioManager] StartAllLayers: soundscapeLayers array is NULL. Nothing will play.");
            return;
        }

        if (soundscapeLayers.Length == 0)
        {
            Debug.LogError("[AudioManager] StartAllLayers: soundscapeLayers array is EMPTY. Assign AudioSources in Inspector.");
            return;
        }

        // Check for AudioListener in scene — missing or duplicate kills all audio.
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length == 0)
            Debug.LogError("[AudioManager] No AudioListener found in scene. All audio will be silent.");
        else if (listeners.Length > 1)
            Debug.LogWarning($"[AudioManager] {listeners.Length} AudioListeners found. Unity will disable all but one — audio may be silent. Remove duplicate AudioListeners.");

        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            // Never start a layer that has already been permanently faded out.
            if (_permanentlyFadedOut[i])
            {
                Log($"Layer {i} is permanently faded out — skipping playback.");
                continue;
            }

            var source = soundscapeLayers[i];
            if (source == null)
            {
                Debug.LogError($"[AudioManager] Layer {i} is null.");
                continue;
            }
            if (source.clip == null)
            {
                Debug.LogError($"[AudioManager] Layer {i} ({source.name}) has no AudioClip assigned. It will be silent.");
                continue;
            }
            if (!source.isPlaying)
                source.Play();
        }

        Log($"Started soundscape layers.");
    }

    // -------------------------------------------------------------------------
    // Fade Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FadeLayer(int index, float targetVolume, float duration)
    {
        AudioSource source = soundscapeLayers[index];
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (targetVolume <= 0f)
            source.Stop();

        _fadeCoroutines[index] = null;
        Log($"Layer {index} ({source.name}) fade complete. Final volume: {targetVolume}");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool IsValidIndex(int index)
    {
        if (soundscapeLayers == null || index < 0 || index >= soundscapeLayers.Length)
        {
            Debug.LogWarning($"[AudioManager] Layer index {index} is out of range.");
            return false;
        }
        return true;
    }

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[AudioManager] {message}");
    }

    // -------------------------------------------------------------------------
    // Debug Support
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keeps _targetVolumes in sync if array size changes in Inspector.
        if (soundscapeLayers != null && _targetVolumes != null &&
            soundscapeLayers.Length != _targetVolumes.Length)
        {
            InitialiseLayers();
        }
    }
#endif
}