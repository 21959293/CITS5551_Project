using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using gta_background_traffic_v2;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using IniParser;
using IniParser.Model;

/// <summary>
/// Controls the traffic lights
/// </summary>
public class TrafficLightController : MonoBehaviour
{

    private WorldMap world; // Map representing the world
    private Graph g; // Graph representing the world as a logical graph
    
    private HashSet<string>[] trafficLightDirections; // Array of HashSets of Strings which hold all traffic light nodes for each direction (0-4]
    private HashSet<string> trafficLightSet; // HashSet of Strings which holds all traffic light nodes
    private int prevTrafficLightIndex = 0; // The previous traffic light index (0-4]
    private int trafficLightIndex = 1; // The current traffic light index (0-4]
    private float startTime; // Holds when the traffic lights changed
    private float waitTime = 10f; // How long to wait in seconds for the traffic lights to change
    private float changeDelayTime = 1f; // How long to hold a yellow light for the traffic lights to change
    private bool delaySet = false;
    private Dictionary<string, int> trafficLightDirectionMap; // Map that stores the directionality of each traffic light
    private HashSet<GameObject>[] trafficLightDirectionObjects; // Holds all traffic light game objects using their index as direction (0-4]

    private IniData configData; // Object that holds game config information

    public Material greenLightMaterial; // Holds the material of Green Lights
    public Material yellowLightMaterial; // Holds the material of Yellow Lights
    public Material redLightMaterial; // Holds the material of Red Lights

    /// <summary>
    /// Loads data before the application starts
    /// </summary>
    void Awake()
    {
        // Load in graph
        GameObject go = GameObject.Find("CitySimulatorMap");
        world = (WorldMap)go.GetComponent<WorldMap>();
        trafficLightDirectionMap = new Dictionary<string, int>();

        // Initialize trafficLightDirectionObjects
        trafficLightDirectionObjects = new HashSet<GameObject>[4];
        for (int i = 0; i < 4; i++)
        {
            trafficLightDirectionObjects[i] = new HashSet<GameObject>();
        }

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
        float.TryParse(configData["Session"]["trafficLightTransitionTime"], out waitTime);
    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // Get all traffic lights
        g = GameObject.Find("Graph").GetComponent<GraphController>().graph;
        trafficLightDirections = g.trafficLightDirections;
        trafficLightSet = g.trafficLights;

        UnityEngine.Debug.Log("trafficLightIndex: " + trafficLightIndex);

        // Make all possible traffic lights at the start (surprisingly quick)
        foreach (Graph.Node node in g.nodes)
        {
            foreach (Graph.Node.Neighbour neighbour in node.neighbours)
            {
                if (trafficLightSet.Contains(neighbour.neighbourId)) isLightGreen(node.nodeId, neighbour.neighbourId);
            }
        }

        // Set green to green initially
        setColour(trafficLightIndex, "green");
    }

    /// <summary>
    /// On each frame update, checks whether the lights should be changes
    /// </summary>
    void Update()
    {
        // Orange light to allow cars to finish crossing
        if (Time.time > startTime + waitTime)
        {
            delaySet = true;
            setColour(trafficLightIndex, "yellow");
        }

        // Change lights
        if (Time.time > startTime + waitTime + changeDelayTime)
        {
            // Update traffic data
            startTime = Time.time;

            prevTrafficLightIndex = trafficLightIndex;
            trafficLightIndex = (trafficLightIndex + 1 == 4 ? 0 : trafficLightIndex + 1);
            //UnityEngine.Debug.Log("trafficLightIndex: " + trafficLightIndex);
            delaySet = false;

            // Update light colours
            setColour(prevTrafficLightIndex, "red");
            setColour(trafficLightIndex, "green");
        }
    }

    /// <summary>
    /// Sets all of the chosen index's traffic lights to the colour selected
    /// </summary>
    /// <param name="index">Integer representing the directionality index to change</param>
    /// <param name="colour">string representing the colour to change the objects to</param>
    private void setColour(int index, string colour)
    {
        Material mat;
        if (colour == "green")
        {
            mat = greenLightMaterial;
        }
        else if (colour == "yellow")
        {
            mat = yellowLightMaterial;
        }
        else
        {
            mat = redLightMaterial;
        }

        foreach (GameObject light in trafficLightDirectionObjects[index])
        {
            light.GetComponent<Renderer>().material = mat;
        }
    }

    /// <summary>
    /// Calculates the direction of a novel fromNode toNode pair where:
    ///     0 --> North
    ///     1 --> East
    ///     2 --> South
    ///     3 --> West
    /// </summary>
    /// <param name="fromNode">String representing the first node</param>
    /// <param name="toNode">String representing the second node</param>
    /// <returns>Integer indicating directionality</returns>
    private int calcBearing(string fromNode, string toNode)
    {
        // Load node info
        Graph.Node startNode = g.nodeIdNodeDict[fromNode];
        double[] start = new double[2];
        start[0] = startNode.lat * Math.PI/180;
        start[1] = startNode.lon * Math.PI / 180;

        Graph.Node endNode = g.nodeIdNodeDict[toNode];
        double[] end = new double[2];
        end[0] = endNode.lat * Math.PI / 180;
        end[1] = endNode.lon * Math.PI / 180;

        // Calculate the bearing
        double x = Math.Sin(end[1] - start[1]) * Math.Cos(end[0]);
        double y = Math.Cos(start[0]) * Math.Sin(end[0]) - Math.Sin(start[0]) * Math.Cos(end[0]) * Math.Cos(end[1] - start[1]);
        double theta = Math.Atan2(y, x);
        double brng = (theta * 180 / Math.PI + 360) % 360;

        //UnityEngine.Debug.Log("brng: " + brng);

        // Calculate the direction
        int direction = -1;
        if (brng >= 315 || brng < 45)
        {
            // North
            direction = 0;
        }
        else if (brng >= 45 && brng < 135)
        {
            // East
            direction = 1;
        }
        else if (brng >= 135 && brng < 225)
        {
            // South
            direction = 2;
        }
        else
        {
            // West
            direction = 3;
        }

        // Make traffic light object appear
        createTrafficLightObject(fromNode, toNode, direction);

        return direction;
    }

