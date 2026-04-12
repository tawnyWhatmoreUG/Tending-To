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
///      Plant Anchors            — array of 3 empty Transforms inside window box (positions where plants are placed)
///      Window Box Sill Position — empty Transform marking where box ends up (window sill)
///      Move Duration            — seconds for box to move to sill (default 1.5s)
///      Pot Drop Duration        — seconds for pots to tip over (default 0.8s)
///
///    Compost Bag
///      Compost Bag Closed  — the closed bag mesh (active at start)
///      Compost Bag Opened  — the open bag mesh (shown after all plants placed)
///      Compost Bag Soil    — the soil mesh (shown after all plants placed)
///
///    VR Hand
///      Hand Ray Interactor  — the XRRayInteractor on the right hand
///      Hand Line Visual     — the XRInteractorLineVisual on the right hand
///      Trigger Action       — Input action for the trigger button
///
///    Completion
///      Completion Audio — AudioSource with the success sound clip
///
/// 3. Setup XRSimpleInteractable components:
///    - On each plant's root GameObject, add XRSimpleInteractable
///      Wire "Select Entered" → WindowBoxPlantingController.OnPlantSelected (same method for all plants)
///    - On the Window Box, add XRSimpleInteractable
///      Wire "Hover Entered" → OnWindowBoxHoverEnter
///      Wire "Hover Exited"  → OnWindowBoxHoverExit
///
/// INTERACTION FLOW
/// ================
///   Pick up any plant using ray selection → plant snaps to hand, pot stays behind
///   Hover over window box + pull trigger → plant places in next available anchor with zeroed rotation
///   Repeat for remaining plants in any order
///   After all plants placed: Automatically opens compost bag, moves box to sill, tips over pots, plays sound, completes stage
/// </summary>
public class WindowBoxPlantingController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    public event Action OnPlantingComplete;

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
    public InputActionProperty triggerAction;

    [Header("Completion")]
    public AudioSource completionAudio;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Transform currentPlantInHand;
    private PlantData currentPlantData;
    private bool isHoveringWindowBox = false;
    private int nextAnchorIndex = 0;
    private int plantsPlaced = 0;
    private const int TOTAL_PLANTS = 3;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        Debug.Log($"[WindowBoxPlanting] Starting planting interaction. Total plants: {plants.Length}");
        
        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i].plantAndSoil == null)
                Debug.LogError($"[WindowBoxPlanting] Plant {i + 1} plantAndSoil is NULL!");
            else
                Debug.Log($"[WindowBoxPlanting] Plant {i + 1} configured: {plants[i].plantAndSoil.name}");
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
                float triggerValue = triggerAction.action.ReadValue<float>();
                if (triggerValue > 0.1f)
                {
                    Debug.Log($"[WindowBoxPlanting] Trigger pulled ({triggerValue:F2}) while hovering - placing plant");
                    PlacePlantInWindowBox();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Plant Selection Method (wire to XRSimpleInteractable Select Entered on all plants)
    // -------------------------------------------------------------------------

    public void OnPlantSelected(SelectEnterEventArgs args)
    {
        // If already holding a plant, ignore
        if (currentPlantInHand != null)
        {
            Debug.LogWarning("[WindowBoxPlanting] Already holding a plant, ignoring selection.");
            return;
        }

        // Find which plant was selected
        Transform selectedTransform = args.interactableObject.transform;
        PlantData selectedPlant = null;
        
        foreach (PlantData plant in plants)
        {
            if (plant.plantAndSoil == selectedTransform || plant.plantAndSoil.IsChildOf(selectedTransform) || selectedTransform.IsChildOf(plant.plantAndSoil))
            {
                selectedPlant = plant;
                break;
            }
        }

        if (selectedPlant == null)
        {
            Debug.LogError($"[WindowBoxPlanting] Could not find plant data for selected object: {selectedTransform.name}");
            return;
        }

        Debug.Log($"[WindowBoxPlanting] Picking up plant: {selectedPlant.plantAndSoil.name}");
        PickupPlant(selectedPlant, args);
    }

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
        
        // Hide the line visual but keep ray enabled for hover detection
        if (handLineVisual != null) handLineVisual.enabled = false;

        // Hide the pot mesh
        if (plant.potMesh != null) plant.potMesh.SetActive(false);

        // Parent to hand
        Transform handTransform = args.interactorObject.transform;
        plant.plantAndSoil.SetParent(handTransform);
        
        // Keep the plant upright in world space
        plant.plantAndSoil.rotation = Quaternion.Euler(0f, handTransform.eulerAngles.y, 0f);
        plant.plantAndSoil.localPosition = Vector3.zero;

        // Adjust position based on grip point
        if (plant.gripPoint != null)
        {
            Vector3 offset = plant.plantAndSoil.position - plant.gripPoint.position;
            plant.plantAndSoil.position += offset;
        }

        currentPlantInHand = plant.plantAndSoil;
        currentPlantData = plant;
        
        Debug.Log($"[WindowBoxPlanting] Plant picked up successfully. Position: {plant.plantAndSoil.position}");
    }

    private void PlacePlantInWindowBox()
    {
        if (currentPlantInHand == null)
        {
            Debug.LogWarning("[WindowBoxPlanting] PlacePlantInWindowBox called but no plant in hand!");
            return;
        }

        Debug.Log($"[WindowBoxPlanting] Placing plant in window box. Next anchor index: {nextAnchorIndex}");
        
        // Use the next available anchor
        if (nextAnchorIndex < plantAnchors.Length && plantAnchors[nextAnchorIndex] != null)
        {
            Transform anchor = plantAnchors[nextAnchorIndex];
            
            // Unparent temporarily to set absolute positioning
            currentPlantInHand.SetParent(null);
            currentPlantInHand.position = anchor.position;
            
            // Zero out world rotation (completely flat/upright)
            currentPlantInHand.rotation = Quaternion.identity;
            
            // Parent to window box, keeping world position and rotation
            currentPlantInHand.SetParent(windowBoxTransform, worldPositionStays: true);
            
            Debug.Log($"[WindowBoxPlanting] Plant placed at anchor {nextAnchorIndex + 1}: {anchor.name}, world rotation reset to identity");
        }
        else
        {
            Debug.LogError($"[WindowBoxPlanting] Plant anchor {nextAnchorIndex + 1} not assigned or out of range!");
        }

        // Re-show the line visual
        if (handLineVisual != null) handLineVisual.enabled = true;

        // Clear current plant references
        currentPlantInHand = null;
        currentPlantData = null;
        isHoveringWindowBox = false;

        // Move to next anchor and increment placed count
        nextAnchorIndex++;
        plantsPlaced++;

        // Check if all plants are placed
        if (plantsPlaced >= TOTAL_PLANTS)
        {
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
