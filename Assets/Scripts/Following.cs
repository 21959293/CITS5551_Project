using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class used to calculate how to follow the player (could probably be consolidated)
/// </summary>
public class Following
{
    public bool rotate; // Whether or not to rotate with the player
    public float height; // Height above the player to track


    /// <summary>
    /// Constructor for the object to set basic parameters
    /// </summary>
    /// <param name="RotateWithPlayer">Whether or not to rotate with the player</param>
    /// <param name="HeightAbovePlayer">  Height above the player to track </param>
    public Following(bool RotateWithPlayer, float HeightAbovePlayer)
    {
        rotate = RotateWithPlayer;
        height = HeightAbovePlayer;
    }

    /// <summary>
    /// Calculates how much to move based on the player's position
    /// </summary>
    /// <param name="playerPosition">The player's current position as a vector</param>
    /// <returns>The position the camera should be in to be directly above the player</returns>
    public Vector3 CalculateMovement(Vector3 playerPosition)
    {
        //Increase height above player
        playerPosition.y = height;

        return playerPosition;
    }

    /// <summary>
    /// Calculates the new rotation of the camera based on the player's position
    /// </summary>
    /// <param name="playerRotation">The player's current rotation</param>
    /// <param name="currentRotation">The camera's current rotation</param>
    /// <returns></returns>
    public Quaternion CalculateRotation(Quaternion playerRotation, Quaternion currentRotation)
    {
        Vector3 player = playerRotation.eulerAngles;
        Vector3 current = currentRotation.eulerAngles;

        if (rotate)
        {
            //Rotate to same direction as player
            current.y = player.y;
        }

        return Quaternion.Euler(current);
    }
}
