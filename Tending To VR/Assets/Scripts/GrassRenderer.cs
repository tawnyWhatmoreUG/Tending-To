using UnityEngine;

public class GrassRenderer : MonoBehaviour
{
    [Header("References")]
    public Collider         lawnCollider;
    public CutMaskPainter   cutMaskPainter;
    public Mesh             bladeMesh;      // assign a simple quad mesh (see note below)
    public Material         grassMaterial;  // assign your Grass material

    [Header("Density")]
    public int   bladeCount  = 50000;
    public float heightMin   = 0.2f;
    public float heightMax   = 0.5f;

    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _positionBuffer;
    private Bounds        _drawBounds;

    void Start()
    {
        SetupBuffers();
    }

    void SetupBuffers()
    {
        if (lawnCollider == null || bladeMesh == null || grassMaterial == null)
        {
            Debug.LogWarning("Missing references on GrassRenderer! Please assign lawnCollider, bladeMesh, and grassMaterial in the Inspector.");
            return;
        }

        Bounds b = lawnCollider.bounds;
        _drawBounds = b;

        // Generate random positions across the lawn (XZ only)
        Vector4[] positions = new Vector4[bladeCount];
        for (int i = 0; i < bladeCount; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            float h = Random.Range(heightMin, heightMax);      // w = blade height scale
            float r = Random.Range(0f, 360f);                  // z = random Y rotation
            positions[i] = new Vector4(x, b.min.y, z, h);
        }

        _positionBuffer = new ComputeBuffer(bladeCount, sizeof(float) * 4);
        _positionBuffer.SetData(positions);
        grassMaterial.SetBuffer("_Positions", _positionBuffer);

        // Args buffer: index count, instance count, start index, base vertex, start instance
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = bladeMesh.GetIndexCount(0);
        args[1] = (uint)bladeCount;
        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint),
                                        ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(args);
    }

    void Update()
    {
        if (_argsBuffer == null || bladeMesh == null || grassMaterial == null)
            return;

        // Pass the cut mask texture to the shader every frame (it changes as robot moves)
        if (cutMaskPainter != null && cutMaskPainter.cutMask != null)
        {
            grassMaterial.SetTexture("_CutMask", cutMaskPainter.cutMask);

            // Also pass the lawn bounds so the shader can convert world pos → UV
            Bounds b = lawnCollider.bounds;
            grassMaterial.SetVector("_LawnMin", new Vector4(b.min.x, b.min.y, b.min.z, 0));
            grassMaterial.SetVector("_LawnSize", new Vector4(b.size.x, b.size.y, b.size.z, 0));
        }

        // One draw call for all 50 000 blades
        Graphics.DrawMeshInstancedIndirect(
            bladeMesh, 0, grassMaterial,
            _drawBounds, _argsBuffer);
    }

    void OnDestroy()
    {
        _positionBuffer?.Release();
        _argsBuffer?.Release();
    }
}