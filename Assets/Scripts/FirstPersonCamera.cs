using UnityEngine;
using System.Collections;

/// <summary>
/// The camera used to track the player's field of view
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{

    public float rangeX = 180.0f; // The allowable POV shift as the mouse is moved

    // The allowable speed of the camera
    public float speedH = 2.0f;
    public float speedV = 2.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    /// <summary>
    /// Updates every frame to match the actions of the mouse
    /// </summary>
    void Update()
    {
        yaw += speedH * Input.GetAxis("Mouse X");
        // pitch -= speedV * Input.GetAxis("Mouse Y"); //Client requested no vertical movement

        // Make sure yaw is within range
        yaw = Mathf.Min(yaw, rangeX);
        yaw = Mathf.Max(yaw, -rangeX);

        transform.localEulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
