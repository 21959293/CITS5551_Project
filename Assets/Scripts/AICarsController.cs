using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using gta_background_traffic_v2;
using System;
using Newtonsoft.Json;
using System.IO;
using IniParser;
using IniParser.Model;

/// <summary>
/// Controls the behaviour of all AI cars including spawning 
/// </summary>
public class AICarsController : MonoBehaviour
{
    private WorldMap world; // The map as an object
    private Graph g; // The map as a logical graph
    private int numAICars = 500; // The number of cars to spawn if not using cached path
    private bool useCachedPath = true; // Whether to generate paths or not (Should be set to TRUE if using map generator due to performance issues)
    private List<DumbCar> dumbCars = new List<DumbCar>(); // Holds all cars that have a predefined route
    private string mapDir = "Maps/SignalTest/"; // Relative filepath of the map to use
    private List<SmartCar> smartCars = new List<SmartCar>(); // Holds all cars that dynamically update their route
    //Pick depending on map

    //@todo make this pickup the actual player start instead of hardcoding
    private HashSet<string> takenStartingNodes;

    public GameObject AICarObject; // The template gameobject used to spawn more AI cars
    private int numSmartRemaining; // Tracks the number of cars still to spawn
    private int numSmartSpawned; // Tracks the number of cars that have been spawned 
    private IniData configData; // An object holding the game config data

    /// <summary>
    /// Is called before the application starts (NOTE: This will have issues finding any dynamically created objects as they do not exist yet)
    /// </summary>
    void Awake()
    {
        // First sets up the gameworld map
        GameObject go = GameObject.Find("CitySimulatorMap");
        world = (WorldMap)go.GetComponent<WorldMap>();
        g = GameObject.Find("Graph").GetComponent<GraphController>().graph;

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
        configData = parser.ReadFile(configFilepath);

        // Grab map data
        string mapAttributesPath;
        if (UnityEngine.Debug.isDebugBuild)
        {
            mapAttributesPath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/config.ini";
        }
        else
        {
            mapAttributesPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/config.ini";
        }
        IniData mapAttributes = parser.ReadFile(mapAttributesPath);

        // Grab player path data
        takenStartingNodes = new HashSet<string>();
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
            if(!takenStartingNodes.Contains(playerNodeObject.originNode)) takenStartingNodes.Add(playerNodeObject.originNode);
        }

        // Set the random seed according to the config file
        int.TryParse(configData["Session"]["randomSeed"], out var randSeed);
        UnityEngine.Random.InitState(randSeed);
        UnityEngine.Debug.Log("randomSeed: " + randSeed);

        if (useCachedPath) generateExistingPaths();
        else createPaths();

