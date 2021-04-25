using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using gta_background_traffic_v2;
using IniParser;
using IniParser.Model;

/// <summary>
/// Controls the logical graph representation of the map
/// </summary>
public class GraphController : MonoBehaviour
{
    public Graph graph; // Object representing the graph

    /// <summary>
    /// Before the application starts, the config data is used to generate the correct graph based on the map specified
    /// </summary>
    void Awake()
    {
        // Grab config data
        string configFilepath;
        if (UnityEngine.Debug.isDebugBuild)
        {
            configFilepath = "Assets/Resources/config.ini";
        }
        else
        {
            configFilepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/config.ini";
        }
        var parser = new FileIniDataParser();
        IniData configData = parser.ReadFile(configFilepath);

        graph = new Graph();
        string filepath;
        if (UnityEngine.Debug.isDebugBuild)
        {
            filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/nodes.json"; //"nodesWayIDAdded.json";
        }
        else
        {
            filepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/nodes.json"; //"nodesWayIDAdded.json";
        }
        graph.ReadInGraphFromJSON(filepath);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
