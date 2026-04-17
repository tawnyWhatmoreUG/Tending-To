using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays poem subtitles line-by-line in sync with PoemPlayer audio playback.
///
/// SETUP:
///   1. Create a World Space Canvas as a child of your XR Camera transform.
///      - Position: (0, -0.4, 2) relative to camera — lower-third, 2m ahead.
///      - Add a CanvasGroup component to the Canvas root.
///      - Add a semi-transparent black Image child as a backing panel.
///      - Add a TextMeshProUGUI child for the subtitle text (centred, white, readable size).
///   2. Add this SubtitlePlayer component to any persistent scene GameObject.
///   3. Assign subtitleText and canvasGroup in the Inspector.
///   4. Populate SubtitleMappings: one entry per verse stage, pointing to its SubtitleData asset.
///
/// FLOW:
///   PoemPlayer fires OnVerseStarted → SubtitlePlayer looks up the SubtitleData for that stage
///   → lines are displayed one at a time using the authored lineStartTimes.
///   PoemPlayer fires OnVerseStopped (or a stage change occurs) → panel fades out.
/// </summary>
public class SubtitlePlayer : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static SubtitlePlayer Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The TextMeshProUGUI component that displays subtitle text.")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Tooltip("CanvasGroup on the subtitle panel root — used for whole-panel fade in/out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [Tooltip("Duration in seconds to fade the panel in when a verse starts.")]
    [SerializeField] private float panelFadeInDuration = 0.3f;

    [Tooltip("Duration in seconds to fade the panel out when a verse ends.")]
    [SerializeField] private float panelFadeOutDuration = 0.5f;

    [Tooltip("Duration in seconds for each line crossfade transition.")]
    [SerializeField] private float lineCrossfadeDuration = 0.25f;

    [Header("Subtitle Mappings")]
    [Tooltip("One entry per stage that has a verse. Stage must match the SubtitleData asset's stage field.")]
    [SerializeField] private SubtitleMapping[] subtitleMappings;

    [Serializable]
    public struct SubtitleMapping
    {
        public Stage stage;
        public SubtitleData data;
    }

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Coroutine _displayCoroutine;
    private Coroutine _panelFadeCoroutine;

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

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (subtitleText != null) subtitleText.text = string.Empty;
    }

    private void OnEnable()
    {
        PoemPlayer.OnVerseStarted += HandleVerseStarted;
        PoemPlayer.OnVerseStopped += HandleVerseStopped;
        GameManager.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        PoemPlayer.OnVerseStarted -= HandleVerseStarted;
        PoemPlayer.OnVerseStopped -= HandleVerseStopped;
        GameManager.OnStageChanged -= HandleStageChanged;
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    private void HandleVerseStarted(Stage stage, float clipLength)
    {
        SubtitleData data = GetDataForStage(stage);
        if (data == null || data.lines == null || data.lines.Length == 0) return;

        if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
        _displayCoroutine = StartCoroutine(DisplaySubtitlesCoroutine(data));
    }

    private void HandleVerseStopped()
    {
        StopDisplayAndFadeOut();
    }

    private void HandleStageChanged(Stage newStage)
    {
        // Clears any subtitle that may still be visible during a debug stage jump.
        // In normal gameplay the verse finishes before stage advances, so this
        // acts only as a safety net.
        StopDisplayAndFadeOut();
    }

    // -------------------------------------------------------------------------
    // Display Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator DisplaySubtitlesCoroutine(SubtitleData data)
    {
        // Show the first line immediately and fade the panel in together.
        SetTextInstant(data.lines[0], data.textColor, alpha: 0f);
        yield return StartCoroutine(FadePanel(0f, 1f, panelFadeInDuration));
        SetTextInstant(data.lines[0], data.textColor, alpha: 1f);

        float verseStartTime = Time.time;

        for (int i = 1; i < data.lines.Length; i++)
        {
            float targetTime = (data.lineStartTimes != null && i < data.lineStartTimes.Length)
                ? verseStartTime + data.lineStartTimes[i]
                : verseStartTime + i * 5f; // fallback: 5s per line if timings not set

            // Wait until the authored start time for this line.
            while (Time.time < targetTime)
                yield return null;

            yield return StartCoroutine(CrossfadeText(data.lines[i], data.textColor));
        }

        // Last line remains visible until HandleVerseStopped or HandleStageChanged fires.
        _displayCoroutine = null;
    }

    // -------------------------------------------------------------------------
    // Fade & Text Helpers
    // -------------------------------------------------------------------------

    private void StopDisplayAndFadeOut()
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }

        if (_panelFadeCoroutine != null) StopCoroutine(_panelFadeCoroutine);
        _panelFadeCoroutine = StartCoroutine(FadePanelOut());
    }

    private IEnumerator CrossfadeText(string newText, Color color)
    {
        float half = lineCrossfadeDuration * 0.5f;

        // Fade text alpha out.
        float elapsed = 0f;
        Color c = subtitleText != null ? subtitleText.color : color;
        float startAlpha = c.a;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(startAlpha, 0f, elapsed / half), color);
            yield return null;
        }
        SetTextAlpha(0f, color);

        if (subtitleText != null) subtitleText.text = newText;

        // Fade text alpha back in.
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(0f, 1f, elapsed / half), color);
            yield return null;
        }
        SetTextAlpha(1f, color);
    }

    private IEnumerator FadePanel(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadePanelOut()
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        if (startAlpha <= 0f)
        {
            if (subtitleText != null) subtitleText.text = string.Empty;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < panelFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / panelFadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        if (subtitleText != null) subtitleText.text = string.Empty;
        _panelFadeCoroutine = null;
    }

    private void SetTextInstant(string text, Color color, float alpha)
    {
        if (subtitleText == null) return;
        subtitleText.text = text;
        Color c = color;
        c.a = alpha;
        subtitleText.color = c;
    }

    private void SetTextAlpha(float alpha, Color color)
    {
        if (subtitleText == null) return;
        Color c = color;
        c.a = alpha;
        subtitleText.color = c;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SubtitleData GetDataForStage(Stage stage)
    {
        foreach (var mapping in subtitleMappings)
        {
            if (mapping.stage == stage) return mapping.data;
        }
        return null;
    }
}
