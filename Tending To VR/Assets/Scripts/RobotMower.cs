using UnityEngine;

public class RobotMower : MonoBehaviour
{
    public enum StartCorner
    {
        TopLeft,     // -X, +Z
        TopRight,    // +X, +Z
        BottomLeft,  // -X, -Z
        BottomRight  // +X, -Z
    }

    public enum MowAxis
    {
        X,
        Z
    }

    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────
    [Header("Lawn Reference")]
    [Tooltip("Must have a BoxCollider for accurate bounds.")]
    public Collider lawnCollider;

    [Tooltip("Reference to the script that handles the cut grass mask.")]
    public CutMaskPainter cutMaskPainter;

    [Header("Configuration")]
    public StartCorner startCorner = StartCorner.TopLeft;
    [Tooltip("The main axis the mower travels along.")]
    public MowAxis mowAxis = MowAxis.X;
    [Tooltip("Distance to keep from the edge. X is padding along the world X-axis (left/right bounds), Y is padding along the world Z-axis (forward/back bounds).")]
    public Vector2 edgePadding = new Vector2(0.5f, 0.5f);

    [Header("Movement")]
    public float moveSpeed  = 2f;
    public float rowSpacing = 0.8f;

    [Header("Rotation")]
    [Tooltip("0 = model faces +X by default, 90 = faces +Z, 180 = faces -X, 270 = faces -Z")]
    public float facingOffset = 0f;
    [Tooltip("Degrees per second for all rotations.")]
    public float turnSpeed    = 90f;

    [Header("Debug")]
    public bool drawGizmoBounds = true;

    // ─────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────
    private enum State
    {
        Mowing,
        Turn_RotateToRow,
        Turn_MoveToRow,
        Turn_RotateToMow,
        ReturningHome_RotateToFace,
        ReturningHome_Move,
        ReturningHome_RotateToStart,
        Done
    }

    private State state;

    private float minX, maxX, minZ, maxZ;
    private float yPos;
    private Vector3 startPosition;

    private int   moveDir;     // +1 or -1 along the mowAxis
    private int   stepDir;     // +1 or -1 along the cross axis
    private float nextRowPos;  // Position of the next row on the cross axis

    // Shared rotation lerp fields
    private Quaternion rotFrom;
    private Quaternion rotTo;
    private float      rotProgress;

    // ─────────────────────────────────────────
    // Init
    // ─────────────────────────────────────────
    void Start()
    {
        if (lawnCollider == null)
        {
            Debug.LogError("RobotMower: assign the Lawn Collider in the Inspector.");
            enabled = false;
            return;
        }

        // Apply edge padding to shrink the bounds inward
        Bounds b = lawnCollider.bounds;
        minX = b.min.x + edgePadding.x;  maxX = b.max.x - edgePadding.x;
        minZ = b.min.z + edgePadding.y;  maxZ = b.max.z - edgePadding.y;

        yPos = transform.position.y;

        // Determine starting coordinates and directions based on chosen corner
        float startX = 0f, startZ = 0f;

        switch (startCorner)
        {
            case StartCorner.TopLeft:
                startX = minX; startZ = maxZ;
                moveDir = (mowAxis == MowAxis.X) ? 1 : -1;
                stepDir = (mowAxis == MowAxis.X) ? -1 : 1;
                break;
            case StartCorner.TopRight:
                startX = maxX; startZ = maxZ;
                moveDir = -1;
                stepDir = -1;
                break;
            case StartCorner.BottomLeft:
                startX = minX; startZ = minZ;
                moveDir = 1;
                stepDir = 1;
                break;
            case StartCorner.BottomRight:
                startX = maxX; startZ = minZ;
                moveDir = (mowAxis == MowAxis.X) ? -1 : 1;
                stepDir = (mowAxis == MowAxis.X) ? 1 : -1;
                break;
        }

        startPosition      = new Vector3(startX, yPos, startZ);
        transform.position = startPosition;
        transform.rotation = MowRotation(moveDir);

        state = State.Mowing;
    }

