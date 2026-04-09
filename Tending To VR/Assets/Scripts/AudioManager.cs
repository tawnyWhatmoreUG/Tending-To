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

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    private void Start()
    {
        StartAllLayers();
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        StageData data = GameManager.Instance?.GetStageData(newStage);
        if (data == null) return;

        if (data.soundscapeLayerToRemove >= 0)
        {
            FadeOutLayer(data.soundscapeLayerToRemove);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fades out a specific layer by index. Safe to call if already faded.
    /// </summary>
    public void FadeOutLayer(int index)
    {
        if (!IsValidIndex(index)) return;
        if (_targetVolumes[index] <= 0f)
        {
            Log($"Layer {index} is already faded out — skipping.");
            return;
        }

        Log($"Fading out layer {index} ({soundscapeLayers[index].name}) over {fadeOutDuration}s.");

        if (_fadeCoroutines[index] != null)
            StopCoroutine(_fadeCoroutines[index]);

        _targetVolumes[index] = 0f;
        _fadeCoroutines[index] = StartCoroutine(FadeLayer(index, 0f, fadeOutDuration));
    }

    /// <summary>
    /// Immediately silences all layers. Used for hard resets.
    /// </summary>
    public void SilenceAll()
    {
        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            if (_fadeCoroutines[i] != null)
                StopCoroutine(_fadeCoroutines[i]);

            _targetVolumes[i] = 0f;
            soundscapeLayers[i].volume = 0f;
        }
    }

    /// <summary>
    /// Restores all layers to full volume and restarts playback.
    /// Useful for debug resets when jumping back to early stages.
    /// </summary>
    public void RestoreAll()
    {
        for (int i = 0; i < soundscapeLayers.Length; i++)
        {
            if (_fadeCoroutines[i] != null)
                StopCoroutine(_fadeCoroutines[i]);

            _targetVolumes[i] = 1f;
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
            soundscapeLayers[i].volume = 1f;
            _targetVolumes[i] = 1f;
        }
    }

    private void StartAllLayers()
    {
        if (soundscapeLayers == null) return;

        foreach (var source in soundscapeLayers)
        {
            if (source != null && !source.isPlaying)
                source.Play();
        }

        Log($"Started {soundscapeLayers.Length} soundscape layers.");
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
    /// <summary>
    /// When debug jumping to an early stage, restore all layers so the
    /// soundscape reflects the correct state for that point in the game.
    /// Called automatically when OnStageChanged fires from a debug jump.
    /// 
    /// This re-evaluates which layers should be active based on the new stage
    /// and fades out only those that should already be gone by that point.
    /// </summary>
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
