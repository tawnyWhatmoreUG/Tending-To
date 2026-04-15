using System.Collections;
using UnityEngine;

/// <summary>
/// Cracked dry ground effect for the BrokenFlowers stage.
///
/// Animates ground cracks appearing on arrival and transitions the soil
/// material from healthy to dry/desaturated.
///
/// HOW IT WORKS:
///   Two layered techniques run simultaneously on arrival:
///     1. MATERIAL TRANSITION — lerps a "_Saturation" float and a "_DryBlend"
///        float on the ground material from fertile (1.0, 0.0) to dry (0.0, 1.0).
///        Compatible with a custom Shader Graph (see notes below).
///     2. CRACK ANIMATION — enables a crack overlay mesh/decal and grows it
///        from scale 0 → 1 with a configurable ease curve, giving the
///        impression of cracks spreading outward from the player.
///
/// SETUP:
///   1. Add this script to the ground GameObject (or a manager GO).
///   2. Assign groundRenderer — the MeshRenderer for the soil/flower-bed ground.
///   3. Assign crackOverlayObject — a child GameObject sitting just above the
///      ground plane that holds the crack mesh or decal projector.
///      - Scale it to (0, 0, 0) in the scene; this script animates it to (1, 1, 1).
///      - Its material should be an alpha-blended crack texture.
///   4. Assign crackParticleSystem (optional) — a burst particle system for
///      dust/dirt puffs when cracks first appear.
///   5. Subscribe to OnEffectComplete if you need a callback when done.
///
/// SHADER GRAPH SETUP (for groundRenderer material):
///   Your ground Shader Graph needs two exposed properties:
///     - "_Saturation"  Float  (default 1.0)   — drives a Hue/Saturation node
///     - "_DryBlend"    Float  (default 0.0)   — lerps in a dry/cracked albedo texture
///   If you are using a simpler material without these properties, the script
///   gracefully skips the property it can't find and only animates what exists.
///
/// INTEGRATION WITH StageSequencer:
///   StageSequencer.OnPlayerArrivedAtAnchor fires for BrokenFlowers.
///   Either call TriggerEffect() directly from a BrokenSceneController subclass,
///   or subscribe to GameManager.OnStageChanged here (see OnEnable/OnDisable below).
/// </summary>
public class CrackedGroundEffect : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static CrackedGroundEffect Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector — Renderers & Objects
    // -------------------------------------------------------------------------

    [Header("Ground")]
    [Tooltip("The MeshRenderer for the soil / flower-bed ground plane.")]
    [SerializeField] private MeshRenderer groundRenderer;

    [Tooltip("GameObject holding the crack mesh or decal projector. " +
             "Should start at localScale (0, 0, 0) in the scene.")]
    [SerializeField] private GameObject crackOverlayObject;

    [Tooltip("Optional particle system for dust burst when cracks appear.")]
    [SerializeField] private ParticleSystem crackDustParticles;

    // -------------------------------------------------------------------------
    // Inspector — Material Property Names
    // -------------------------------------------------------------------------

    [Header("Material Properties")]
    [Tooltip("Shader property name for saturation (1 = full colour, 0 = greyscale).")]
    [SerializeField] private string saturationProperty = "_Saturation";

    [Tooltip("Shader property name for dry texture blend (0 = fertile, 1 = dry).")]
    [SerializeField] private string dryBlendProperty = "_DryBlend";

    // -------------------------------------------------------------------------
    // Inspector — Timing & Curves
    // -------------------------------------------------------------------------

    [Header("Timing")]
    [Tooltip("Seconds to wait after arrival before cracks start appearing.")]
    [SerializeField] private float delayOnArrival = 0.4f;

    [Tooltip("Total duration of the crack spread animation.")]
    [SerializeField] private float crackSpreadDuration = 2.2f;

    [Tooltip("Total duration of the material desaturation transition.")]
    [SerializeField] private float desaturationDuration = 3.5f;

    [Header("Curves")]
    [Tooltip("Easing curve for crack overlay scale (x = time 0-1, y = scale 0-1). " +
             "Try a curve that holds at 0, then rises sharply for a snap-crack feel.")]
    [SerializeField] private AnimationCurve crackSpreadCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 4f),
        new Keyframe(0.3f, 0.85f, 0.5f, 0.5f),
        new Keyframe(0.6f, 0.95f, 0.1f, 0.1f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Tooltip("Easing curve for material desaturation (x = time 0-1, y = blend 0-1).")]
    [SerializeField] private AnimationCurve desaturationCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 0.3f),
        new Keyframe(1f, 1f)
    );

    // -------------------------------------------------------------------------
    // Inspector — Debug
    // -------------------------------------------------------------------------

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private bool triggerOnStart = false; // For quick previewing in Editor

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when the full effect (crack spread + desaturation) is complete.
    /// </summary>
    public event System.Action OnEffectComplete;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Material _groundMaterial;
    private bool _hasSaturationProp;
    private bool _hasDryBlendProp;
    private bool _effectTriggered = false;
    private Coroutine _effectCoroutine;
    private Vector3 _crackOverlayFullScale = Vector3.one;

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

        InitialiseMaterial();
        InitialiseCrackOverlay();
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
        if (triggerOnStart)
            TriggerEffect();

        // If the game is already past BrokenFlowers when this scene loads, show cracked state.
        if (GameManager.Instance != null && GameManager.Instance.CurrentStage >= Stage.BrokenFlowers)
            ShowCrackedImmediate();
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        if (newStage == Stage.BrokenFlowers)
        {
            // BrokenFlowers has no teleport anchor — effect triggers immediately
            // when the stage begins (matching StageSequencer's auto-poem behaviour).
            TriggerEffect();
        }
        else if (newStage > Stage.BrokenFlowers)
        {
            // Past BrokenFlowers — ensure cracked state is visible immediately.
            ShowCrackedImmediate();
        }
        // Stages before BrokenFlowers: do nothing — cracked earth is never reset.
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Triggers the full cracked-ground effect sequence.
    /// Safe to call multiple times — subsequent calls are ignored.
    /// </summary>
    public void TriggerEffect()
    {
        if (_effectTriggered)
        {
            Log("Effect already triggered — ignoring duplicate call.");
            return;
        }

        _effectTriggered = true;
        Log("Triggering cracked ground effect.");

        if (_effectCoroutine != null)
            StopCoroutine(_effectCoroutine);

        _effectCoroutine = StartCoroutine(RunEffect());
    }

    /// <summary>
    /// Resets the ground to its fertile state immediately.
    /// Called when jumping away from this stage (debug or sequence end).
    /// </summary>
    public void ResetToFertile()
    {
        _effectTriggered = false;

        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }

        // Restore material properties.
        if (_groundMaterial != null)
        {
            if (_hasSaturationProp) _groundMaterial.SetFloat(saturationProperty, 1f);
            if (_hasDryBlendProp)   _groundMaterial.SetFloat(dryBlendProperty, 0f);
        }

        // Hide crack overlay.
        if (crackOverlayObject != null)
        {
            crackOverlayObject.transform.localScale = Vector3.zero;
            crackOverlayObject.SetActive(false);
        }

        Log("Reset to fertile state.");
    }

    /// <summary>
    /// Instantly applies the fully cracked state without animation.
    /// Used when arriving at a stage past BrokenFlowers.
    /// </summary>
    public void ShowCrackedImmediate()
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }

        _effectTriggered = true;

        if (_groundMaterial != null)
        {
            if (_hasSaturationProp) _groundMaterial.SetFloat(saturationProperty, 0f);
            if (_hasDryBlendProp)   _groundMaterial.SetFloat(dryBlendProperty, 1f);
        }

        if (crackOverlayObject != null)
        {
            crackOverlayObject.SetActive(true);
            crackOverlayObject.transform.localScale = _crackOverlayFullScale;
        }

        Log("Cracked state applied immediately.");
    }

    // -------------------------------------------------------------------------
    // Effect Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator RunEffect()
    {
        // --- 1. Brief pause after arrival ---
        yield return new WaitForSeconds(delayOnArrival);

        // --- 2. Enable crack overlay (starts at scale 0) ---
        if (crackOverlayObject != null)
        {
            crackOverlayObject.SetActive(true);
            crackOverlayObject.transform.localScale = Vector3.zero;
        }

        // Trigger dust particle burst at the start of crack spread.
        if (crackDustParticles != null)
            crackDustParticles.Play();

        // --- 3. Animate crack spread and desaturation in parallel ---
        float crackElapsed = 0f;
        float desatElapsed = 0f;
        bool crackDone = false;
        bool desatDone = false;

        while (!crackDone || !desatDone)
        {
            float dt = Time.deltaTime;

            // Crack spread
            if (!crackDone)
            {
                crackElapsed += dt;
                float t = Mathf.Clamp01(crackElapsed / crackSpreadDuration);
                float scale = crackSpreadCurve.Evaluate(t);

                if (crackOverlayObject != null)
                    crackOverlayObject.transform.localScale = _crackOverlayFullScale * scale;

                if (t >= 1f) crackDone = true;
            }

            // Desaturation
            if (!desatDone)
            {
                desatElapsed += dt;
                float t = Mathf.Clamp01(desatElapsed / desaturationDuration);
                float blend = desaturationCurve.Evaluate(t);
                float saturation = 1f - blend;

                if (_groundMaterial != null)
                {
                    if (_hasSaturationProp) _groundMaterial.SetFloat(saturationProperty, saturation);
                    if (_hasDryBlendProp)   _groundMaterial.SetFloat(dryBlendProperty, blend);
                }

                if (t >= 1f) desatDone = true;
            }

            yield return null;
        }

        // --- 4. Ensure final values are exact ---
        if (crackOverlayObject != null)
            crackOverlayObject.transform.localScale = _crackOverlayFullScale;

        if (_groundMaterial != null)
        {
            if (_hasSaturationProp) _groundMaterial.SetFloat(saturationProperty, 0f);
            if (_hasDryBlendProp)   _groundMaterial.SetFloat(dryBlendProperty, 1f);
        }

        _effectCoroutine = null;
        Log("Cracked ground effect complete.");
        OnEffectComplete?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    private void InitialiseMaterial()
    {
        if (groundRenderer == null)
        {
            Debug.LogWarning("[CrackedGroundEffect] groundRenderer not assigned — material transition will be skipped.");
            return;
        }

        // Create an instance so we don't modify the shared material asset.
        _groundMaterial = groundRenderer.material;

        _hasSaturationProp = _groundMaterial.HasProperty(saturationProperty);
        _hasDryBlendProp   = _groundMaterial.HasProperty(dryBlendProperty);

        if (!_hasSaturationProp)
            Debug.LogWarning($"[CrackedGroundEffect] Material does not have property '{saturationProperty}'. " +
                             $"Saturation transition will be skipped. Check your Shader Graph.");

        if (!_hasDryBlendProp)
            Debug.LogWarning($"[CrackedGroundEffect] Material does not have property '{dryBlendProperty}'. " +
                             $"Dry blend transition will be skipped. Check your Shader Graph.");

        // Ensure starting values are fertile.
        if (_hasSaturationProp) _groundMaterial.SetFloat(saturationProperty, 1f);
        if (_hasDryBlendProp)   _groundMaterial.SetFloat(dryBlendProperty, 0f);
    }

    private void InitialiseCrackOverlay()
    {
        if (crackOverlayObject == null)
        {
            Debug.LogWarning("[CrackedGroundEffect] crackOverlayObject not assigned — crack spread animation will be skipped.");
            return;
        }

        // Capture the authored "full" scale so animation returns to the intended size.
        _crackOverlayFullScale = crackOverlayObject.transform.localScale;

        // Start hidden and at zero scale.
        crackOverlayObject.transform.localScale = Vector3.zero;
        crackOverlayObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[CrackedGroundEffect] {message}");
    }

    // -------------------------------------------------------------------------
    // Editor Preview
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    [UnityEngine.ContextMenu("Preview Effect")]
    private void PreviewEffect()
    {
        ResetToFertile();
        TriggerEffect();
    }

    [UnityEngine.ContextMenu("Reset to Fertile")]
    private void EditorReset()
    {
        ResetToFertile();
    }
#endif
}
