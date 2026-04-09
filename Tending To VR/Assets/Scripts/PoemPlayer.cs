using System.Collections;
using UnityEngine;

/// <summary>
/// Plays the poem verse AudioClip for each stage and reports completion
/// to GameManager via ReportVerseComplete().
///
/// SETUP:
///   - Add this script to a GameObject with an AudioSource component.
///   - Set the AudioSource Spatial Blend to 0 (2D).
///   - Set AudioSource Play On Awake to false.
///   - Assign verse clips via StageData assets (no direct assignment needed here).
///
/// FLOW:
///   StageSequencer calls PlayVerseForStage(stage) after the player arrives
///   at an anchor, or immediately on stage change for Broken stages.
///   When the clip ends, ReportVerseComplete() is sent to GameManager,
///   which checks whether the stage can now advance.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PoemPlayer : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static PoemPlayer Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Fade")]
    [Tooltip("Duration in seconds to fade the verse in at the start.")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Duration in seconds to fade the verse out at the end.")]
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Debug")]
    [Tooltip("If true, logs verse start/end events to the console.")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private AudioSource _audioSource;
    private Coroutine _playbackCoroutine;
    private Stage _currentStage;

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

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // Force 2D
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by StageSequencer to play the verse for a given stage.
    /// Looks up the AudioClip from StageData. If a verse is already
    /// playing it is stopped cleanly before the new one begins.
    /// </summary>
    public void PlayVerseForStage(Stage stage)
    {
        StageData data = GameManager.Instance?.GetStageData(stage);

        if (data == null)
        {
            Debug.LogWarning($"[PoemPlayer] No StageData found for stage: {stage}");
            return;
        }

        if (data.poemVerse == null)
        {
            // No verse for this stage (e.g. PendingToDo or Relax).
            // Report verse complete immediately so GameManager isn't blocked.
            Log($"No verse clip for stage {stage} — reporting verse complete immediately.");
            GameManager.Instance?.ReportVerseComplete();
            return;
        }

        // Stop any currently playing verse.
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _audioSource.Stop();
        }

        _currentStage = stage;
        _playbackCoroutine = StartCoroutine(PlayVerseCoroutine(data.poemVerse, stage));
    }

    /// <summary>
    /// Stops playback immediately without reporting verse complete.
    /// Use only for hard resets (e.g. debug stage jumps).
    /// </summary>
    public void StopPlayback()
    {
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }
        _audioSource.Stop();
        _audioSource.volume = 1f;
    }

    // -------------------------------------------------------------------------
    // Playback Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator PlayVerseCoroutine(AudioClip clip, Stage stage)
    {
        Log($"Playing verse for stage: {stage} — clip: {clip.name} ({clip.length:F1}s)");

        // Fade in
        _audioSource.clip = clip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        yield return StartCoroutine(FadeVolume(0f, 1f, fadeInDuration));

        // Wait for the clip to finish, minus fade-out time
        float waitTime = clip.length - fadeInDuration - fadeOutDuration;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // Fade out
        yield return StartCoroutine(FadeVolume(1f, 0f, fadeOutDuration));

        _audioSource.Stop();
        _audioSource.volume = 1f;
        _playbackCoroutine = null;

        Log($"Verse complete for stage: {stage}");
        GameManager.Instance?.ReportVerseComplete();
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            _audioSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _audioSource.volume = to;
    }

    // -------------------------------------------------------------------------
    // Debug Stage Jump Support
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    /// <summary>
    /// When a debug stage jump fires OnStageChanged, stop any in-progress
    /// verse so it doesn't report completion for the wrong stage.
    /// </summary>
    private void OnStageChanged(Stage newStage)
    {
        // Only interrupt if this is a jump to a different stage than what
        // is currently playing, to avoid cancelling a legitimately started verse.
        if (_playbackCoroutine != null && newStage != _currentStage)
        {
            Log($"Stage jumped to {newStage} — stopping in-progress verse for {_currentStage}.");
            StopPlayback();
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[PoemPlayer] {message}");
    }
}
