using UnityEngine;

public class BrokenMaterialSwapper : MonoBehaviour
{
    [Header("Materials")]
    public Material brokenMaterial;

    [Header("Trigger")]
    [Tooltip("The stage at which the material swaps to broken.")]
    [SerializeField] private Stage swapAtStage;

    [Tooltip("If true, swapping back to original when jumping to an earlier stage (editor debug only).")]
    [SerializeField] private bool restoreOnEarlierStage = true;

    private Renderer _renderer;
    private Material _originalMaterial;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _originalMaterial = _renderer.material;
    }

    private void Start()
    {
        // Apply correct material for the current stage on first load.
        if (GameManager.Instance != null)
            OnStageChanged(GameManager.Instance.CurrentStage);
    }

    private void OnEnable()
    {
        StageSequencer.OnPlayerArrived += OnStageChanged;
    }

    private void OnDisable()
    {
        StageSequencer.OnPlayerArrived -= OnStageChanged;
    }

    private void OnStageChanged(Stage newStage)
    {
        if (newStage >= swapAtStage)
            SwapToBroken();
        else if (restoreOnEarlierStage)
            SwapToOriginal();
    }

    public void SwapToBroken()
    {
        if (_renderer != null && brokenMaterial != null)
            _renderer.material = brokenMaterial;
    }

    public void SwapToOriginal()
    {
        if (_renderer != null && _originalMaterial != null)
            _renderer.material = _originalMaterial;
    }
}