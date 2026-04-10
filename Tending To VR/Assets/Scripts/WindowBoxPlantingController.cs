using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the window box planting interaction.
///
/// SETUP SUMMARY
/// =============
/// 1. Place this script on a central GameObject in the scene (e.g., "PlantingInteractionManager").
///
/// 2. In the Inspector, assign:
///    Plants (3 separate plant objects)
///      Plant 1 Root             — the root Transform of Plant 1
///      Plant 1 Pot Mesh         — the pot mesh GameObject (stays behind when picked up)
///      Plant 1 Plant And Soil   — the plant + soil GameObject (gets picked up)
///      Plant 1 Grip Point       — empty Transform marking where the hand grabs
///      
///      (Repeat for Plant 2 and Plant 3)
///
///    Window Box
///      Window Box Transform     — the window box GameObject
///      Window Box Floor Position — empty Transform marking where box starts (floor)
///      Window Box Sill Position  — empty Transform marking where box ends up (window sill)
///      Move Duration             — seconds for box to move to sill (default 1.5s)
///
///    Empty Pots Final Positions
///      Pot 1 Drop Position      — empty Transform with rotation for tipped pot 1
///      Pot 2 Drop Position      — empty Transform with rotation for tipped pot 2
///      Pot 3 Drop Position      — empty Transform with rotation for tipped pot 3
///      Pot Drop Duration        — seconds for pots to tip over (default 0.8s)
///
///    Compost Bag
///      Compost Bag Root         — the compost bag GameObject
///
///    VR Hand
///      Hand Ray Interactor      — the XRRayInteractor on the right hand
///      Hand Line Visual         — the XRInteractorLineVisual on the right hand
///      Trigger Action           — Input action for the trigger button
///
///    Plant Alignment
///      Plant Rotation Offset    — Z-axis rotation when plant is equipped (default 0)
///
///    Completion
///      Completion Audio         — AudioSource with the success sound clip
///
/// 3. Setup XRSimpleInteractable components:
///    - On Plant 1's root GameObject, add XRSimpleInteractable
///      Wire "Select Entered" → WindowBoxPlantingController.OnPlant1Selected
///    - Repeat for Plant 2 → OnPlant2Selected
///    - Repeat for Plant 3 → OnPlant3Selected
///    - On the Compost Bag, add XRSimpleInteractable
///      Wire "Select Entered" → WindowBoxPlantingController.OnCompostBagSelected
///    - On the Window Box, add XRSimpleInteractable (only used for placement trigger)
///
/// INTERACTION FLOW
/// ================
///   Step 1: Ray + Select Plant 1  →  plant snaps to hand, pot stays, ray off
///   Step 2: Hover over window box + Trigger  →  plant places in box, ray on
///   Step 3-6: Repeat for Plant 2 and Plant 3
///   Step 7: Ray + Select Compost Bag  →  complete! Box moves to sill, pots tip over, sound
/// </summary>
public class WindowBoxPlantingController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Plant 1")]
    [Tooltip("Root Transform of the first plant assembly.")]
    public Transform plant1Root;
    [Tooltip("The pot mesh GameObject that stays behind when the plant is picked up.")]
    public GameObject plant1PotMesh;
    [Tooltip("The plant + soil GameObject that moves with the player's hand.")]
    public Transform plant1PlantAndSoil;
    [Tooltip("Empty Transform marking the grip point for Plant 1.")]
    public Transform plant1GripPoint;

    [Header("Plant 2")]
    public Transform plant2Root;
    public GameObject plant2PotMesh;
    public Transform plant2PlantAndSoil;
    public Transform plant2GripPoint;

    [Header("Plant 3")]
    public Transform plant3Root;
    public GameObject plant3PotMesh;
    public Transform plant3PlantAndSoil;
    public Transform plant3GripPoint;

    [Header("Window Box")]
    [Tooltip("The window box GameObject that will move to the sill at the end.")]
    public Transform windowBoxTransform;
    [Tooltip("Empty Transform marking the starting position (floor).")]
    public Transform windowBoxFloorPosition;
    [Tooltip("Empty Transform marking the final position (window sill).")]
    public Transform windowBoxSillPosition;
    [Tooltip("Duration (seconds) for the window box to move to the sill.")]
    public float moveDuration = 1.5f;

    [Header("Empty Pots Final Positions")]
    [Tooltip("Empty Transform with rotation for where Pot 1 ends up tipped over.")]
    public Transform pot1DropPosition;
    [Tooltip("Empty Transform with rotation for where Pot 2 ends up tipped over.")]
    public Transform pot2DropPosition;
    [Tooltip("Empty Transform with rotation for where Pot 3 ends up tipped over.")]
    public Transform pot3DropPosition;
    [Tooltip("Duration (seconds) for pots to animate to their tipped positions.")]
    public float potDropDuration = 0.8f;

    [Header("Compost Bag")]
    [Tooltip("The compost bag GameObject.")]
    public Transform compostBagRoot;

    [Header("VR Hand")]
    [Tooltip("XRRayInteractor on the right hand controller.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor handRayInteractor;
    [Tooltip("XRInteractorLineVisual on the right hand controller.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual handLineVisual;
    [Tooltip("Input action for the trigger button (to place plants).")]
    public InputActionProperty triggerAction;

    [Header("Plant Alignment")]
    [Tooltip("Z-axis rotation offset when a plant is equipped (adjust for model orientation).")]
    public float plantRotationOffset = 0f;

    [Header("Completion")]
    [Tooltip("AudioSource containing the success/completion sound clip.")]
    public AudioSource completionAudio;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private enum PlantingState
    {
        WaitingForPlant1,
        Plant1InHand,
        WaitingForPlant2,
        Plant2InHand,
        WaitingForPlant3,
        Plant3InHand,
        WaitingForCompost,
        Completed
    }

    private PlantingState currentState = PlantingState.WaitingForPlant1;

    // Track which plant is currently held
    private Transform currentPlantInHand;
    private Transform currentPlantGripPoint;

    // Original positions for potential reset
    private Vector3 plant1OriginalPos;
    private Quaternion plant1OriginalRot;
    private Transform plant1OriginalParent;

    private Vector3 plant2OriginalPos;
    private Quaternion plant2OriginalRot;
    private Transform plant2OriginalParent;

    private Vector3 plant3OriginalPos;
    private Quaternion plant3OriginalRot;
    private Transform plant3OriginalParent;

    // Hovering over window box
    private bool isHoveringWindowBox = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        // Store original positions of all plants
        if (plant1PlantAndSoil != null)
        {
            plant1OriginalPos = plant1PlantAndSoil.localPosition;
            plant1OriginalRot = plant1PlantAndSoil.localRotation;
            plant1OriginalParent = plant1PlantAndSoil.parent;
        }

        if (plant2PlantAndSoil != null)
        {
            plant2OriginalPos = plant2PlantAndSoil.localPosition;
            plant2OriginalRot = plant2PlantAndSoil.localRotation;
            plant2OriginalParent = plant2PlantAndSoil.parent;
        }

        if (plant3PlantAndSoil != null)
        {
            plant3OriginalPos = plant3PlantAndSoil.localPosition;
            plant3OriginalRot = plant3PlantAndSoil.localRotation;
            plant3OriginalParent = plant3PlantAndSoil.parent;
        }

        // Position window box at floor position
        if (windowBoxTransform != null && windowBoxFloorPosition != null)
        {
            windowBoxTransform.position = windowBoxFloorPosition.position;
            windowBoxTransform.rotation = windowBoxFloorPosition.rotation;
        }
    }

    void Update()
    {
        // Only check for trigger input when a plant is in hand
        if (currentPlantInHand != null && isHoveringWindowBox)
        {
            float triggerValue = triggerAction.action.ReadValue<float>();
            bool isTriggerPressed = triggerValue > 0.1f;

            if (isTriggerPressed)
            {
                PlacePlantInWindowBox();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Plant Selection Methods (wire these to XRSimpleInteractable Select Entered)
    // -------------------------------------------------------------------------

    public void OnPlant1Selected(SelectEnterEventArgs args)
    {
        if (currentState != PlantingState.WaitingForPlant1) return;

        PickupPlant(plant1PlantAndSoil, plant1PotMesh, plant1GripPoint, args);
        currentState = PlantingState.Plant1InHand;
    }

    public void OnPlant2Selected(SelectEnterEventArgs args)
    {
        if (currentState != PlantingState.WaitingForPlant2) return;

        PickupPlant(plant2PlantAndSoil, plant2PotMesh, plant2GripPoint, args);
        currentState = PlantingState.Plant2InHand;
    }

    public void OnPlant3Selected(SelectEnterEventArgs args)
    {
        if (currentState != PlantingState.WaitingForPlant3) return;

        PickupPlant(plant3PlantAndSoil, plant3PotMesh, plant3GripPoint, args);
        currentState = PlantingState.Plant3InHand;
    }

    // -------------------------------------------------------------------------
    // Compost Bag Selection (final step)
    // -------------------------------------------------------------------------

    public void OnCompostBagSelected(SelectEnterEventArgs args)
    {
        if (currentState != PlantingState.WaitingForCompost) return;

        StartCoroutine(CompleteInteraction());
    }

    // -------------------------------------------------------------------------
    // Window Box Hover Detection (wire these to XRSimpleInteractable on window box)
    // -------------------------------------------------------------------------

    public void OnWindowBoxHoverEnter(HoverEnterEventArgs args)
    {
        // Only care about hover if a plant is in hand
        if (currentPlantInHand != null)
        {
            isHoveringWindowBox = true;
        }
    }

    public void OnWindowBoxHoverExit(HoverExitEventArgs args)
    {
        isHoveringWindowBox = false;
    }

    // -------------------------------------------------------------------------
    // Core Plant Interaction Logic
    // -------------------------------------------------------------------------

    private void PickupPlant(Transform plantAndSoil, GameObject potMesh, Transform gripPoint, SelectEnterEventArgs args)
    {
        // Disable ray interactor
        if (handRayInteractor != null) handRayInteractor.enabled = false;
        if (handLineVisual != null) handLineVisual.enabled = false;

        // Hide the pot mesh (it stays on the ground)
        if (potMesh != null) potMesh.SetActive(false);

        // Parent the plant + soil to the hand
        Transform handTransform = args.interactorObject.transform;
        plantAndSoil.SetParent(handTransform);
        plantAndSoil.localPosition = Vector3.zero;
        plantAndSoil.localRotation = Quaternion.identity;

        // Align plant forward to hand forward
        plantAndSoil.forward = handTransform.forward;

        // Apply rotation offset
        plantAndSoil.Rotate(0f, 0f, plantRotationOffset, Space.Self);

        // Shift based on grip point
        if (gripPoint != null)
        {
            Vector3 offset = plantAndSoil.position - gripPoint.position;
            plantAndSoil.position += offset;
        }

        // Track current plant
        currentPlantInHand = plantAndSoil;
        currentPlantGripPoint = gripPoint;
    }

    private void PlacePlantInWindowBox()
    {
        if (currentPlantInHand == null) return;

        // Parent the plant to the window box
        currentPlantInHand.SetParent(windowBoxTransform);

        // Position it somewhere sensible in the box (you can adjust this)
        // For simplicity, we'll just place them in a row
        Vector3 localPlantPosition = Vector3.zero;

        switch (currentState)
        {
            case PlantingState.Plant1InHand:
                localPlantPosition = new Vector3(-0.2f, 0.1f, 0f); // Left side
                break;
            case PlantingState.Plant2InHand:
                localPlantPosition = new Vector3(0f, 0.1f, 0f); // Center
                break;
            case PlantingState.Plant3InHand:
                localPlantPosition = new Vector3(0.2f, 0.1f, 0f); // Right side
                break;
        }

        currentPlantInHand.localPosition = localPlantPosition;
        currentPlantInHand.localRotation = Quaternion.identity;

        // Re-enable ray interactor
        if (handRayInteractor != null) handRayInteractor.enabled = true;
        if (handLineVisual != null) handLineVisual.enabled = true;

        // Clear current plant reference
        currentPlantInHand = null;
        currentPlantGripPoint = null;
        isHoveringWindowBox = false;

        // Advance state
        AdvanceToNextState();
    }

    private void AdvanceToNextState()
    {
        switch (currentState)
        {
            case PlantingState.Plant1InHand:
                currentState = PlantingState.WaitingForPlant2;
                break;
            case PlantingState.Plant2InHand:
                currentState = PlantingState.WaitingForPlant3;
                break;
            case PlantingState.Plant3InHand:
                currentState = PlantingState.WaitingForCompost;
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Completion Sequence
    // -------------------------------------------------------------------------

    private IEnumerator CompleteInteraction()
    {
        currentState = PlantingState.Completed;

        // Play completion audio
        if (completionAudio != null)
        {
            completionAudio.Play();
        }

        // Animate window box moving to sill
        if (windowBoxTransform != null && windowBoxSillPosition != null)
        {
            yield return StartCoroutine(MoveWindowBoxToSill());
        }

        // Animate empty pots tipping over
        yield return StartCoroutine(TipOverEmptyPots());

        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportInteractionComplete();
        }

        // Wait a moment before any potential reset
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator MoveWindowBoxToSill()
    {
        Vector3 startPosition = windowBoxTransform.position;
        Quaternion startRotation = windowBoxTransform.rotation;
        Vector3 endPosition = windowBoxSillPosition.position;
        Quaternion endRotation = windowBoxSillPosition.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // Smooth ease-in-out
            t = t * t * (3f - 2f * t);

            windowBoxTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            windowBoxTransform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            yield return null;
        }

        // Ensure final position is exact
        windowBoxTransform.position = endPosition;
        windowBoxTransform.rotation = endRotation;
    }

    private IEnumerator TipOverEmptyPots()
    {
        // Start all three pots tipping simultaneously
        Coroutine pot1Routine = null;
        Coroutine pot2Routine = null;
        Coroutine pot3Routine = null;

        if (plant1PotMesh != null && pot1DropPosition != null)
        {
            pot1Routine = StartCoroutine(TipPot(plant1PotMesh.transform, pot1DropPosition));
        }

        if (plant2PotMesh != null && pot2DropPosition != null)
        {
            pot2Routine = StartCoroutine(TipPot(plant2PotMesh.transform, pot2DropPosition));
        }

        if (plant3PotMesh != null && pot3DropPosition != null)
        {
            pot3Routine = StartCoroutine(TipPot(plant3PotMesh.transform, pot3DropPosition));
        }

        // Wait for all to complete
        if (pot1Routine != null) yield return pot1Routine;
        if (pot2Routine != null) yield return pot2Routine;
        if (pot3Routine != null) yield return pot3Routine;
    }

    private IEnumerator TipPot(Transform potTransform, Transform targetPosition)
    {
        Vector3 startPosition = potTransform.position;
        Quaternion startRotation = potTransform.rotation;
        Vector3 endPosition = targetPosition.position;
        Quaternion endRotation = targetPosition.rotation;

        float elapsed = 0f;

        while (elapsed < potDropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / potDropDuration;

            // Add a slight bounce/fall effect
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease-out sine

            potTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            potTransform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            yield return null;
        }

        // Ensure final position is exact
        potTransform.position = endPosition;
        potTransform.rotation = endRotation;
    }
}
