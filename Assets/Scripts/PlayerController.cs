using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using gta_background_traffic_v2;
using System;
using System.IO;
using IniParser;
using IniParser.Model;
using System.Diagnostics;

/// <summary>
/// Controller for player behaviour
/// </summary>
public class PlayerController : MonoBehaviour
{
    private WorldMap world; // World map represented as a Mapbox object
    List<string> path; // Path that the player will travel on as a consecutive list of node IDs
    AVController av; // The car controller of the player's car
    Graph g; // Map of the world as a logical graph
    List<string> startIDs; // Holds all starting node IDs for the player
    List<string> endIDs; // Holds all end node IDs for the player
    GameObject PathGameObject; // Holds the player's path gameobject

    IniData configData; // Object holding data about the game's configuration

    /// <summary>
    /// Called before the application is started to initialize basic objects
    /// </summary>
    void Awake()
    {
        // Initialize objects
        GameObject go = GameObject.Find("CitySimulatorMap");
        world = (WorldMap)go.GetComponent<WorldMap>();
        g = GameObject.Find("Graph").GetComponent<GraphController>().graph;
        GameObject PlayerObject = GameObject.FindGameObjectWithTag("Player");
        PathGameObject = PlayerObject.transform.Find("Path").gameObject;

        // Read in start and end nodes from each round
        startIDs = new List<string>();
        endIDs = new List<string>();
        readInPlayerNodes();

        //Pathing(1);
    }

    /// <summary>
    /// Reads in the config files and sets the player start and end nodes for each round
    /// </summary>
    private void readInPlayerNodes()
    {
        // Read in config file
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
        configData = parser.ReadFile(configFilepath);

        // For each round
        PlayerNodeClass playerNodeObject;
        int numRounds;
        int.TryParse(configData["Session"]["numberOfRounds"], out numRounds);
        for (int i = 1; i <= numRounds; i++)
        {
            // Read in start and end data for each player
            string filepath;
            if (UnityEngine.Debug.isDebugBuild)
            {
                filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/playerPaths/session" + i + ".json";
            }
            else
            {
                filepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/playerPaths/session" + i + ".json";
            }
            StreamReader reader = new StreamReader(filepath);
            playerNodeObject = PlayerNodeClass.CreateFromJSON(reader.ReadToEnd());

            // Add to the ID lists
            startIDs.Add(playerNodeObject.originNode);
            endIDs.Add(playerNodeObject.destinationNode);
        }
    }

    /// <summary>
    /// Recreates the player path for the round
    /// </summary>
    /// <param name="roundNum">Integer representing the round's number (indexes from 0)</param>
    public void Pathing(int roundNum)
    {
        path = g.PathToFrom(startIDs[roundNum], endIDs[roundNum]);
        av = GetComponentInChildren<AVController>();
        clearPath();
        makePath(path);
    }

    /// <summary>
    /// Start is called before the first frame update to instantiate dynamic objects
    /// </summary>
    void Start()
    {
        GameObject PlayerObject = GameObject.FindGameObjectWithTag("Player");
        PathGameObject = PlayerObject.transform.Find("Path").gameObject;

        av = GetComponentInChildren<AVController>();

        // Set spawn
        GameObject SpawnObject = PlayerObject.transform.Find("Spawn").gameObject;
        GameObject SpawnPointObject = SpawnObject.transform.Find("SpawnPoint").gameObject;
        g = GameObject.Find("Graph").GetComponent<GraphController>().graph;

    }

    /// <summary>
    /// Destroys all node gameobjects under the player's path gameobject
    /// </summary>
    public void clearPath()
    {
        // Store children to destroy in an array
        GameObject[] destChildren = new GameObject[PathGameObject.transform.childCount - 1];
        bool isFirst = true;
        int i = 0;
        foreach (Transform child in PathGameObject.transform)
        {
            if(isFirst)
            {
                isFirst = false;
                continue;
                
            } else
            {
                destChildren[i] = child.gameObject;
                i += 1;
            }
            
        }

        // Destroy all children
        foreach (GameObject child in destChildren)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// Generates a new path for the player based on the path provided
    /// If it is a new round then all AI car paths will be reset too
    /// </summary>
    /// <param name="path">List of all node IDs for the player to use as a path</param>
    /// <param name="newRound">Boolean indicating whether the path is being made for a new round</param>
    /// <param name="newCurrentNode">Integer indicating which is the next current node (used for changing existing paths)</param>
    public void makePath(List<string> path, bool newRound = true, int newCurrentNode = 0)
    {
        // Generate Node Elements for path
        GameObject NodeGameObject = PathGameObject.transform.GetChild(0).gameObject;
        bool isFirst = true;
        Vector3 nodePos;
        foreach (string nodeID in path)
        {
            Graph.Node node = g.nodeIdNodeDict[nodeID];
            Vector2d xy = world.toXY(node.lat, node.lon);
            nodePos = new Vector3((float)xy[0], 0.71f, (float)xy[1]);

            if (isFirst)
            {
                NodeGameObject.transform.position = (nodePos);
                isFirst = false;
            }
            else
            {
                GameObject childObject = Instantiate(NodeGameObject) as GameObject;
                childObject.transform.position = (nodePos);
                childObject.transform.SetParent(PathGameObject.transform);
            }
        }

        av = GetComponentInChildren<AVController>();
        av.setPath(path);
        av.currentNode = newCurrentNode;
        av.resetPaths(newRound);
        av.setOneWayRoads(path);
    }
    
    /// <summary>
    /// Internal class to hold player information
    /// </summary>
    internal class PlayerNodeClass
    {
        public string originNode;
        public string destinationNode;

        public static PlayerNodeClass CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<PlayerNodeClass>(jsonString);
        }
    }
}
