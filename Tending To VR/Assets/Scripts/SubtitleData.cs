using UnityEngine;

/// <summary>
/// Holds the subtitle text and per-line timing for one poem verse.
/// Create one asset per stage that has a verse via Assets > Create > TendingTo > SubtitleData.
///
/// TIMING SETUP:
///   Set lineStartTimes[i] to the number of seconds into the audio clip when line i should appear.
///   Use the "Fill Even Timing" context menu as a starting point:
///     1. Set Clip Length For Timing to the length of the verse audio clip in seconds.
///     2. Right-click this asset in the Inspector → Fill Even Timing.
///     3. Adjust individual values by ear against the narration audio.
/// </summary>
[CreateAssetMenu(fileName = "SubtitleData", menuName = "TendingTo/SubtitleData")]
public class SubtitleData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The stage this subtitle asset belongs to.")]
    public Stage stage;

    [Header("Text")]
    [Tooltip("Each element is one line of poem text, displayed in sequence.")]
    public string[] lines;

    [Header("Timing")]
    [Tooltip("Seconds from the start of the verse audio clip when each line should appear. " +
             "Must match the length of Lines.")]
    public float[] lineStartTimes;

    [Header("Appearance")]
    [Tooltip("Colour of the subtitle text. Use to visually distinguish action verses (white) " +
             "from reverse verses (e.g. a desaturated or cooler tone).")]
    public Color textColor = Color.white;

    // -------------------------------------------------------------------------
    // Editor Utility
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    [Header("Timing Utility")]
    [Tooltip("Set this to the verse audio clip length in seconds, then right-click → Fill Even Timing.")]
    public float clipLengthForTiming = 20f;

    [ContextMenu("Fill Even Timing")]
    private void FillEvenTiming()
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("[SubtitleData] No lines defined — add lines first.");
            return;
        }

        lineStartTimes = new float[lines.Length];
        float interval = clipLengthForTiming / lines.Length;
        for (int i = 0; i < lines.Length; i++)
            lineStartTimes[i] = i * interval;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[SubtitleData] Filled even timing: {lines.Length} lines over {clipLengthForTiming}s " +
                  $"({interval:F2}s per line). Tune individual values against the narration audio.");
    }
#endif
}