        numSmartRemaining = smartCars.Count;
        numSmartSpawned = 0;
        InvokeRepeating("spawnSmartTraffic", 15f, 10f); // Repeatedly spawns smart traffic
    }

    /// <summary>
    /// Creates all AI cars and sets their path according to the existing path files for the config specified
    /// </summary>
    private void generateExistingPaths()
    {
        // Read in file according to map selected
        string filepath = string.Empty;
        string fp_smart = string.Empty;
        if (UnityEngine.Debug.isDebugBuild)
        {
            filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/paths.json";
            fp_smart = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/smartODPairs.json";
        }
        else
        {
            try
            {
                filepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/paths.json";
                fp_smart = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/smartODPairs.json";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e.ToString());
            }
        }
        using (var r = new StreamReader(filepath))
        {
            string json = r.ReadToEnd();
            Cars c = JsonConvert.DeserializeObject<Cars>(json);
            dumbCars = c.cars;
        }
        using (StreamReader r = new StreamReader(fp_smart))
        {
            string json = r.ReadToEnd();
            SmartODPairs s = JsonConvert.DeserializeObject<SmartODPairs>(json);
            smartCars = s.cars;
        }

        // Make an AI car using each path
        GameObject AICarsObject = GameObject.Find("AICars");
        int idCtr = 0;
        foreach (DumbCar car in dumbCars) 
        {
            if (car.path.Count <= 1) continue;
            if (takenStartingNodes.Contains(car.path[0])) continue;
            GameObject newAICarObject = Instantiate(AICarObject) as GameObject;
            newAICarObject.transform.SetParent(AICarsObject.transform);
            setAICarParams(newAICarObject, new List<string>(car.path), idCtr, false);
            idCtr++;
            takenStartingNodes.Add(car.path[0]);
        }
    }

    /// <summary>
    /// Crates all AI cars and generates their paths randomly up to the limit without caching (REPLACE BY MAP BUILDER)
    /// </summary>
    private void createPaths()
    {
        // Make an array of AICars
        int numNodes = g.nodes.Count;
        GameObject AICarsObject = GameObject.Find("AICars");

        int randNum;
        string startID;
        string endID;
        for (int i = 0; i < numAICars; i++)
        {
            // Choose a starting and end node where start must be unique [NOTE: will fail if you try to make more AI cars than nodes exist] @todo: can extend by a spawn radius
            do
            {
                randNum = UnityEngine.Random.Range(0, numNodes - 1);
                startID = g.nodes[randNum].nodeId;
            } while (takenStartingNodes.Contains(startID));
            takenStartingNodes.Add(startID);
            randNum = UnityEngine.Random.Range(0, numNodes - 1);
            endID = g.nodes[randNum].nodeId;
            //string startID = "qd66hjswu6z";

            List<string> path = g.PathToFrom(startID, endID);
            if (path.Count == 1) continue; 
            /*
            while (path.Count == 1) {
                randNum = UnityEngine.Random.Range(0, numNodes - 1);
                endID = g.nodes[randNum].nodeId;
                randNum = UnityEngine.Random.Range(0, numNodes - 1);
                startID = g.nodes[randNum].nodeId; 
                path = g.PathToFrom(startID, endID);
            }
            */

            GameObject newAICarObject = Instantiate(AICarObject) as GameObject;
            newAICarObject.transform.SetParent(AICarsObject.transform);
            //setAICarParams(newAICarObject, startID, endID);
            setAICarParams(newAICarObject, path, i, false);
        }

        string json = JsonConvert.SerializeObject(dumbCars);
        // Output depending on mode
        // Pointless, I suppose unless this is added as an option to the config file
        if (UnityEngine.Debug.isDebugBuild)
        {
            System.IO.File.WriteAllText("Assets/Resources/Maps" + configData["Map"]["name"] + "/paths.json", json);
        }
        else
        {
            System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/paths.json", json);
        }
    }

    /// <summary>
    /// Sets the path for the car gameobject and places it in the world
    /// </summary>
    /// <param name="CarObject">GameObject representing the AI car</param>
    /// <param name="path">List of nodes for the car to travel to which form a path</param>
    /// <param name="dumbCarID">Integer identifying the car</param>
    /// <param name="smart">Boolean indicating whether the car is smart or not</param>
    private void setAICarParams(GameObject CarObject, List<string> path, int dumbCarID, bool smart)
    {
        // Make path from start to end
        GameObject PathGameObject = CarObject.transform.Find("Path").gameObject;
        if (!useCachedPath)
        {
            //Path thisPath = new Path() { nodeIDs = path };
            DumbCar thisCar = new DumbCar();
            thisCar.id = dumbCarID;
            //thisCar.path = path.ToArray();
            dumbCars.Add(thisCar); // NOTE: seems to add all cars whether they are dumb or not
            //paths.Add(thisPath);
        }

        makePath(PathGameObject, g, path);

        AVController av = CarObject.GetComponentInChildren<AVController>();
        Vector3 offset = av.setOneWayRoads(path);
        av.setPath(path);
        av.isSmart = smart;

        // Set spawn
        GameObject SpawnObject = CarObject.transform.Find("Spawn").gameObject;
        GameObject SpawnPointObject = SpawnObject.transform.Find("SpawnPoint").gameObject;
        Graph.Node startingNode = g.nodeIdNodeDict[path[0]];
        Vector2d xy = world.toXY(startingNode.lat, startingNode.lon);

        if (smart)
        {
            av.roundOver = false;
            av.TP2Start();
        }
        //SpawnPointObject.transform.position = new Vector3((float)xy[0], 0.71f, (float)xy[1]) + transform.InverseTransformPoint(offset);
    }

    /// <summary>
    /// Iteratively generates node gameobjects and adds them as children to a path gameobject
    /// </summary>
    /// <param name="PathGameObject">GameObject representing the path of an AI Car</param>
    /// <param name="g">Graph representing the world logically</param>
    /// <param name="path">List of strings representing nodes to travel in the path (must exist in the graph provided)</param>
    private void makePath(GameObject PathGameObject, Graph g, List<string> path)
    {
        // Generate Node Elements for path
        GameObject NodeGameObject = PathGameObject.transform.GetChild(0).gameObject;
        bool isFirst = true;
        Vector3 nodePos;
        //UnityEngine.Debug.Log("Size: " + path.Count);
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
    }

    /// <summary>
    /// Repeatedly spawns smart cars
    /// </summary>
    private void spawnSmartTraffic()
    {
        if (numSmartRemaining>0)
        {
            numSmartRemaining--;
            SmartPathing sp = new SmartPathing(g);
            SmartCar sc = smartCars[numSmartSpawned];
            List<string> path = sp.PathToFrom(sc.startNode, sc.destinationID);
            string startingNodeID = sc.startNode;

            numSmartSpawned++;

            if (!takenStartingNodes.Contains(startingNodeID))
            {
                takenStartingNodes.Add(startingNodeID);

                GameObject AICarsObject = GameObject.Find("AICars");
                GameObject newAICarObject = Instantiate(AICarObject) as GameObject;
                newAICarObject.transform.SetParent(AICarsObject.transform);
                setAICarParams(newAICarObject, path, 1000 + numSmartSpawned, true);
                //SET AI CAR PARAMS
                // Make path from start to end
                //GameObject PathGameObject = newAICarObject.transform.Find("Path").gameObject;
                //makePath(PathGameObject, g, path);

                //AVController av = newAICarObject.GetComponentInChildren<AVController>();
                //Vector3 offset = av.setOneWayRoads(path);
                //av.setPath(path);

                //// Set spawn
                //GameObject SpawnObject = newAICarObject.transform.Find("Spawn").gameObject;
                //GameObject SpawnPointObject = SpawnObject.transform.Find("SpawnPoint").gameObject;
                //Graph.Node startingNode = g.nodeIdNodeDict[path[0]];
                //UnityEngine.Debug.Log("StartingNode nodeId: " + startingNode.nodeId);
                //UnityEngine.Debug.Log("StartingNode Lat: " + startingNode.lat);
                //UnityEngine.Debug.Log("StartingNode Lon: " + startingNode.lon);
                //Vector2d xy = world.toXY(startingNode.lat, startingNode.lon);
                //UnityEngine.Debug.Log("X: " + xy[0]);
                //UnityEngine.Debug.Log("Y: " + xy[1]);
            }
        }
        else
        {
            CancelInvoke("spawnSmartTraffic");
        }
    }

    /// <summary>
    /// An object representing a typical dumb car in-code
    /// </summary>
    public class DumbCar    {
        public int id { get; set; } // Unique identifier for the car
        public string startNode { get; set; } // String representing the starting node for the car corresponding to a node on the Graph
        public string destinationNode { get; set; } // String representing the starting node for the car corresponding to a node on the Graph
        public List<string> path { get; set; }  // List of Strings representing the path for the car to travel corresponding to nodes on the Graph
    }

    /// <summary>
    /// An object representing multiple DumbCars
    /// </summary>
    public class Cars    {
        public List<DumbCar> cars { get; set; } 
    }


    /// <summary>
    /// Internally used object representing multiple smart cars
    /// </summary>
    internal class SmartODPairs
    {
        public List<SmartCar> cars { get; set; }
    }

    /// <summary>
    /// Internally used object representing a typical smart car in-code
    /// </summary>
    internal class SmartCar
    {
        public int id { get; set; } // Unique identifier for the car
        public string startNode { get; set; } // String representing the starting node for the car corresponding to a node on the Graph
        public string destinationID { get; set; }// String representing the starting node for the car corresponding to a node on the Graph
    }

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