    // ─────────────────────────────────────────
    // Update
    // ─────────────────────────────────────────
    void Update()
    {
        switch (state)
        {
            case State.Mowing:                      UpdateMowing();                   break;
            case State.Turn_RotateToRow:            UpdateRotateToRow();              break;
            case State.Turn_MoveToRow:              UpdateMoveToRow();                break;
            case State.Turn_RotateToMow:            UpdateRotateToMow();              break;
            case State.ReturningHome_RotateToFace:  UpdateReturningHomeRotateToFace();break;
            case State.ReturningHome_Move:          UpdateReturningHomeMove();        break;
            case State.ReturningHome_RotateToStart: UpdateReturningHomeRotateToStart();break;
            case State.Done:                                                          break;
        }
    }

    // ─────────────────────────────────────────
    // MOWING
    // ─────────────────────────────────────────
    void UpdateMowing()
    {
        Vector3 velocity = (mowAxis == MowAxis.X)
            ? new Vector3(moveDir, 0f, 0f)
            : new Vector3(0f, 0f, moveDir);

        transform.position += velocity * (moveSpeed * Time.deltaTime);

        bool rowComplete = false;
        if (mowAxis == MowAxis.X)
        {
            rowComplete = (moveDir == 1 && transform.position.x >= maxX) ||
                          (moveDir == -1 && transform.position.x <= minX);
        }
        else
        {
            rowComplete = (moveDir == 1 && transform.position.z >= maxZ) ||
                          (moveDir == -1 && transform.position.z <= minZ);
        }

        if (!rowComplete) return;

        // Snap exactly to the lawn edge bounds
        float snapX = transform.position.x;
        float snapZ = transform.position.z;

        if (mowAxis == MowAxis.X) snapX = (moveDir == 1) ? maxX : minX;
        else                      snapZ = (moveDir == 1) ? maxZ : minZ;

        transform.position = new Vector3(snapX, yPos, snapZ);

        // Calculate next row step
        float currentCrossPos = (mowAxis == MowAxis.X) ? transform.position.z : transform.position.x;
        float candidatePos    = currentCrossPos + (stepDir * rowSpacing);

        bool outOfBounds = (mowAxis == MowAxis.X)
            ? (candidatePos < minZ || candidatePos > maxZ)
            : (candidatePos < minX || candidatePos > maxX);

        if (outOfBounds)
        {
            BeginReturnHome();
            return;
        }

        nextRowPos = candidatePos;
        BeginRotateToRow();
    }

    // ─────────────────────────────────────────
    // TURN PHASE 1
    // ─────────────────────────────────────────
    void BeginRotateToRow()
    {
        state = State.Turn_RotateToRow;
        BeginRotation(RowCrossRotation(stepDir));
    }

    void UpdateRotateToRow()
    {
        if (StepRotation())
            state = State.Turn_MoveToRow;
    }

    // ─────────────────────────────────────────
    // TURN PHASE 2
    // ─────────────────────────────────────────
    void UpdateMoveToRow()
    {
        Vector3 target = transform.position;
        if (mowAxis == MowAxis.X) target.z = nextRowPos;
        else                      target.x = nextRowPos;

        transform.position = Vector3.MoveTowards(
            transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.001f)
        {
            transform.position = target;
            BeginRotateToMow();
        }
    }

    // ─────────────────────────────────────────
    // TURN PHASE 3
    // ─────────────────────────────────────────
    void BeginRotateToMow()
    {
        state = State.Turn_RotateToMow;
        moveDir *= -1;
        BeginRotation(MowRotation(moveDir));
    }

    void UpdateRotateToMow()
    {
        if (StepRotation())
            state = State.Mowing;
    }

