using System.Collections;
using UnityEngine;

/// <summary>
/// Lerps the "_Saturation" float property on a Renderer's material from 1
/// (full colour) down to a configurable minimum when a target stage is reached.
///
/// SETUP:
///   1. Add this script to the GameObject whose Renderer you want to desaturate.
///   2. Assign targetRenderer (or leave blank to auto-detect on this GameObject).
///   3. Set triggerStage to the Stage at which desaturation should begin.
///   4. Tune minimumSaturation, fadeDuration, and fadeDelay in the Inspector.
///
/// SHADER REQUIREMENT:
///   The material must expose a "_Saturation" Float property
///   (default 1 = full colour, 0 = greyscale).
/// </summary>
public class DesaturateEffect : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Stage Trigger")]
    [Tooltip("The stage at which desaturation begins.")]
    [SerializeField] private Stage triggerStage;

    [Tooltip("If true, resets to full saturation when an earlier stage begins " +
             "(useful when debug-jumping stages).")]
    [SerializeField] private bool resetOnEarlierStage = true;

    [Header("Renderer")]
    [Tooltip("The Renderer whose material will be desaturated. " +
             "Leave empty to auto-detect from this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Index of the material on the Renderer to affect (0 = first/only material).")]
    [SerializeField] private int materialIndex = 0;

    [Header("Saturation Settings")]
    [Tooltip("Target saturation value at the end of the transition (0 = fully greyscale).")]
    [SerializeField, Range(0f, 1f)] private float minimumSaturation = 0f;

    [Tooltip("How long in seconds the desaturation transition takes.")]
    [SerializeField] private float fadeDuration = 3f;

    [Tooltip("Delay in seconds after stage change before desaturation begins.")]
    [SerializeField] private float fadeDelay = 0f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Material _instanceMaterial;
    private Coroutine _fadeCoroutine;
    private static readonly int SaturationProperty = Shader.PropertyToID("_Saturation");

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[DesaturateEffect] No Renderer found on {name}. Assign one in the Inspector.");
            enabled = false;
            return;
        }

        _instanceMaterial = targetRenderer.materials[materialIndex];

        if (!_instanceMaterial.HasProperty(SaturationProperty))
        {
            Debug.LogError($"[DesaturateEffect] Material on {name} does not have a '_Saturation' property.");
            enabled = false;
            return;
        }

        _instanceMaterial.SetFloat(SaturationProperty, 1f);
    }

    private void OnEnable()
    {
        StageSequencer.OnPlayerArrived += OnStageChanged;
    }

    private void OnDisable()
    {
        StageSequencer.OnPlayerArrived -= OnStageChanged;
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        if (newStage == triggerStage)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(DesaturateRoutine());
            return;
        }

        if (resetOnEarlierStage && (int)newStage < (int)triggerStage)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _instanceMaterial.SetFloat(SaturationProperty, 1f);
            Log($"Reset to full saturation (jumped back to {newStage}).");
        }
    }

    // -------------------------------------------------------------------------
    // Desaturation Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator DesaturateRoutine()
    {
        Log($"Desaturation triggered by stage: {triggerStage}. Delay: {fadeDelay}s, Duration: {fadeDuration}s.");

        if (fadeDelay > 0f)
            yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float saturation = Mathf.Lerp(1f, minimumSaturation, elapsed / fadeDuration);
            _instanceMaterial.SetFloat(SaturationProperty, saturation);
            yield return null;
        }

        _instanceMaterial.SetFloat(SaturationProperty, minimumSaturation);
        _fadeCoroutine = null;
        Log("Desaturation complete.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[DesaturateEffect] {message}");
    }
}
