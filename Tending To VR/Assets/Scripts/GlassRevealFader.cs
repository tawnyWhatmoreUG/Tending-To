using System.Collections;
using UnityEngine;

/// <summary>
/// Fades a single material's alpha from its starting value down to a minimum
/// when a target stage is reached (i.e. when the player arrives at that stage's anchor).
///
/// SETUP:
///   1. Attach this script to the GameObject whose Renderer holds the material.
///   2. Assign targetRenderer, or leave blank to auto-detect on this GameObject.
///   3. Set materialIndex to the slot of the material you want to fade.
///   4. Set triggerStage to the Stage that should start the fade.
///   5. Tune minimumAlpha, fadeDuration, and fadeDelay in the Inspector.
///
/// SHADER REQUIREMENT:
///   The material must expose a Color property that drives alpha.
///   URP/HDRP standard is "_BaseColor"; legacy built-in is "_Color".
///   The material's Surface Type must be set to Transparent.
/// </summary>
public class GlassRevealFader : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Stage Trigger")]
    [Tooltip("The stage at which the alpha fade begins.")]
    [SerializeField] private Stage triggerStage;

    [Tooltip("If true, resets to the original alpha when an earlier stage is entered " +
             "(useful when debug-jumping stages).")]
    [SerializeField] private bool resetOnEarlierStage = true;

    [Header("Renderer")]
    [Tooltip("Renderer that holds the material to fade. " +
             "Leave empty to auto-detect from this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Index of the material slot to fade (0 = first material).")]
    [SerializeField] private int materialIndex = 0;

    [Header("Alpha Settings")]
    [Tooltip("Target alpha at the end of the transition (0 = fully transparent).")]
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0f;

    [Tooltip("How long in seconds the fade takes.")]
    [SerializeField] private float fadeDuration = 3f;

    [Tooltip("Delay in seconds after the stage change before the fade starts.")]
    [SerializeField] private float fadeDelay = 0f;

    [Header("Shader")]
    [Tooltip("The Color property on the shader that controls alpha. " +
             "URP/HDRP: '_BaseColor'. Built-in: '_Color'.")]
    [SerializeField] private string colorProperty = "_BaseColor";

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Material _instanceMaterial;
    private float _originalAlpha;
    private Coroutine _fadeCoroutine;
    private int _colorPropertyID;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[GlassRevealFader] No Renderer found on '{name}'. Assign one in the Inspector.");
            enabled = false;
            return;
        }

        if (materialIndex < 0 || materialIndex >= targetRenderer.materials.Length)
        {
            Debug.LogError($"[GlassRevealFader] materialIndex {materialIndex} is out of range on '{name}' " +
                           $"(Renderer has {targetRenderer.materials.Length} material(s)).");
            enabled = false;
            return;
        }

        _colorPropertyID = Shader.PropertyToID(colorProperty);
        _instanceMaterial = targetRenderer.materials[materialIndex];

        if (!_instanceMaterial.HasProperty(_colorPropertyID))
        {
            Debug.LogError($"[GlassRevealFader] Material at index {materialIndex} on '{name}' " +
                           $"does not have a '{colorProperty}' property.");
            enabled = false;
            return;
        }

        _originalAlpha = _instanceMaterial.GetColor(_colorPropertyID).a;
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

            _fadeCoroutine = StartCoroutine(FadeAlphaRoutine());
            return;
        }

        if (resetOnEarlierStage && (int)newStage < (int)triggerStage)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            SetAlpha(_originalAlpha);
            Log($"Reset to original alpha ({_originalAlpha:F2}) on stage jump back to {newStage}.");
        }
    }

    // -------------------------------------------------------------------------
    // Fade Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FadeAlphaRoutine()
    {
        Log($"Fade triggered by stage '{triggerStage}'. Delay: {fadeDelay}s, Duration: {fadeDuration}s.");

        if (fadeDelay > 0f)
            yield return new WaitForSeconds(fadeDelay);

        float startAlpha = _instanceMaterial.GetColor(_colorPropertyID).a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(Mathf.Lerp(startAlpha, minimumAlpha, t));
            yield return null;
        }

        SetAlpha(minimumAlpha);
        _fadeCoroutine = null;
        Log("Fade complete.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetAlpha(float alpha)
    {
        Color c = _instanceMaterial.GetColor(_colorPropertyID);
        c.a = alpha;
        _instanceMaterial.SetColor(_colorPropertyID, c);
    }

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[GlassRevealFader] {message}");
    }
}
