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

public class AICarsController : MonoBehaviour
{
    private WorldMap world;
    private Graph g;
    private int numAICars = 500;
    private bool useCachedPath = true;
    //private List<Path> paths = new List<Path>();
    private List<DumbCar> dumbCars = new List<DumbCar>();
    private string mapDir = "Maps/SignalTest/"; //"Maps/New York/";
    private List<SmartCar> smartCars = new List<SmartCar>();
    //Pick depending on map

    //@todo make this pickup the actual player start instead of hardcoding
    public string playerNodeID = "dr5rmqt7y20";

    public GameObject AICarObject;

    private int numSmartRemaining;
    private IniData configData;
    void Awake()
    {
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

        if(useCachedPath) generateExistingPaths();
        else createPaths();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void generateExistingPaths()
    {
        // Read in file
        string filepath;
        string fp_smart;
        if (UnityEngine.Debug.isDebugBuild)
        {
            filepath = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/paths.json";
            fp_smart = "Assets/Resources/Maps/" + configData["Map"]["name"] + "/smartODPairs.json";
        }
        else
        {
            filepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/paths.json";
            fp_smart = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/Maps/" + configData["Map"]["name"] + "/smartODPairs.json";
        }
        using (StreamReader r = new StreamReader(filepath))
        {
            string json = r.ReadToEnd();
            Cars c = JsonConvert.DeserializeObject<Cars>(json);
            dumbCars = c.cars;
            //dumbCars = JsonConvert.DeserializeObject<List<DumbCar>>(json);
        }
        // using (StreamReader r = new StreamReader(fp_smart))
        // {
        //     string json = r.ReadToEnd();
        //     SmartODPairs s = JsonConvert.DeserializeObject<SmartODPairs>(json);
        //     smartCars = s.cars;
            
        //     //smartCars = JsonConvert.DeserializeObject<List<SmartCar>>(json);
        // }


        // Make an AI car using each path
        GameObject AICarsObject = GameObject.Find("AICars");
        int idCtr = 0;
        foreach (DumbCar car in dumbCars) 
        {
            GameObject newAICarObject = Instantiate(AICarObject) as GameObject;
            newAICarObject.transform.SetParent(AICarsObject.transform);
            setAICarParams(newAICarObject, new List<string>(car.path), idCtr);
            idCtr++;
        }
    }

    private void createPaths()
    {
        // Make an array of AICars
        int numNodes = g.nodes.Count;
        HashSet<string> startingNodeIds = new HashSet<string>();
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
            } while (startingNodeIds.Contains(startID) || startID == playerNodeID);
            startingNodeIds.Add(startID);
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
            setAICarParams(newAICarObject, path, i);
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

    private void setAICarParams(GameObject CarObject, List<string> path, int dumbCarID)
    {
        // Make path from start to end
        GameObject PathGameObject = CarObject.transform.Find("Path").gameObject;
        if (!useCachedPath)
        {
            //Path thisPath = new Path() { nodeIDs = path };
            DumbCar thisCar = new DumbCar();
            thisCar.id = dumbCarID;
            thisCar.path = path.ToArray();
            dumbCars.Add(thisCar);
            //paths.Add(thisPath);
        }

        makePath(PathGameObject, g, path);

        AVController av = CarObject.GetComponentInChildren<AVController>();
        Vector3 offset = av.setOneWayRoads(path);
        av.setPath(path);

        // Set spawn
        GameObject SpawnObject = CarObject.transform.Find("Spawn").gameObject;
        GameObject SpawnPointObject = SpawnObject.transform.Find("SpawnPoint").gameObject;
        Graph.Node startingNode = g.nodeIdNodeDict[path[0]];
        UnityEngine.Debug.Log("StartingNode nodeId: " + startingNode.nodeId);
        UnityEngine.Debug.Log("StartingNode Lat: " + startingNode.lat);
        UnityEngine.Debug.Log("StartingNode Lon: " + startingNode.lon);
        Vector2d xy = world.toXY(startingNode.lat, startingNode.lon);
        UnityEngine.Debug.Log("X: " + xy[0]);
        UnityEngine.Debug.Log("Y: " + xy[1]);
        //SpawnPointObject.transform.position = new Vector3((float)xy[0], 0.71f, (float)xy[1]) + transform.InverseTransformPoint(offset);
    }

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

    // internal class Path
    // {
    //     public List<string> nodeIDs { get; set; }
    // }

    internal class DumbCar
    {
        public int id { get; set; }
        public string startNode { get; set; }
        public string destinationNode { get; set; }
        public string[] path { get; set; }
    }

    internal class Cars 
    {
        public List<DumbCar> cars { get; set; }
    }

    internal class SmartODPairs
    {
        public List<SmartCar> cars { get; set; }
    }

    internal class SmartCar
    {
        public int id { get; set; }
        public string startNode { get; set; }
        public string destinationID { get; set; }
    }
}
