#region Using
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

#endregion

// *********************************************************************************************
//  DrawPath Class
//
/// <summary>
///     Provides class for drawing path on minimap and full map.
///     </summary> 
//
// *********************************************************************************************
public class DrawPath : MonoBehaviour
{
    // Initialises list for nodes
    private List<Transform> nodes = new List<Transform>();
    // Initialises LineRenderer for drawing path
    private LineRenderer pathRenderer;
    GameObject PlayerObject;

    // *****************************************************************************************
    //  DrawLinesOnMap Method
    //
    /// <summary>
    ///     Draws the path made up of nodes on the minimap and full map
    ///     </summary>
    //
    // *****************************************************************************************
    void DrawLinesOnMap()
    {
        // Gets current player object
        PlayerObject = GameObject.FindGameObjectWithTag("Player");
        // Gets LineRenderer
        pathRenderer = GetComponent<LineRenderer>();
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
        // Sets position counter of LineRenderer to number of nodes in path
        pathRenderer.positionCount = pathCount;

        // Loops through each node in the path
        for (int x = 0; x < pathCount; x++)
        {
            // Set the position of the LineRenderer to the current node
            pathRenderer.SetPosition(x, new Vector3(PathObject.transform.GetChild(x).transform.position.x,
                4, PathObject.transform.GetChild(x).transform.position.z));
        }
        // Sets thickness of path
        pathRenderer.startWidth = 15f;
        pathRenderer.endWidth = 15f;
    }
    // *****************************************************************************************
    //  OnRenderObject Method
    //
    /// <summary>
    ///     Calls other methods when player path object is rendered
    ///     </summary>
    //
    // *****************************************************************************************
    void OnRenderObject()
    {
        DrawLinesOnMap();
    }
}