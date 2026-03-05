using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("PLAYER")]
    public Transform player;

    [Header("1. HEIGHT (Up/Down)")]
    public float height = 0.4f;

    [Header("2. DISTANCE (Close/Far)")]
    public float distance = 2f;

    [Header("3. HORIZONTAL ROTATION (Angle)")]
    public float yaw = 270f;

    [Header("4. PLAYER SCREEN POSITION (FIXED!)")]
    public float playerLeftOffset = -1.3f; 
    // 0 = CENTER
    // 3 = PLAYER ON LEFT SIDE
    // -3 = PLAYER ON RIGHT SIDE

    [Header("SMOOTH")]
    public float smooth = 8f;

    void LateUpdate()
    {
        // 1. Camera rotation (only angle)
        Quaternion rot = Quaternion.Euler(0, yaw, 0);

        // 2. Base camera position (only distance + height)
        Vector3 basePos = rot * new Vector3(0, height, -distance);

        // 3. REAL SCREEN LEFT OFFSET (WORKS 100% ALONE)
        Vector3 screenOffset = rot * Vector3.right * playerLeftOffset;

        // 4. Final position
        Vector3 finalCamPos = player.position + basePos + screenOffset;
        transform.position = Vector3.Lerp(transform.position, finalCamPos, smooth * Time.deltaTime);

        // --------------------------
        // KEY FIX: CAMERA LOOKS AT PLAYER + OFFSET SCREEN LEFT
        // --------------------------
        Vector3 lookTarget = player.position - player.right * playerLeftOffset;
        transform.LookAt(lookTarget);
    }
}