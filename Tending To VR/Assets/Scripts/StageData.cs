using UnityEngine;

/// <summary>
/// Defines all data associated with a single game stage.
/// Create one asset per stage via Assets > Create > TendingTo > StageData.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "TendingTo/StageData")]
public class StageData : ScriptableObject
{
    [Header("Identity")]
    public Stage stage;

    [Tooltip("If true, GameManager waits for BOTH verse completion AND interaction completion " +
             "before advancing. Set false for Broken stages (verse-only) and PendingToDo (direct advance).")]
    public bool requiresInteraction = true;

    [Header("Poem")]
    [Tooltip("The audio clip for this stage's poem verse. Leave null for PendingToDo.")]
    public AudioClip poemVerse;

    [Header("Soundscape")]
    [Tooltip("Index into AudioManager's soundscape layers array to fade out on this stage. -1 = no removal.")]
    public int soundscapeLayerToRemove = -1;

    [Header("Wrist Canvas")]
    [Tooltip("Index of the scribble/todo item to mark on the WristCanvas. -1 = no change.")]
    public int wristCanvasScribbleIndex = -1;

    [Header("Teleport")]
    [Tooltip("Does this stage have a teleport anchor that should activate when the stage begins?")]
    public bool hasTeleportAnchor = true;
}
