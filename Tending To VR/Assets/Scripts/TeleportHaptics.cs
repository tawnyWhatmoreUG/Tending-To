using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Triggers haptic feedback on both VR controllers whenever the player
/// teleports to a stage anchor.
///
/// SETUP:
///   - Add to any persistent GameObject in the scene (e.g. alongside GameManager).
///   - No controller references needed — haptics are sent via UnityEngine.XR.InputDevices,
///     which works for any OpenXR / Oculus device automatically.
///   - Optionally, add entries to 'stageOverrides' to customise the haptic
///     amplitude and duration per stage. Stages without an override use the
///     default values.
///
/// HOW IT WORKS:
///   At start-up this script subscribes to the 'teleporting' event on every
///   TeleportationAnchor found in the scene. When any anchor fires, it reads
///   the current stage from GameManager, looks up the haptic parameters, and
///   calls SendHapticImpulse on both the left and right XRNode hand devices.
/// </summary>
public class TeleportHaptics : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static TeleportHaptics Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Default Haptic Settings")]
    [Tooltip("Haptic motor amplitude used when no stage override is defined (0–1).")]
    [SerializeField, Range(0f, 1f)] private float defaultAmplitude = 0.5f;

    [Tooltip("Duration in seconds used when no stage override is defined.")]
    [SerializeField, Min(0f)] private float defaultDuration = 0.2f;

    [Header("Per-Stage Overrides")]
    [Tooltip("Optional per-stage haptic parameters. " +
             "Stages not listed here use the default amplitude and duration.")]
    [SerializeField] private StageHapticOverride[] stageOverrides;

    [System.Serializable]
    public struct StageHapticOverride
    {
        public Stage stage;
        [Range(0f, 1f)] public float amplitude;
        [Min(0f)] public float duration;
    }

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
    }

    private void OnEnable()
    {
        var anchors = FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None);
        foreach (var anchor in anchors)
            anchor.teleporting.AddListener(OnTeleported);
    }

    private void OnDisable()
    {
        var anchors = FindObjectsByType<TeleportationAnchor>(FindObjectsSortMode.None);
        foreach (var anchor in anchors)
            anchor.teleporting.RemoveListener(OnTeleported);
    }

    // -------------------------------------------------------------------------
    // Teleport Handler
    // -------------------------------------------------------------------------

    private void OnTeleported(TeleportingEventArgs args)
    {
        Stage currentStage = GameManager.Instance != null
            ? GameManager.Instance.CurrentStage
            : default;

        GetHapticParams(currentStage, out float amplitude, out float duration);

        Debug.Log($"[TeleportHaptics] Playing haptics for stage {currentStage} " +
                  $"(amplitude: {amplitude}, duration: {duration}s)");

        SendImpulse(amplitude, duration);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void GetHapticParams(Stage stage, out float amplitude, out float duration)
    {
        if (stageOverrides != null)
        {
            foreach (var entry in stageOverrides)
            {
                if (entry.stage == stage)
                {
                    amplitude = entry.amplitude;
                    duration  = entry.duration;
                    return;
                }
            }
        }

        amplitude = defaultAmplitude;
        duration  = defaultDuration;
    }

    private void SendImpulse(float amplitude, float duration)
    {
        SendImpulseToNode(XRNode.LeftHand,  amplitude, duration);
        SendImpulseToNode(XRNode.RightHand, amplitude, duration);
    }

    private static void SendImpulseToNode(XRNode node, float amplitude, float duration)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);

        foreach (var device in devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}
