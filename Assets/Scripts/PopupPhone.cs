using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using gta_background_traffic_v2;
using IniParser;
using IniParser.Model;
using System.Diagnostics;
using System;
using System.Security.Policy;

/// <summary>
/// Object representing the popup phone which acts as the menu in the simulation
/// </summary>
public class PopupPhone : MonoBehaviour
{
    public Text costText; // Holds the current text to display Cost
    public Text etaText; // Holds the current text to display ETA Time
    public Button yesButton; // Clickable button to indicate acceptance
    public Button noButton; // Clickable button to indicate declining
    public SessionManager session; // Holds the current session
    public float movementSpeed = 100f; // How fast the phone should move up the screen
    public float phoneTop = 100f; // Indicates how high the phone should be when it is showing
    public float phoneBottom = -300f; // Indicates how high the phone should be when it is NOT showing
    private bool rising; // Whether the phone is being animated to rise up
    private float timeElapsed; // Tracks time elapsed
    private Graph g; // Holds a representation of the map as a logica graph

    private SessionManager sm;
    private IniData configData; // Holds configuration settings

    /// <summary>
    /// Prepares the phone before the first frame of the game
    /// </summary>
    void Start()
    {
        fastClosePhone();
        noButton.onClick.AddListener(clickNoButton);
        yesButton.onClick.AddListener(clickYesButton);

        sm = GameObject.Find("SessionManager").GetComponent<SessionManager>();
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
    }

    /// <summary>
    /// On each frame update to change the position of the phone to set limits based on whether it should be rising or lowering
    /// </summary>
    private void Update()
    {
        Vector3 newPosition = transform.localPosition;

        //Change position of phone, update time while rising
        if (rising)
        {
            timeElapsed += Time.deltaTime;
            newPosition += new Vector3(0, movementSpeed * Time.deltaTime, 0);
        }
        else
        {
            newPosition -= new Vector3(0, movementSpeed * Time.deltaTime, 0);
        }

        //Keep phone within range
        newPosition.y = Mathf.Min(newPosition.y, phoneTop);
        newPosition.y = Mathf.Max(newPosition.y, phoneBottom);

        //If phone reaches top buttons are interactable, if not they're unusable
        if (newPosition.y == phoneTop)
        {
            noButton.interactable = true;
            yesButton.interactable = true;
        }
        else
        {
            noButton.interactable = false;
            yesButton.interactable = false;
        }

        //Make phone disappear if bottom reached, useful for audio too
        if (newPosition.y == phoneBottom)
        {
            gameObject.SetActive(false);
        }

        transform.localPosition = newPosition;
    }

    /// <summary>
    /// Sends a message that the user has rejected the prompt and closes the phone
    /// </summary>
    void clickNoButton()
    {
        session.rejectPrompt(timeElapsed);
        closePhone();
    }

    /// <summary>
    /// Sends a message that the user has accepted the prompt, updates the game and closes the phone
    /// </summary>
    void clickYesButton()
    {
        session.acceptPrompt(timeElapsed);
        changePath();
        closePhone();
    }

    /// <summary>
    /// Changes the player's path by picking a new intermediate point and constructing a path that goes via it
    /// </summary>
    public void changePath()
    {
        // Get next location and end locations
        GameObject playerCarObject = GameObject.FindGameObjectWithTag("DriverCar");
        AVController playerAV = playerCarObject.GetComponent<AVController>();
        List<string> pathList = playerAV.pathList;
        int currentNode = playerAV.currentNode;

        string startNodeID = pathList[0];
        string nextNodeID = pathList[currentNode];
        string endNodeID = pathList[pathList.Count - 1];

        // Make a pathSet from pathList for faster lookups
        HashSet<string> pathSet = new HashSet<string>();
        foreach (string str in pathList)
        {
            pathSet.Add(str);
        }

        // Select an intermediate point
        string intNodeID = selectIntermediateNode(nextNodeID, pathSet);

        // Construct the path
        List<string> pathToNext = g.PathToFrom(startNodeID, nextNodeID);
        List<string> newPath = g.PathToFrom(nextNodeID, intNodeID);
        List<string> pathToEnd = g.PathToFromExclude(intNodeID, endNodeID, newPath[newPath.Count-2]);

        int i = 0;
        foreach (string str in newPath)
        {
            if (i > 0) pathToNext.Add(str);
            i++;
        }
        i = 0;
        foreach (string str in pathToEnd)
        {
            if (i > 0) pathToNext.Add(str);
            i++;
        }

        // Replace the player path with the new script
        GameObject PlayerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerController pc = PlayerObject.GetComponent<PlayerController>();
        pc.clearPath();
        pc.makePath(pathToNext, false, currentNode);
    }

