using System.Collections;
using UnityEngine;

/// <summary>
/// Fades a Renderer's material from fully opaque to fully transparent
/// when the player teleports to a specified stage.
///
/// HOW IT WORKS:
///   Listens for GameManager.OnStageChanged. When the target stage is reached
///   (i.e. the player has teleported there), it fades the material alpha from
///   1 (opaque) to 0 (transparent) over a configurable duration.
///   Optionally disables the Renderer entirely once fully transparent.
///
/// MATERIAL SETUP (required — see below):
///   Your material MUST use a shader that supports transparency.
///   The recommended setup for built-in RP, URP, or HDRP is described
///   in the Inspector tooltip and the README comment at the bottom of this file.
///
/// SETUP:
///   1. Create and configure your material (see MATERIAL SETUP section below).
///   2. Add this script to the GameObject whose Renderer you want to fade.
///   3. Assign the Renderer in the Inspector (or leave blank to auto-detect).
///   4. Set 'triggerStage' to the Stage when the fade should begin.
///   5. Optionally assign a second material index if the mesh has multiple materials.
///
/// NOTE ON INSTANCING:
///   This script uses renderer.material (not sharedMaterial) so it creates a
///   per-instance copy. This is intentional — it won't affect other objects
///   using the same material asset.
/// </summary>
public class GlassRevealFader : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Stage Trigger")]
    [Tooltip("The stage at which this object begins fading to transparent.")]
    [SerializeField] private Stage triggerStage;

    [Tooltip("If true, resets to opaque when any earlier stage begins " +
             "(useful when debug-jumping stages).")]
    [SerializeField] private bool resetOnEarlierStage = true;

    [Header("Renderer")]
    [Tooltip("The Renderer whose material will be faded. " +
             "Leave empty to auto-detect from this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Index of the material on the Renderer to fade (0 = first/only material).")]
    [SerializeField] private int materialIndex = 0;

    [Header("Fade Settings")]
    [Tooltip("How long in seconds the fade from opaque to transparent takes.")]
    [SerializeField] private float fadeDuration = 2f;

    [Tooltip("Delay in seconds after the stage change before the fade begins. " +
             "Useful if you want a beat of pause after teleporting.")]
    [SerializeField] private float fadeDelay = 0f;

    [Tooltip("If true, disables the Renderer entirely once alpha reaches 0. " +
             "Frees GPU overhead for objects that are permanently gone.")]
    [SerializeField] private bool disableRendererWhenComplete = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Material _instanceMaterial;
    private Coroutine _fadeCoroutine;
    private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");

    // Standard URP/Built-in alpha property names to try, in order of preference.
    // We probe these at start to find the right one for your shader.
    private static readonly string[] AlphaCandidates =
    {
        "_Alpha",           // Custom shaders
        "_BaseColor",       // URP Lit / Unlit (Color.a)
        "_Color",           // Built-in Standard (Color.a)
        "_TintColor",       // Particles
    };

    private string _resolvedColorProperty = null;
    private bool _usesColorProperty = false; // true = fade via Color.a, false = direct float

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[GlassRevealFader] No Renderer found on {name}. Assign one in the Inspector.");
            enabled = false;
            return;
        }

        // Create a per-instance material so we don't affect shared assets.
        _instanceMaterial = targetRenderer.materials[materialIndex];

        // Resolve which property to animate.
        ResolveAlphaProperty();

        // Ensure the object starts fully opaque.
        SetAlpha(1f);
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    // -------------------------------------------------------------------------
    // Stage Listener
    // -------------------------------------------------------------------------

    private void OnStageChanged(Stage newStage)
    {
        if (newStage == triggerStage)
        {
            // Player has arrived at our stage — begin fading.
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeRoutine());
            return;
        }

        // If we've jumped to an earlier stage (debug or reset), restore opacity.
        if (resetOnEarlierStage && (int)newStage < (int)triggerStage)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (targetRenderer != null)
                targetRenderer.enabled = true;

            SetAlpha(1f);
            Log($"Reset to opaque (jumped back to {newStage}).");
        }
    }

    // -------------------------------------------------------------------------
    // Fade Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FadeRoutine()
    {
        Log($"Fade triggered by stage: {triggerStage}. Delay: {fadeDelay}s, Duration: {fadeDuration}s.");

        if (fadeDelay > 0f)
            yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);

        if (disableRendererWhenComplete && targetRenderer != null)
        {
            targetRenderer.enabled = false;
            Log("Renderer disabled after fade complete.");
        }

        _fadeCoroutine = null;
        Log("Fade to transparent complete.");
    }

    // -------------------------------------------------------------------------
    // Alpha Helpers
    // -------------------------------------------------------------------------

    private void SetAlpha(float alpha)
    {
        if (_instanceMaterial == null) return;

        if (_usesColorProperty && _resolvedColorProperty != null)
        {
            Color c = _instanceMaterial.GetColor(_resolvedColorProperty);
            c.a = alpha;
            _instanceMaterial.SetColor(_resolvedColorProperty, c);
        }
        else if (!_usesColorProperty && _resolvedColorProperty != null)
        {
            _instanceMaterial.SetFloat(_resolvedColorProperty, alpha);
        }
        else
        {
            // Fallback: try _BaseColor directly.
            if (_instanceMaterial.HasProperty("_BaseColor"))
            {
                Color c = _instanceMaterial.GetColor("_BaseColor");
                c.a = alpha;
                _instanceMaterial.SetColor("_BaseColor", c);
            }
        }
    }

    /// <summary>
    /// Probes the material's shader for known alpha-controlling properties
    /// and records which one to use at runtime.
    /// </summary>
    private void ResolveAlphaProperty()
    {
        if (_instanceMaterial == null) return;

        // Check for a direct float "_Alpha" first.
        if (_instanceMaterial.HasProperty("_Alpha"))
        {
            _resolvedColorProperty = "_Alpha";
            _usesColorProperty = false;
            Log($"Using float property: _Alpha");
            return;
        }

        // Otherwise look for Color properties and use their .a channel.
        string[] colorCandidates = { "_BaseColor", "_Color", "_TintColor" };
        foreach (string prop in colorCandidates)
        {
            if (_instanceMaterial.HasProperty(prop))
            {
                _resolvedColorProperty = prop;
                _usesColorProperty = true;
                Log($"Using color property: {prop} (alpha channel)");
                return;
            }
        }

        Debug.LogWarning($"[GlassRevealFader] Could not find a recognised alpha property on material " +
                         $"'{_instanceMaterial.name}'. Fading will have no effect. " +
                         $"Ensure your shader exposes _BaseColor (URP), _Color (Built-in), or _Alpha.");
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Manually trigger the fade. Useful for testing or non-stage-driven fades.
    /// </summary>
    public void TriggerFade()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    /// <summary>
    /// Immediately resets the material to fully opaque.
    /// </summary>
    public void ResetToOpaque()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (targetRenderer != null)
            targetRenderer.enabled = true;

        SetAlpha(1f);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[GlassRevealFader] {message}");
    }
}

