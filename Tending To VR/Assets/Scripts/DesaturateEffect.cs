using System.Collections;
using UnityEngine;

/// <summary>
/// Gradually desaturates the material on this GameObject when the player
/// arrives at the teleport anchor for a specified stage.
///
/// SETUP:
///   1. Add to the lawn GameObject (needs a Renderer).
///   2. Set 'triggerStage' to BrokenGrass in the Inspector.
///   3. Call OnPlayerArrivedAtStage(stage) from BrokenSceneController,
///      matching the pattern your other broken-stage effects use.
///   4. Set 'desaturationDuration' to roughly match the verse clip length.
///
/// SHADER NOTE:
///   Option A (preferred): Use a shader with a _Saturation float property
///   (e.g. Shader Graph with a Saturation node). Set the property name below.
///   Option B (fallback): Enable 'useFallbackColorShift' — the script lerps
///   _Color toward grey instead. Zero shader setup required.
/// </summary>
public class DesaturationEffect : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The stage that triggers this effect. Set to BrokenGrass.")]
    [SerializeField] private Stage triggerStage = Stage.BrokenGrass;

    [Header("Timing")]
    [Tooltip("Duration of the full desaturation. Match to the verse clip length.")]
    [SerializeField] private float desaturationDuration = 12f;

    [Tooltip("Seconds to wait after arrival before desaturation begins.")]
    [SerializeField] private float delayBeforeStart = 1f;

    [Header("Shader (Option A — preferred)")]
    [Tooltip("Float property name on your shader that controls saturation.")]
    [SerializeField] private string saturationPropertyName = "_Saturation";

    [Tooltip("Saturation at full colour. Usually 1.")]
    [SerializeField] private float fullSaturation = 1f;

    [Tooltip("Saturation when fully desaturated. Usually 0.")]
    [SerializeField] private float desaturatedValue = 0f;

    [Header("Fallback (Option B — no custom shader needed)")]
    [Tooltip("If true, lerps material _Color toward grey instead of driving a shader property.")]
    [SerializeField] private bool useFallbackColorShift = false;

    [Tooltip("Target colour when fully desaturated. Cold grey-green suits a dead lawn.")]
    [SerializeField] private Color desaturatedColor = new Color(0.65f, 0.68f, 0.65f);

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------

    private Renderer _renderer;
    private Material _material;        // Instance material — safe to mutate
    private Coroutine _effectCoroutine;
    private Color _originalColor;
    private bool _hasSaturationProperty;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError($"[DesaturationEffect] No Renderer on {gameObject.name}.");
            enabled = false;
            return;
        }

        _material = _renderer.material;   // Creates instance automatically
        _originalColor = _material.color;
        _hasSaturationProperty = _material.HasProperty(saturationPropertyName);

        if (!useFallbackColorShift && !_hasSaturationProperty)
            Debug.LogWarning($"[DesaturationEffect] Shader has no '{saturationPropertyName}'. " +
                             "Enable 'useFallbackColorShift' or use a shader with that property.");
    }

    private void OnEnable()  => GameManager.OnStageChanged += OnStageChanged;
    private void OnDisable() => GameManager.OnStageChanged -= OnStageChanged;

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        if (newStage != triggerStage)
        {
            StopEffect();
            RestoreSaturation();
        }
        // Entering triggerStage: wait for anchor arrival, not stage change.
    }

    /// <summary>
    /// Wire this up in BrokenSceneController.OnPlayerArrivedAtStage(),
    /// same pattern as your other broken-stage effects.
    /// </summary>
    public void OnPlayerArrivedAtStage(Stage stage)
    {
        if (stage != triggerStage) return;

        Log($"Player arrived — desaturating over {desaturationDuration}s.");
        StopEffect();
        _effectCoroutine = StartCoroutine(DesaturationCoroutine());
    }

    // -------------------------------------------------------------------------

    private IEnumerator DesaturationCoroutine()
    {
        if (delayBeforeStart > 0f)
            yield return new WaitForSeconds(delayBeforeStart);

        float elapsed = 0f;
        while (elapsed < desaturationDuration)
        {
            elapsed += Time.deltaTime;
            ApplySaturation(Mathf.Clamp01(elapsed / desaturationDuration));
            yield return null;
        }

        ApplySaturation(1f);
        _effectCoroutine = null;
        Log("Desaturation complete.");
    }

    // t = 0 → full colour, t = 1 → fully desaturated
    private void ApplySaturation(float t)
    {
        if (useFallbackColorShift || !_hasSaturationProperty)
            _material.color = Color.Lerp(_originalColor, desaturatedColor, t);
        else
            _material.SetFloat(saturationPropertyName, Mathf.Lerp(fullSaturation, desaturatedValue, t));
    }

    private void RestoreSaturation()
    {
        if (_material == null) return;
        if (useFallbackColorShift || !_hasSaturationProperty)
            _material.color = _originalColor;
        else
            _material.SetFloat(saturationPropertyName, fullSaturation);
        Log("Saturation restored.");
    }

    private void StopEffect()
    {
        if (_effectCoroutine != null) { StopCoroutine(_effectCoroutine); _effectCoroutine = null; }
    }

    private void Log(string msg) { if (verboseLogging) Debug.Log($"[DesaturationEffect] {msg}"); }
}