    // ─────────────────────────────────────────
    // RETURNING HOME
    // ─────────────────────────────────────────
    void BeginReturnHome()
    {
        Vector3 toHome = (startPosition - transform.position).normalized;
        if (toHome != Vector3.zero)
        {
            state = State.ReturningHome_RotateToFace;
            Quaternion targetRot = Quaternion.LookRotation(toHome) * Quaternion.Euler(0f, facingOffset, 0f);
            BeginRotation(targetRot);
        }
        else
        {
            BeginReturnHomeEndRotation();
        }
    }

    void UpdateReturningHomeRotateToFace()
    {
        if (StepRotation())
            state = State.ReturningHome_Move;
    }

    void UpdateReturningHomeMove()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, startPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startPosition) < 0.01f)
        {
            transform.position = startPosition;
            BeginReturnHomeEndRotation();
        }
    }

    void BeginReturnHomeEndRotation()
    {
        state = State.ReturningHome_RotateToStart;
        
        // Re-determine starting moveDir
        if (mowAxis == MowAxis.X)
        {
            moveDir = (startCorner == StartCorner.TopLeft || startCorner == StartCorner.BottomLeft) ? 1 : -1;
        }
        else
        {
            moveDir = (startCorner == StartCorner.BottomLeft || startCorner == StartCorner.BottomRight) ? 1 : -1;
        }

        BeginRotation(MowRotation(moveDir));
    }

    void UpdateReturningHomeRotateToStart()
    {
        if (StepRotation())
        {
            state = State.Done;
            Debug.Log("RobotMower: finished — returned home.");

            if (cutMaskPainter != null)
            {
                cutMaskPainter.CutAllGrass();
            }
        }
    }

    // ─────────────────────────────────────────
    // Rotation helpers
    // ─────────────────────────────────────────
    void BeginRotation(Quaternion target)
    {
        rotFrom     = transform.rotation;
        rotTo       = target;
        rotProgress = 0f;
    }

    bool StepRotation()
    {
        rotProgress += (turnSpeed / 90f) * Time.deltaTime;
        rotProgress  = Mathf.Clamp01(rotProgress);

        float t = Mathf.SmoothStep(0f, 1f, rotProgress);
        transform.rotation = Quaternion.Slerp(rotFrom, rotTo, t);

        return rotProgress >= 1f;
    }

    Quaternion MowRotation(int dir)
    {
        float yaw = 0f;
        if (mowAxis == MowAxis.X)
            yaw = (dir == 1) ? 0f : 180f;
        else
            yaw = (dir == 1) ? 270f : 90f; // +Z is 270 (facing local forward depending on how mesh is aligned), actually standard Unity +Z is 0° but based on the previous offset logic:
                                           // We'll rely on facingOffset to fix model orientation.
                                           // Assuming X+ = 0, Z- = 90, X- = 180, Z+ = 270 relative to how you had X+ = 0 and Z- = 270 previously. Wait:
                                           // Since your script previously had: X+=0, X-=180, and Z-=270. That means Z+ is 90.
            yaw = (dir == 1) ? 90f : 270f; 

        return Quaternion.Euler(0f, yaw + facingOffset, 0f);
    }

    Quaternion RowCrossRotation(int dir)
    {
        float yaw = 0f;
        if (mowAxis == MowAxis.X)
            yaw = (dir == 1) ? 90f : 270f;  // cross axis is Z
        else
            yaw = (dir == 1) ? 0f : 180f;   // cross axis is X

        return Quaternion.Euler(0f, yaw + facingOffset, 0f);
    }

    // ─────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!drawGizmoBounds || lawnCollider == null) return;
        
        Bounds b = lawnCollider.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(b.center, b.size);

        // Draw the padded bounds where the mower will actually travel
        Gizmos.color = Color.yellow;
        Vector3 paddedSize = b.size - new Vector3(edgePadding.x * 2, 0f, edgePadding.y * 2);
        if (paddedSize.x > 0 && paddedSize.z > 0)
        {
            Gizmos.DrawWireCube(b.center, paddedSize);
        }
    }
}