    /// <summary>
    /// Selects a random intermediate node that is approximately a predefined distance away and is not currently on the path
    /// </summary>
    /// <param name="nextNodeID">String representing the Node ID of the next node on the player's path</param>
    /// <param name="pathSet">Hashset Strings representing all Node IDs that are currently on the player's path</param>
    /// <returns>String representing the Node ID of the selected intermediate node</returns>
    public string selectIntermediateNode(string nextNodeID, HashSet<string> pathSet)
    {
        // Find distance desired
        double distanceDesired;
        string sessionRoundString = "SessionRound" + sm.roundNumber;
        double.TryParse(configData[sessionRoundString]["additionalStopDistance"], out distanceDesired);
        distanceDesired = distanceDesired / 1000; // Convert from m to km

        //UnityEngine.Debug.Log("distanceDesired: " + distanceDesired);

        // Find all nodes within that distance +- some error margin
        Dictionary<string, double> nodeToDist = new Dictionary<string, double>(); // Maps nodeID to distance from nextNode
        Queue<string> neighbours = new Queue<string>(); // Queue to hold next node and its parent
        HashSet<string> nodesExamined = new HashSet<string>(); // Set to hold nodes already 

        // Set up search from next node
        Graph.Node node = g.nodeIdNodeDict[nextNodeID];
        nodeToDist.Add(nextNodeID, 0f);
        nodesExamined.Add(nextNodeID);
        bool br = false;
        string nodeID;
        string selectedNode = "";

        double initialMin = Double.MaxValue; // Just to make sure the smallest one is chosen if nearby
        foreach (Graph.Node.Neighbour neighbour in node.neighbours)
        {
            //UnityEngine.Debug.Log("neighbour.neighbourId " + neighbour.neighbourId + " --> " + neighbour.distance);
            nodeToDist.Add(neighbour.neighbourId, neighbour.distance);
            neighbours.Enqueue(neighbour.neighbourId);
            nodesExamined.Add(neighbour.neighbourId);
            if(neighbour.distance > distanceDesired && !pathSet.Contains(neighbour.neighbourId) && neighbour.distance < initialMin) {
                selectedNode = neighbour.neighbourId;
                br = true;
            }
        }


        // Perform a BFS until the distance has been reached
        while (neighbours.Count != 0 && !br)
        {
            // Get next node to examine
            nodeID = neighbours.Dequeue();
            node = g.nodeIdNodeDict[nodeID];

            // Look through neighbours
            foreach (Graph.Node.Neighbour neighbour in node.neighbours)
            {
                // Check if passed threshold AND doesn't lie on path
                double totalDistance = nodeToDist[nodeID] + neighbour.distance;
                if (totalDistance > distanceDesired && !pathSet.Contains(neighbour.neighbourId))
                {
                    //UnityEngine.Debug.Log("FINISHED! neighbour.neighbourId " + neighbour.neighbourId + " --> " + totalDistance);
                    //if(!nodeToDist.ContainsKey(neighbour.neighbourId)) nodeToDist.Add(neighbour.neighbourId, totalDistance); // Debugging --> can remove
                    selectedNode = neighbour.neighbourId;
                    br = true;
                    break;
                }

                // Check if neighbour has been examined
                if (!nodesExamined.Contains(neighbour.neighbourId))
                {
                    //UnityEngine.Debug.Log("neighbour.neighbourId " + neighbour.neighbourId + " --> " + totalDistance);
                    nodeToDist.Add(neighbour.neighbourId, totalDistance);
                    neighbours.Enqueue(neighbour.neighbourId);
                    nodesExamined.Add(neighbour.neighbourId);
                }
            }
        }

        //UnityEngine.Debug.Log("selectedNode: " + selectedNode);
        //UnityEngine.Debug.Log("selectedNode distance: " + nodeToDist[selectedNode]);

        return selectedNode;
    }

    /// <summary>
    /// Makes phone rise and display message with input cost and eta
    /// </summary>
    /// <param name="cost">Float representing the cost of the trip offered (in dollars)</param>
    /// <param name="eta">Float representing the additional time that will be taken for the trip (in seconds)</param>
    public void openPhone(float cost, float eta)
    {
        //Measure time taken to show phone and start animation rising
        timeElapsed = 0f;
        rising = true;
        transform.localPosition = new Vector3(0, phoneBottom, 0);
        gameObject.SetActive(true);

        costText.text = "Reduce Cost: -" + cost.ToString("c2");
        etaText.text = "Increased Travel Time: " + getTimeString(eta) + "mins";
    }

    /// <summary>
    /// Fast method to close phone without animation, used when round ends
    /// </summary>
    public void fastClosePhone()
    {
        gameObject.SetActive(false);
        rising = false;
        transform.localPosition = new Vector3(0, phoneBottom, 0);
    }

    /// <summary>
    /// Begins closing the phone (which is updated according to the Update function)
    /// </summary>
    private void closePhone()
    {
        rising = false;
    }

    /// <summary>
    /// Converts the numerical value of time in seconds to a formatted string to be displayed
    /// </summary>
    /// <param name="time">Float representing the time (in seconds)</param>
    /// <returns>String that correctly displays the time in the format: "m:ss"</returns>
    private string getTimeString(float time)
    {
        //Get minute and second values (rounded down)
        float minutesLeft = Mathf.Floor(time / 60);
        float secondsLeft = Mathf.Floor(time % 60);

        var minuteText = "";
        var secondsText = "";

        minuteText = (minutesLeft).ToString("0");
        secondsText = (secondsLeft).ToString("0");

        //Add '0' infront of single second digits
        if (secondsLeft < 10)
        {
            secondsText = "0" + secondsText;
        }

        string timeString = minuteText + ":" + secondsText;

        if (time <= 0)
        {
            timeString = "0:00";
        }

        return timeString;
    }

}