/*
================================================================================
MATERIAL SETUP — READ THIS BEFORE USING
================================================================================

The shader you use MUST support transparency. Here's how to set it up
for each render pipeline:

── URP (Universal Render Pipeline) ─────────────────────────────────────────────

  1. Create a new Material.
  2. Shader: Universal Render Pipeline/Lit  (or /Unlit for an unlit look)
  3. In the Material Inspector:
       Surface Type  →  Transparent
       Blending Mode →  Alpha
  4. Set the Base Map colour's alpha to 255 (fully opaque) to start.
  5. The script will animate the _BaseColor.a channel.

── Built-in Render Pipeline ─────────────────────────────────────────────────────

  1. Create a new Material.
  2. Shader: Standard
  3. Rendering Mode → Transparent   (NOT Fade — Transparent preserves specular)
  4. Set the Albedo colour's alpha to 255 to start.
  5. The script will animate the _Color.a channel.

── HDRP ─────────────────────────────────────────────────────────────────────────

  1. Create a new Material.
  2. Shader: HDRP/Lit
  3. Surface Type → Transparent
  4. The script will animate the _BaseColor.a channel.

── Custom / Shader Graph ─────────────────────────────────────────────────────────

  Expose a float property named "_Alpha" (reference name: _Alpha) and drive
  your output alpha from it. The script will detect and animate it directly.

================================================================================
*/