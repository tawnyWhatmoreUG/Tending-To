#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// EDITOR ONLY — stripped from builds entirely.
///
/// Listens for stage changes triggered by DebugStageController and restores
/// the soundscape to the correct state for that stage.
///
/// Without this, jumping back to CutTheGrass after DoTheWindowBox would
/// leave all layers silent even though only layer 0 should be gone.
///
/// SETUP:
///   Add to the same GameObject as AudioManager.
///   No additional Inspector setup required.
/// </summary>
public class AudioManagerDebugHelper : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;
    }

    private void OnStageChanged(Stage newStage)
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;

        // Restore all layers to full volume first.
        am.RestoreAll();

        // Then fade out any layers that should already be gone
        // by the time we reach this stage.
        // Each action stage removes the layer defined in its StageData.
        // We replay all removals for stages that come before newStage.
        int newStageIndex = (int)newStage;

        for (int i = 0; i < newStageIndex; i++)
        {
            Stage pastStage = (Stage)i;
            StageData data = GameManager.Instance?.GetStageData(pastStage);
            if (data == null) continue;

            if (data.soundscapeLayerToRemove >= 0)
            {
                // Silence immediately (no fade) since this is a debug jump.
                am.FadeOutLayer(data.soundscapeLayerToRemove);
            }
        }

        Debug.Log($"[AudioManagerDebugHelper] Soundscape restored for stage: {newStage}");
    }
}
#endif
