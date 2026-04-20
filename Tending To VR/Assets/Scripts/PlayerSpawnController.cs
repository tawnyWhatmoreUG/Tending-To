using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Moves the XR Origin so the player's camera lands exactly on the assigned
/// spawnPoint at scene start, regardless of real-world room offset.
///
/// SETUP:
///   - Attach this script to the XR Origin GameObject (or any scene object).
///   - Create an empty GameObject in the scene at the desired player start
///     position and assign it to spawnPoint.
///   - If xrOrigin is left unassigned it will be found automatically.
/// </summary>
public class PlayerSpawnController : MonoBehaviour
{
    [Tooltip("Where the player's camera should be placed when the scene starts.")]
    public Transform spawnPoint;

    [Tooltip("The XR Origin in the scene. Auto-found if left blank.")]
    public XROrigin xrOrigin;

    private void Start()
    {
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("[PlayerSpawnController] No XROrigin found in scene.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[PlayerSpawnController] No spawnPoint assigned — player will spawn at room-relative origin.");
            return;
        }

        xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);
        Debug.Log($"[PlayerSpawnController] Camera moved to spawn point: {spawnPoint.position}");
    }
}
