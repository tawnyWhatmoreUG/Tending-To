using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// The note sitting on the table at the start of the experience.
/// When the player ray-clicks it during PendingToDo, it:
///   1. Reveals the Wrist Menu canvas on the player's left hand.
///   2. Hides itself (the note is "picked up").
///   3. Tells GameManager to advance directly to CutTheGrass.
///
/// SETUP:
///   - Add this script to the note GameObject (your Blender model or plane).
///   - Add an XRSimpleInteractable component to the same GameObject.
///   - Add a Collider (MeshCollider or BoxCollider) — required for ray hit detection.
///   - The note should be active in the scene at Start (GameManager begins
///     at PendingToDo so the note is the first thing the player sees).
///
/// HIERARCHY:
///   Note_OnTable
///     ├── MeshFilter + MeshRenderer  (your paper model)
///     ├── Collider                   (BoxCollider or MeshCollider)
///     ├── XRSimpleInteractable       (XRI built-in)
///     └── NoteOnTable.cs             (this script)
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class NoteOnTable : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Note Visual")]
    [Tooltip("The renderer to hide when the note is picked up. " +
             "Defaults to the MeshRenderer on this GameObject if left empty.")]
    [SerializeField] private Renderer noteRenderer;

    [Tooltip("If true the whole GameObject is deactivated on pickup. " +
             "If false only the renderer and collider are disabled, " +
             "which is safer if other scripts hold a reference to this GO.")]
    [SerializeField] private bool deactivateOnPickup = true;

    [Header("Audio")]
    [Tooltip("Audio clip to play when the note is picked up.")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Volume at which to play the pickup sound (0-1).")]
    [SerializeField] private float pickupSoundVolume = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // -------------------------------------------------------------------------
    // Private Refs
    // -------------------------------------------------------------------------

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;
    private Collider _collider;
    private AudioSource _audioSource;
    private bool _hasBeenPickedUp = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        _collider = GetComponent<Collider>();
        _audioSource = GetComponent<AudioSource>();

        if (noteRenderer == null)
            noteRenderer = GetComponent<Renderer>();

        // Hook into XRI's select event (ray click).
        _interactable.selectEntered.AddListener(OnNoteSelected);
    }

    private void OnEnable()
    {
        GameManager.OnStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageChanged -= OnStageChanged;

        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnNoteSelected);
    }

    // -------------------------------------------------------------------------
    // Stage Guard
    // -------------------------------------------------------------------------

    /// <summary>
    /// If the stage somehow changes away from PendingToDo without the note
    /// being clicked (e.g. a debug jump), disable the note so it isn't
    /// sitting on the table during later stages.
    /// </summary>
    private void OnStageChanged(Stage newStage)
    {
        if (newStage == Stage.PendingToDo)
        {
            // Debug jump back to start — re-enable the note.
            _hasBeenPickedUp = false;
            gameObject.SetActive(true);
        }
        else if (!_hasBeenPickedUp)
        {
            // Jumped past PendingToDo without picking up — hide the note.
            HideNote();
        }
    }

    // -------------------------------------------------------------------------
    // Interaction Handler
    // -------------------------------------------------------------------------

    private void OnNoteSelected(SelectEnterEventArgs args)
    {
        // Only respond during PendingToDo.
        if (GameManager.Instance?.CurrentStage != Stage.PendingToDo)
        {
            Log("Note selected but not in PendingToDo stage — ignoring.");
            return;
        }

        if (_hasBeenPickedUp)
        {
            Log("Note already picked up — ignoring duplicate event.");
            return;
        }

        Log("Note picked up.");
        _hasBeenPickedUp = true;

        // Play pickup sound.
        PlayPickupSound();

        // 1. Reveal the wrist canvas.
        WristCanvas.Instance?.ShowCanvas();

        // 2. Hide the note.
        HideNote();

        // 3. Advance directly to CutTheGrass.
        GameManager.Instance?.AdvanceStageDirect();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void HideNote()
    {
        if (deactivateOnPickup)
        {
            gameObject.SetActive(false);
        }
        else
        {
            if (noteRenderer != null) noteRenderer.enabled = false;
            if (_collider != null) _collider.enabled = false;
            if (_interactable != null) _interactable.enabled = false;
        }
    }

    private void PlayPickupSound()
    {
        if (pickupSound == null)
        {
            Log("Pickup sound not assigned.");
            return;
        }

        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
        }
        else
        {
            Log("No AudioSource component found. Assign one to play sound.");
        }
    }

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[NoteOnTable] {message}");
    }
}