    /// <summary>
    /// Creates a traffic light object at a fixed distance from the toNode representing a traffic light. This assumes there are no nodes in the intersection.
    /// </summary>
    /// <param name="fromNode">String representing the first node to consider</param>
    /// <param name="toNode">String representing the second node to consider</param>
    /// <param name="direction">Integer representing which way the traffic lights should face (I think)</param>
    /// <param name="distanceFromMiddle">Float representing an offset towards the car (ensures it can see the object)</param>
    /// <param name="distanceToLeft">Float representing an offset to the left of the car (ensures it can see the object)</param>
    /// <param name="height">Float representing how high the object should spawn (ensures it can see the object)</param>
    private void createTrafficLightObject(string fromNode, string toNode, int direction, float distanceFromMiddle = 10f,float distanceToLeft = 3f, float height = 3f)
    {
        // Get both nodes
        Graph.Node startNode = g.nodeIdNodeDict[fromNode];
        Graph.Node trafficNode = g.nodeIdNodeDict[toNode];

        // Convert to world coordinates
        Vector2d startxy = world.toXY(startNode.lat, startNode.lon);
        Vector3 startNodePos = new Vector3((float)startxy[0], height, (float)startxy[1]);
        Vector2d trafficxy = world.toXY(trafficNode.lat, trafficNode.lon);
        Vector3 trafficNodePos = new Vector3((float)trafficxy[0], height, (float)trafficxy[1]);

        // Calculate where traffic light should be placed
            // First calculate offset from traffic node
        Vector3 dir = -1*((trafficNodePos - startNodePos).normalized);
            // The calculate offset to the left
        Vector3 midpoint = Vector3.Lerp(trafficNodePos, startNodePos, 0.5f);
        midpoint[1] = 0.0f;
        Vector3 leftDir = Vector3.Cross(trafficNodePos - midpoint, startNodePos - midpoint);
        leftDir = Vector3.Normalize(leftDir);
            // Apply change
        Vector3 trafficLightPos = trafficNodePos + (dir * distanceFromMiddle) + (leftDir * distanceToLeft);


        // Spawn traffic light block
        GameObject trafficLightObject = GameObject.Find("TrafficLight");
        GameObject newTrafficLightObject = Instantiate(trafficLightObject) as GameObject;
        newTrafficLightObject.transform.SetParent(gameObject.transform);
        newTrafficLightObject.transform.position = trafficLightPos;
        newTrafficLightObject.transform.rotation = Quaternion.LookRotation(-1 * ((trafficNodePos - startNodePos).normalized));
        newTrafficLightObject.GetComponent<Renderer>().material = redLightMaterial;

        // Add traffic light object to correct direction
        trafficLightDirectionObjects[direction].Add(newTrafficLightObject);
    }

    /// <summary>
    /// Returns true if the light is green
    /// </summary>
    /// <param name="fromNode">String representing the first node</param>
    /// <param name="toNode">String representing the second node</param>
    /// <returns>Boolean for whether the light is green</returns>
    private bool isLightGreen(string fromNode, string toNode)
    {
        // Return false if in-transion NOTE: Moving this to trafficCheck breaks it
        if (delaySet) return false;

        // Get the direction of the light
        int checkDirection = -1;
        if (!trafficLightDirectionMap.ContainsKey(fromNode + toNode)) // Avoids recalculation if possible since it is cheaper
        {
            checkDirection = calcBearing(fromNode, toNode);
            trafficLightDirectionMap.Add(fromNode + toNode, checkDirection);
        } 
        else
        {
            checkDirection = trafficLightDirectionMap[fromNode + toNode];
        }

        return checkDirection == trafficLightIndex;
    }


    /// <summary>
    /// Returns true if the car should stop (If it is a traffic light AND is not green)
    /// </summary>
    /// <param name="fromNode">String representing the first node</param>
    /// <param name="toNode">String representing the second node</param>
    /// <returns>Boolean for whether the car should stop</returns>
    public bool trafficCheck(string fromNode, string toNode)
    {
        //UnityEngine.Debug.Log("trafficLightSet: " + trafficLightSet.Count);
        if (trafficLightSet.Contains(toNode))
        {
            //UnityEngine.Debug.Log("Checking traffic light " + fromNode + "->" + toNode + ": " + isLightGreen(fromNode, toNode));
            return !isLightGreen(fromNode, toNode);
        }
        //UnityEngine.Debug.Log("Checking " + fromNode + "->" + toNode + ": NOT A TRAFFIC LIGHT");
        return false;
    }

}
