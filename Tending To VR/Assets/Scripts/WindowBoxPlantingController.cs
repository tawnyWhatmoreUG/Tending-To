using System;
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
///    Plants  (array of 3 PlantData entries — expand each element)
///      Plant And Soil   — the plant + soil GameObject that moves with the hand
///      Pot Mesh         — the pot mesh GameObject that stays behind when picked up
///      Grip Point       — empty Transform marking where the hand grabs
///      Drop Position    — empty Transform with rotation for where the pot ends up tipped over
///
///    Window Box
///      Window Box Transform     — the window box GameObject
///      Plant Anchors            — array of 3 empty Transforms inside window box (positions for each plant)
///      Window Box Sill Position — empty Transform marking where box ends up (window sill)
///      Move Duration            — seconds for box to move to sill (default 1.5s)
///      Pot Drop Duration        — seconds for pots to tip over (default 0.8s)
///
///    Compost Bag
///      Compost Bag Closed  — the closed bag mesh (active at start)
///      Compost Bag Opened  — the open bag mesh (shown on select)
///      Compost Bag Soil    — the soil mesh (shown on select)
///
///    VR Hand
///      Hand Ray Interactor  — the XRRayInteractor on the right hand
///      Hand Line Visual     — the XRInteractorLineVisual on the right hand
///      Trigger Action       — Input action for the trigger button
///
///    Plant Alignment
///      Plant Rotation Offset — Z-axis rotation when plant is equipped (default 0)
///
///    Completion
///      Completion Audio — AudioSource with the success sound clip
///
/// 3. Setup XRSimpleInteractable components:
///    - On Plant 1's root GameObject, add XRSimpleInteractable
///      Wire "Select Entered" → WindowBoxPlantingController.OnPlant1Selected
///    - Repeat for Plant 2 → OnPlant2Selected
///    - Repeat for Plant 3 → OnPlant3Selected
///    - On the Window Box, add XRSimpleInteractable
///      Wire "Hover Entered" → OnWindowBoxHoverEnter
///      Wire "Hover Exited"  → OnWindowBoxHoverExit
///
/// INTERACTION FLOW
/// ================
///   Step 1: Ray + Select Plant 1  →  plant snaps to hand, pot stays
///   Step 2: Hover over window box + Trigger  →  plant places in box
///   Step 3-6: Repeat for Plant 2 and Plant 3
///   After Plant 3 placed: Automatically opens compost bag, moves box to sill, tips over pots, plays sound, completes stage
/// </summary>
public class WindowBoxPlantingController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    public event Action OnPlantingComplete;

    /// <summary>
    /// Fired when the player selects/grabs any plant for the first time.
    /// </summary>
    public event System.Action OnFirstPlantSelected;

    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class PlantData
    {
        public Transform plantAndSoil;
        public GameObject potMesh;
        public Transform gripPoint;
        public Transform dropPosition;
    }

    [Header("Plants")]
    public PlantData[] plants = new PlantData[3];

    [Header("Window Box")]
    public Transform windowBoxTransform;
    public Transform[] plantAnchors = new Transform[3];
    public Transform windowBoxSillPosition;
    public float moveDuration = 1.5f;
    public float potDropDuration = 0.8f;

    [Header("Compost Bag")]
    public GameObject compostBagClosed;
    public GameObject compostBagOpened;
    public GameObject compostBagSoil;

    [Header("VR Hand")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor handRayInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual handLineVisual;
    public InputActionProperty gripAction;

    [Header("Plant Alignment")]
    public float plantRotationOffset = 0f;

    [Header("Completion")]
    public AudioSource completionAudio;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private enum PlantingState
    {
        NoPlantInHand,
        PlantInHand,
        Completed
    }

    private PlantingState currentState = PlantingState.NoPlantInHand;
    private Transform currentPlantInHand;
    private Transform currentPlantGripPoint;
    private int currentPlantIndex = -1;
    private bool[] plantPlaced = new bool[3];
    private bool[] anchorOccupied = new bool[3];
    private int plantsPlacedCount = 0;
    private bool isHoveringWindowBox = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        Debug.Log($"[WindowBoxPlanting] Starting - plants can be placed in any order");
        
        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i].plantAndSoil == null)
                Debug.LogError($"[WindowBoxPlanting] Plant {i + 1} plantAndSoil is NULL!");
            plantPlaced[i] = false;
        }
        
        for (int i = 0; i < anchorOccupied.Length; i++)
        {
            anchorOccupied[i] = false;
        }
        
        if (compostBagClosed != null) compostBagClosed.SetActive(true);
        if (compostBagOpened != null) compostBagOpened.SetActive(false);
        if (compostBagSoil != null) compostBagSoil.SetActive(false);
    }

    void Update()
    {
        if (currentPlantInHand != null)
        {
            // Debug every few frames to avoid spam
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[WindowBoxPlanting] Plant in hand. Hovering: {isHoveringWindowBox}, Ray enabled: {handRayInteractor?.enabled}");
            }
            
            if (isHoveringWindowBox)
            {
                float triggerValue = gripAction.action.ReadValue<float>();
                if (triggerValue > 0.1f)
                {
                    Debug.Log($"[WindowBoxPlanting] Grip pressed ({triggerValue:F2}) while hovering - placing plant");
                    PlacePlantInWindowBox();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Plant Selection Methods (wire to XRSimpleInteractable Select Entered)
    // -------------------------------------------------------------------------

    public void OnPlant1Selected(SelectEnterEventArgs args) => OnPlantSelected(0, args);
    public void OnPlant2Selected(SelectEnterEventArgs args) => OnPlantSelected(1, args);
    public void OnPlant3Selected(SelectEnterEventArgs args) => OnPlantSelected(2, args);

    private void OnPlantSelected(int plantIndex, SelectEnterEventArgs args)
    {
        Debug.Log($"[WindowBoxPlanting] Plant {plantIndex + 1} selected. Current state: {currentState}");
        
        if (currentState == PlantingState.PlantInHand)
        {
            Debug.LogWarning($"[WindowBoxPlanting] Already holding a plant, ignoring selection");
            return;
        }
        
        if (plantPlaced[plantIndex])
        {
            Debug.LogWarning($"[WindowBoxPlanting] Plant {plantIndex + 1} already placed, ignoring");
            return;
        }

        // Notify listeners on first plant selection
        if (currentState == PlantingState.NoPlantInHand)
        {
            OnFirstPlantSelected?.Invoke();
        }
        
        Debug.Log($"[WindowBoxPlanting] Picking up Plant {plantIndex + 1}");
        PickupPlant(plants[plantIndex], args);
        currentPlantIndex = plantIndex;
        currentState = PlantingState.PlantInHand;
    }

    // -------------------------------------------------------------------------
    // Compost Bag (automatically opens on completion)
    // -------------------------------------------------------------------------
    // Note: OnCompostBagSelected is no longer needed - compost bag opens automatically

    // -------------------------------------------------------------------------
    // Window Box Hover Detection (wire to XRSimpleInteractable on window box)
    // -------------------------------------------------------------------------

    public void OnWindowBoxHoverEnter(HoverEnterEventArgs args)
    {
        Debug.Log($"[WindowBoxPlanting] Window box hover ENTERED. Plant in hand: {currentPlantInHand != null}");
        if (currentPlantInHand != null)
        {
            isHoveringWindowBox = true;
            Debug.Log("[WindowBoxPlanting] Ready to place plant - pull trigger to drop it in!");
        }
        else
        {
            Debug.Log("[WindowBoxPlanting] No plant in hand, ignoring hover");
        }
    }

    public void OnWindowBoxHoverExit(HoverExitEventArgs args)
    {
        Debug.Log("[WindowBoxPlanting] Window box hover EXITED");
        isHoveringWindowBox = false;
    }

    // -------------------------------------------------------------------------
    // Core Plant Interaction Logic
    // -------------------------------------------------------------------------

    private void PickupPlant(PlantData plant, SelectEnterEventArgs args)
    {
        Debug.Log($"[WindowBoxPlanting] PickupPlant started for {plant.plantAndSoil?.name}");
        
        if (handLineVisual != null) handLineVisual.enabled = false;
        if (plant.potMesh != null) plant.potMesh.SetActive(false);

        Transform handTransform = args.interactorObject.transform;
        plant.plantAndSoil.SetParent(handTransform);
        plant.plantAndSoil.rotation = Quaternion.Euler(0f, handTransform.eulerAngles.y + plantRotationOffset, 0f);
        plant.plantAndSoil.localPosition = Vector3.zero;

        if (plant.gripPoint != null)
        {
            Vector3 offset = plant.plantAndSoil.position - plant.gripPoint.position;
            plant.plantAndSoil.position += offset;
        }

        currentPlantInHand = plant.plantAndSoil;
        currentPlantGripPoint = plant.gripPoint;
        
        Debug.Log($"[WindowBoxPlanting] Plant picked up successfully");
    }

    private void PlacePlantInWindowBox()
    {
        if (currentPlantInHand == null || currentPlantIndex < 0)
        {
            Debug.LogWarning("[WindowBoxPlanting] PlacePlantInWindowBox called but no plant in hand!");
            return;
        }

        // Find next available anchor
        int anchorIndex = -1;
        for (int i = 0; i < anchorOccupied.Length; i++)
        {
            if (!anchorOccupied[i])
            {
                anchorIndex = i;
                break;
            }
        }
        
        if (anchorIndex < 0)
        {
            Debug.LogError("[WindowBoxPlanting] No available anchors!");
            return;
        }

        Debug.Log($"[WindowBoxPlanting] Placing plant {currentPlantIndex + 1} in anchor {anchorIndex + 1}");
        
        // Place at anchor with -90 X rotation to make plants upright
        if (anchorIndex < plantAnchors.Length && plantAnchors[anchorIndex] != null)
        {
            Transform anchor = plantAnchors[anchorIndex];
            currentPlantInHand.SetParent(windowBoxTransform);
            currentPlantInHand.position = anchor.position;
            currentPlantInHand.rotation = Quaternion.Euler(-90f, 0f, 0f);
            Debug.Log($"[WindowBoxPlanting] Plant placed at anchor {anchorIndex + 1}: {anchor.name}, rotation set to -90 X");
        }
        else
        {
            // Fallback to hardcoded positions
            currentPlantInHand.SetParent(windowBoxTransform);
            Vector3 localPlantPosition = anchorIndex switch
            {
                0 => new Vector3(-0.2f, 0.1f, 0f),
                1 => new Vector3(0f, 0.1f, 0f),
                2 => new Vector3(0.2f, 0.1f, 0f),
                _ => Vector3.zero
            };
            currentPlantInHand.localPosition = localPlantPosition;
            currentPlantInHand.rotation = Quaternion.Euler(-90f, 0f, 0f);
            Debug.LogWarning($"[WindowBoxPlanting] Plant anchor {anchorIndex + 1} not assigned, using fallback position with -90 X rotation");
        }

        // Mark as placed
        plantPlaced[currentPlantIndex] = true;
        anchorOccupied[anchorIndex] = true;
        plantsPlacedCount++;

        if (handLineVisual != null) handLineVisual.enabled = true;

        currentPlantInHand = null;
        currentPlantGripPoint = null;
        currentPlantIndex = -1;
        isHoveringWindowBox = false;
        currentState = PlantingState.NoPlantInHand;

        // Check completion
        if (plantsPlacedCount >= 3)
        {
            currentState = PlantingState.Completed;
            StartCoroutine(CompleteInteraction());
        }
    }

    // -------------------------------------------------------------------------
    // Completion Sequence
    // -------------------------------------------------------------------------

    private IEnumerator CompleteInteraction()
    {
        // Automatically open the compost bag
        if (compostBagClosed != null) compostBagClosed.SetActive(false);
        if (compostBagOpened != null) compostBagOpened.SetActive(true);
        if (compostBagSoil != null) compostBagSoil.SetActive(true);

        if (completionAudio != null)
            completionAudio.Play();

        if (windowBoxTransform != null && windowBoxSillPosition != null)
            yield return StartCoroutine(MoveWindowBoxToSill());

        yield return StartCoroutine(TipOverEmptyPots());

        OnPlantingComplete?.Invoke();

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
            t = t * t * (3f - 2f * t);

            windowBoxTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            windowBoxTransform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            yield return null;
        }

        windowBoxTransform.position = endPosition;
        windowBoxTransform.rotation = endRotation;
    }

    private IEnumerator TipOverEmptyPots()
    {
        Coroutine[] routines = new Coroutine[plants.Length];

        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i].potMesh != null && plants[i].dropPosition != null)
                routines[i] = StartCoroutine(TipPot(plants[i].potMesh.transform, plants[i].dropPosition));
        }

        foreach (Coroutine routine in routines)
        {
            if (routine != null) yield return routine;
        }
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
            float t = Mathf.Sin((elapsed / potDropDuration) * Mathf.PI * 0.5f);

            potTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            potTransform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

            yield return null;
        }

        potTransform.position = endPosition;
        potTransform.rotation = endRotation;
    }
}
