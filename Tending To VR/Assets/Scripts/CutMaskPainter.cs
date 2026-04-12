using UnityEngine;

public class CutMaskPainter : MonoBehaviour
{
    [Header("References")]
    public Transform   robotTransform;   // drag your Robot object here
    public Collider    robotCollider;    // The specific collider representing the cutting area
    public Collider    lawnCollider;     // same lawn collider as RobotMower

    [Header("Texture settings")]
    public int resolution = 512;     // 512×512 is plenty for most lawns

    // The material property name your grass shader expects
    private static readonly int CutMaskID = Shader.PropertyToID("_CutMask");

    [HideInInspector] public RenderTexture cutMask;

    private float brushRadius;
    private Material  _paintMaterial;
    private Bounds    _lawnBounds;
    private RenderTextureFormat _renderTextureFormat;

    void Awake()
    {
        // Use ARGB32 which is universally supported and avoids SRGB conversion issues
        _renderTextureFormat = RenderTextureFormat.ARGB32;

        // Create the RenderTexture — starts fully black (no grass cut)
        cutMask = new RenderTexture(resolution, resolution, 0, _renderTextureFormat);
        cutMask.filterMode = FilterMode.Bilinear;
        cutMask.Create();

        // Clear it to black
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = cutMask;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;

        // A simple unlit additive shader for painting white circles
        _paintMaterial = new Material(Shader.Find("Hidden/CutMaskBrush"));

        _lawnBounds = lawnCollider.bounds;

        // Pass the actual lawn size to the material to avoid elliptical/squashed brushes on rectangular lawns
        _paintMaterial.SetVector("_LawnSize", new Vector4(_lawnBounds.size.x, _lawnBounds.size.z, 0, 0));

        // Automatically calculate brush radius based on the lawnmower's size
        Collider robotCol = robotCollider != null ? robotCollider : robotTransform.GetComponentInChildren<Collider>();
        if (robotCol != null)
        {
            // Use the maximum width/depth of the robot for its diameter
            float robotDiameter = Mathf.Max(robotCol.bounds.size.x, robotCol.bounds.size.z);
            
            // The brush radius in world space is just half the diameter
            brushRadius = robotDiameter / 2f;
            Debug.Log($"Calculated world-space brushRadius to {brushRadius} based on mower size {robotDiameter}");
        }
    }

    void LateUpdate()
    {
        if (robotTransform == null) return;
        PaintAt(robotTransform.position);
    }

    public void PaintAt(Vector3 worldPos)
    {
        // Convert world position → 0..1 UV within the lawn's XZ bounding box
        float u = Mathf.InverseLerp(_lawnBounds.min.x, _lawnBounds.max.x, worldPos.x);
        float v = Mathf.InverseLerp(_lawnBounds.min.z, _lawnBounds.max.z, worldPos.z);

        // Use Graphics.Blit with the brush material to paint onto our RenderTexture
        _paintMaterial.SetVector("_BrushUV",     new Vector4(u, v, 0, 0));
        _paintMaterial.SetFloat ("_BrushRadius", brushRadius);

        // Blit with a temporary render texture to avoid undefined behavior when reading/writing to the same texture
        RenderTexture temp = RenderTexture.GetTemporary(cutMask.width, cutMask.height, 0, _renderTextureFormat);
        Graphics.Blit(cutMask, temp); // First safely copy the current mask state to temp without blending
        Graphics.Blit(temp, cutMask, _paintMaterial); // Then apply the brush to cutMask, using temp as the source
        RenderTexture.ReleaseTemporary(temp);
    }

    public void CutAllGrass()
    {
        if (cutMask == null) return;
        
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = cutMask;
        GL.Clear(true, true, Color.white); // Fill entirely white (cut)
        RenderTexture.active = prev;
        
        Debug.Log("CutMaskPainter: Entire lawn cut mask cleared.");
    }

    void OnDestroy()
    {
        if (cutMask != null) cutMask.Release();
    }
}