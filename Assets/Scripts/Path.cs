using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Draws the path on the minimap
/// </summary>
public class Path : MonoBehaviour {

    public Color lineColor;

    private List<Transform> nodes = new List<Transform>();

    //Draws path line for edit mode
    void OnDrawGizmos() {
        Gizmos.color = lineColor;
        Transform[] pathTransforms = GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();
        string objectName = gameObject.name;

        //Only keep child object positions
        for (int i = 0; i < pathTransforms.Length; i++) {
            if(pathTransforms[i] != transform) {
                nodes.Add(pathTransforms[i]);
            }
        }

        //Draw path
        for(int i = 0; i < nodes.Count; i++) {
            Vector3 currentNode = nodes[i].position;
            Vector3 previousNode = Vector3.zero;

            if (i > 0) {
                previousNode = nodes[i - 1].position;
            } else if(i == 0 && nodes.Count > 1) {
                previousNode = nodes[nodes.Count - 1].position;
            }
        }
    }

}
