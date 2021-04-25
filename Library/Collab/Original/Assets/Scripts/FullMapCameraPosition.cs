#region Using
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;

#endregion

// *********************************************************************************************
//  FullMapCameraPosition Class
//
/// <summary>
///     Provides class for resizing, positioning, and rotating the full map camera based
///     on the path.
///     </summary> 
//
// *********************************************************************************************
public class FullMapCameraPosition : MonoBehaviour
{
    // Gets full map camera object
    public Camera fullMapCameraOrthographic;
    // Initialises list for nodes
    private List<Transform> nodes = new List<Transform>();
    // Default orthographic size for full map camera
    static float orthographicSize = 500;
    // Threshold for maximum x-width before resizing full map
    static float xThresh = 1200;
    // Threshold for maximum z-width before resizing full map
    static float zThresh = 550;
    private float newOrthographicSize;
    GameObject PlayerObject;

    // *****************************************************************************************
    //  ResizeFullMap Method
    //
    /// <summary>
    ///     Changes the orthographic size of the full map camera if they exceed the 
    ///     thresholds set
    ///     </summary>
    //
    // *****************************************************************************************
    void ResizeFullMap()
    {
        // Gets current player object
        PlayerObject = GameObject.FindGameObjectWithTag("Player");
        // Gets current player's path object
        GameObject PathObject = PlayerObject.transform.GetChild(1).gameObject;

        // Check if player's path exists
        if (PathObject == null)
        {
            Debug.Log("Path doesn't exist" + PathObject);
        }
        else
        {
            Debug.Log("Path does exist" + PathObject);
        }

        // The number of nodes in the player's current path
        int pathCount = PathObject.transform.childCount;

        //  The first node in the player's current path
        Vector3 firstNode = PathObject.transform.GetChild(0).transform.position;
        //  The last node in the player's current path
        Vector3 lastNode = PathObject.transform.GetChild(pathCount - 1).transform.position;

        // Calculates differences between x and z coordinates for first and last nodes
        float xDiff = firstNode.x - lastNode.x;
        float zDiff = firstNode.z - lastNode.z;
        //float distance = Vector3.Distance(firstNode, lastNode);

        // Call method to set the full map camera position
        SetCameraPosition(PathObject, firstNode, lastNode, xDiff, zDiff, pathCount);
        
        // Check if either the differences between x-coordinates or z-coordinates is greater than the set threshold
        if (xDiff > xThresh || zDiff > zThresh)
        {
            // Calculates how much they exceed the threshold by
            float xOver = Math.Abs(xDiff - xThresh);
            float zOver = Math.Abs(zDiff - zThresh);
            // If the difference is greater for x-coordinates than z-coordinates
            if (xOver > zOver)
            {
                // Multiply orthographic size by percentage greater than threshold
                float xPercentage = Math.Abs(xDiff / xThresh);
                newOrthographicSize = orthographicSize * xPercentage;
            }
            // Else when the difference is greater for z-coordinates than x-coordinates
            else
            {
                // Multiply orthographic size by percentage greater than threshold
                float zPercentage = Math.Abs(zDiff / zThresh);
                newOrthographicSize = orthographicSize * zPercentage;
            }
            // Set the new orthographic size
            fullMapCameraOrthographic.orthographicSize = newOrthographicSize;

        }
        else
        {
            //Set the orthographic size to the default
            fullMapCameraOrthographic.orthographicSize = orthographicSize;
        }

        // Call method to rotate the full map if necessary
        RotateFullMap(firstNode, lastNode);
    }

    // *****************************************************************************************
    //  ResizeFullMap Method
    //
    /// <summary>
    ///     Changes the orthographic size of the full map camera if they exceed the thresholds set
    ///     </summary>
    /// <param name="PathObject">
    ///     The object containing nodes in the player's current path.</param>
    /// <param name="firstNode">
    ///     The first node in the player's current path.</param>
    /// <param name="lastNode">
    ///     The last node in the player's current path.</param>
    /// <param name="xDiff">
    ///     The difference between the x-coordinates of the first and last nodes.</param>
    /// <param name="zDiff">
    ///     The difference between the z-coordinates of the first and last nodes.</param>
    /// <param name="pathCount">
    ///     The number of nodes in the player's current path.</param>
    //
    // *****************************************************************************************
    void SetCameraPosition(GameObject PathObject, Vector3 firstNode, Vector3 lastNode, float xDiff, float zDiff, int pathCount)
    {
        // Define the target x and z coordinates for the full map camera based on the first and last nodes
        float xTarget = firstNode.x - (xDiff / 2);
        float zTarget = firstNode.z - (zDiff / 2);
        // Create a new vector with the target coordinates
        Vector3 cameraPosition = new Vector3(xTarget, 200, zTarget);
        // Update the position of the full map camera
        GameObject.Find("FullMapCamera").transform.position = cameraPosition;
    }

    // *****************************************************************************************
    //  ResizeFullMap Method
    //
    /// <summary>
    ///     Rotates the full map 90 degrees if the difference in z-coordinates between first and
    ///     last nodes is greater than the difference in x-coordinates
    ///     </summary>
    /// <param name="firstNode">
    ///     The first node in the player's current path.</param>
    /// <param name="lastNode">
    ///     The last node in the player's current path.</param>
    //
    // *****************************************************************************************
    void RotateFullMap(Vector3 firstNode, Vector3 lastNode)
    {
        // Get the quaternion rotation of the full map camera
        Quaternion currentRotation = GameObject.Find("FullMapCamera").transform.rotation;
        // If the difference between z-coordinates is greater than the difference between x-coordinates for first/last nodes
        if (Math.Abs(firstNode.z - lastNode.z) > Math.Abs(firstNode.x - lastNode.x))
        {
            // Rotate the full map camera 90 degrees in the z axis
            currentRotation = Quaternion.Euler(90, 0, 90);
        }
        else
        {
            // Set the full map camera to default rotation
            currentRotation = Quaternion.Euler(90, 0, 0);
        }
        // Update full map camera rotation
        GameObject.Find("FullMapCamera").transform.rotation = currentRotation;
    }

    // *****************************************************************************************
    //  OnRenderObject Method
    //
    /// <summary>
    ///     Calls other methods when full map camera object is rendered
    ///     </summary>
    //
    // *****************************************************************************************
    void OnRenderObject()
    {
        ResizeFullMap();
    }
}