using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using gta_background_traffic_v2;
using System.Linq;

/// <summary>
/// Class to hold information about each axle (front and back)
/// </summary>
[System.Serializable]
public class AxleInfo
{
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;
}

/// <summary>
/// Controller class to manage the behaviour of each car
/// </summary>
public class AVController : MonoBehaviour
{
    bool haveStarted = false;
    public int currentNode = 0; // The node that the car is currently on
    public List<Transform> nodes; // The path the car is currently on as a list of transforms
    public Transform path; // The transform of the path GameObject of the car which contains nodes that make up the path
   
    public Transform spawn = null; // Initial spawn location of the car
    public List<int> spawnNodes = new List<int>(); // Multiple potential spawn locations of the car (depricated)

    public Transform steeringWheel; // Position of the steering wheel
    public List<AxleInfo> axleInfos; // List containing front and back axels
    public float maxMotorTorque = 100f; // The maximum torque allowable while driving
    public float maxSteerAngle = 100f; // The maximum steer angle allowable while driving (Issues if this becomes too large due to physics)
    
    public int frameCount = 0; // Number of frames that have been counted
    public int consecutiveFramesWithNoSpeed = 0; // Number of frames where the speed has maintained 0

    float fps = 0;
    static int minConsecutiveFramesWithNoSpeed = 3600;
    static int minConsecutiveSecondsWithNoSpeed = 150; // Min number of frames where the speed has maintained 0
    static int maxConsecutiveSecondsWithNoSpeed = 210; // Max number of frames where the speed has maintained 0
    public float currentSpeed = 0f; // Current speed the car is going
    public float maxSpeed = 30f; // Maximum allowable speed for the car
    public float lastSpeed = 0f; // Last speed the car was going

    public bool isBraking = false; // Whether or not the car is breaking
    public Vector3 centerOfMass; // The center of mass of the car (acts as a central point)
    public GameTimer timer = null; // Tracks the time of the simulation
    public WorldMap world; // Representation of the world as an object
    public bool isSmart { get; set; } // Whether the car is smart or not
    public bool started; // Whether the round has started or not
    public float rotationRange = 60; // Maximum rotation of the steering wheel
    private Quaternion defaultSteeringrightWheelotation;
    private float pastCarAngle;

    private float drivingOffset = 0f; // The current offset the car is using
    private readonly float leftSideOffset = 5f; // Used for 2 way streets to ensure that cars do not collide going the opposite ways
    private List<bool> oneWays; // A list of all oneway streets since they add some extra confusion

    private float startTime; // The start time of the car
    private bool haveReachedDestination = false; // Whether the car has reached its destination or not

    private TrafficLightController trafficLightController; // Controls the traffic lights and used to check whether a car should stop before lights
    public List<string> pathList; // A list of all path node ids

    private Graph g; // The world as a logical graph
    private SessionManager sm; // The session manager

    private RaycastHit rayHit; // Used for detecting if another car is too close
    private bool hitDetected = false; // True when another car is too close
    private float castDistance = 20f; // The proximity distance of the raycast

    public bool roundOver = true; // Indicates whether the round is over or if cars should keep driving

    private bool isAtTrafficLight = false;

    /// <summary>
    /// Indicates valid directions for the car to turn
    /// </summary>
    public enum turningDirection 
    {
        left,
        right,
        forward
    }

    public enum bearing
    {
        north = 1,
        east = 2,
        south = 3,
        west = 4
    }

    public turningDirection turning; // The direction the car is currently going

    public bearing currentBearing; // The bearing the car is currently on

    public bool isAtIntersection = false; // True when the car is within a certain distance of an intersection
    public bool stopped = false; // True when the car has stopped to wait for another car or for traffic lights

    public float stopTime = 0f;
    private bool braking = false; // Whether the car is trying to slow down

    public int roadPriority;
    
    /// <summary>
    /// Sets up variables before scene is loaded
    /// </summary>
    void Awake()
    {
        GameObject go = GameObject.Find("CitySimulatorMap");
        world = (WorldMap)go.GetComponent<WorldMap>();

        go = GameObject.Find("TrafficLights");
        trafficLightController = (TrafficLightController)go.GetComponent<TrafficLightController>();

        sm = GameObject.Find("SessionManager").GetComponent<SessionManager>();

        g = GameObject.Find("Graph").GetComponent<GraphController>().graph; 

        pathList = new List<string>();
        oneWays = new List<bool>();

    }

    /// <summary>
    /// Set dynamic variables and the nodes in the path
    /// </summary>
    void Start()
    {
        haveStarted = true;

        startTime = Time.time;

        GetComponent<Rigidbody>().centerOfMass = centerOfMass;
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        defaultSteeringrightWheelotation = steeringWheel.localRotation;

        // Filter pathTransforms to have only the childs
        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != path.transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }
        TP2Start();
    }

    /// <summary>
    /// TelePort To start sends the car back to its initial position and resets its path
    /// </summary>
    public void TP2Start()
    {
        if (nodes.Count > 1)
        {
            gameObject.transform.position = nodes[0].position;
            gameObject.transform.LookAt(nodes[1]);
            gameObject.transform.position = transform.TransformPoint(new Vector3(-drivingOffset, 0, 0));
            this.started = true;
        }
        currentNode = 0;
    }
    
    /// <summary>
    /// Resets path variables when a change in the underlying gameobjects has occurred
    /// If it is a new round then also teleports the car back to the start
    /// </summary>
    /// <param name="newRound">Indicates whether there is a new round</param>
    public void resetPaths(bool newRound)
    {
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        // Filter pathTransforms to have only the childs
        //UnityEngine.Debug.Log("pathTransforms: " + pathTransforms.Length);
        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != path.transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }
        if (newRound) TP2Start();
    }

    /// <summary>
    /// Updates the car's goal if it gets too close to its next node
    /// </summary>
    private void CheckWaypointDistance()
    {
        //Update the current node the car is going to if too close to current
        if (Vector3.Distance(transform.position, nodes[currentNode].position) < 4f + drivingOffset)
        {
            if (currentNode == nodes.Count - 1)
            {
                g.RemoveCongestion(pathList[currentNode-1], pathList[currentNode]);
                currentNode = 0;
                haveReachedDestination = true;
            }
            else
            {
                g.RemoveCongestion(pathList[currentNode], pathList[currentNode+1]);
                currentNode++;
                if (currentNode!=(nodes.Count-1)) g.AddCongestion(pathList[currentNode], pathList[currentNode+1]);
            }
            
            if (!haveReachedDestination && !onOneWay()) 
            {
                drivingOffset = leftSideOffset;
            }
            else 
            {
                drivingOffset = 0;
            }

            setNextTurn();
            getBearingAndPriority();
        }
    }

    /// <summary>
    /// Checks whether the current node is oneway
    /// </summary>
    /// <returns>If the current node is oneway</returns>
    private bool onOneWay()
    {
        try 
        {
            return oneWays[currentNode];
        } catch (ArgumentOutOfRangeException e)
        {
            print(e.Message);
            print("max size:" + oneWays.Count + " but got:" + currentNode);
            return false;
        }

    }

    /// <summary>
    /// Create list of one way roads, with index matching up with currentNode travelling towards
    /// </summary>
    /// <param name="pathNodes">List of all nodes on the car's path</param>
    /// <returns>Whether the next road is oneway</returns>
    public Vector3 setOneWayRoads(List<string> pathNodes) 
    {
        oneWays = new List<bool>();
        oneWays.Add(false); // travelling to node 0, shouldnt occur  
        for (int i = 1; i < pathNodes.Count; i++) 
        {

            String currNode = pathNodes[i];
            String prevNode = pathNodes[i-1];
            Graph.Node n = g.nodeIdNodeDict[prevNode];
            List<Graph.Node.Neighbour> neighbours = n.neighbours;

            // check if the path from prevNode to currNode is one way
            foreach (Graph.Node.Neighbour neighbour in neighbours) 
            {
                if (neighbour.neighbourId == currNode)
                {
                    oneWays.Add(neighbour.isOneWay);
                    break;
                }
            }
        }

        if (oneWays[1]) 
        {
            return new Vector3(0,0,0);
        }
        else 
        {
            drivingOffset = leftSideOffset;
            return new Vector3(-drivingOffset, 0, 0);
        }
    }

    /// <summary>
    /// Sets the pathlist
    /// </summary>
    /// <param name="path">New pathlist to set</param>
    public void setPath(List<string> path)
    {
        this.pathList = new List<string>(path);
    }

    /// <summary>
    /// Called when front collider hits another object to detect what should happen
    /// </summary>
    /// <param name="other">Collider of the other object</param>
    private void OnTriggerEnter(Collider other)
    {
        // stop if other object is car/player
        if (!other.isTrigger && (other.tag == "Car" || other.tag == "DriverCar"))
        {
            if (!braking)
            {
                UnityEngine.Debug.Log("Collision with another car");
            }

            GetComponent<Rigidbody>().velocity = new Vector3(0,0,0);
            foreach (AxleInfo axleInfo in axleInfos)
            {
                axleInfo.leftWheel.brakeTorque = maxMotorTorque;
                axleInfo.rightWheel.brakeTorque = maxMotorTorque;
            }
            braking = true;
        }
    }

    /// <summary>
    /// Called when front collider is no longer colliding
    /// </summary>
    /// <param name="other">Collider of the other object</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Car" || other.tag == "DriverCar") 
        {
            if (braking)
            {
                UnityEngine.Debug.Log("No longer braking");
            }

            // stop braking so car can move
            foreach (AxleInfo axleInfo in axleInfos)
            {
                axleInfo.leftWheel.brakeTorque = 0;
                axleInfo.rightWheel.brakeTorque = 0;
            }
        }
        braking = false;
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        if (haveStarted)
        {
            //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }

    /// <summary>
    /// On every fixed frame update, will check how the car should react to its conditions
    /// </summary>
    private void FixedUpdate()
    {
        frameCount++;

        /*if (gameObject.transform.parent.gameObject.name == "AICar(Clone)")
        {
            var player = GameObject.Find("Player");
            if (player)
            {
                UnityEngine.Debug.Log("Player object found");
                //float distance = Vector3.Distance(player.transform.position, gameObject.transform.position);
                //UnityEngine.Debug.Log("Distance from player: " + distance);

            }
        }*/

        // Sleep if the round is over
        if (roundOver)
        {
            foreach (AxleInfo axleInfo in axleInfos)
            {
                axleInfo.leftWheel.motorTorque = 0;
                axleInfo.rightWheel.motorTorque = 0;
            }
            GetComponent<Rigidbody>().Sleep();
            return;
        }

        // Remove AICar if at end of path
        if (gameObject.transform.parent.gameObject.name == "AICar(Clone)" && haveReachedDestination)
        {
            UnityEngine.Debug.Log("Destroyed Car");
            GetComponent<Rigidbody>().Sleep();
            Destroy(gameObject.transform.parent.gameObject);
            return;
        }

        //Stop movement if game timer is paused
        // OR if reached the end of nodes
        if (currentNode == -1 || (timer != null && timer.isPaused()) || haveReachedDestination)
        {
            if (haveReachedDestination && !roundOver)
            {
                sm.driverReachedDest = true;
                haveReachedDestination = false;
            }
            foreach (AxleInfo axleInfo in axleInfos)
            {
                axleInfo.leftWheel.motorTorque = 0;
                axleInfo.rightWheel.motorTorque = 0;
            }
            GetComponent<Rigidbody>().Sleep();
            return;
        }

        var distanceToNode = Vector3.Distance(transform.position, nodes[currentNode].position);

        // If close to node
        if (distanceToNode < 20f && distanceToNode > 5f)
        {
            // Check if traffic light
            if (pathList.Count > 1 && currentNode > 0 && trafficLightController.trafficCheck(pathList[currentNode - 1], pathList[currentNode]))
            {
                if (!isAtTrafficLight)
                {
                    isAtTrafficLight = true;
                    UnityEngine.Debug.Log("Now at Traffic Light");
                }

                foreach (AxleInfo axleInfo in axleInfos)
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.rightWheel.motorTorque = 0;
                }
                GetComponent<Rigidbody>().Sleep();
                return;
            }
            else
            {
                if (isAtTrafficLight)
                {
                    UnityEngine.Debug.Log("Now no longer at Traffic Light");
                }

                isAtTrafficLight = false;
            }

            //check for intersection and then raycast if needed 
            if (pathList.Count > 1 && currentNode > 0 && !g.trafficLights.Contains(pathList[currentNode]) && isIntersection(pathList[currentNode]) && !isAtIntersection)
            {
                Vector3 centre = nodes[currentNode].position;

                bool clear = true;
                LayerMask layer = LayerMask.GetMask("Cars");

                //check intersection is clear
                Collider[] hits = Physics.OverlapBox(centre, transform.localScale * 50, Quaternion.identity, layer, QueryTriggerInteraction.Ignore);
                UnityEngine.Debug.Log(hits.Length + " cars detected in intersection");
                foreach (Collider collider in hits)
                {
                    if (collider == GetComponent<Collider>()) continue;

                    var other = collider.gameObject.GetComponent<AVController>();
                    if (!other.isAtIntersection || !other.isAtIntersection) continue;

                    Vector3 direction = collider.transform.forward;
                    float dot = Vector3.Dot(direction, transform.forward);
                    if (dot > 0.5) // other car is travelling in same direction
                    {
                        continue;
                    }
                    else if (true)
                    {
                        clear = false;
                    }
                }
                
                if (turning == turningDirection.left)
                {
                    // check to the right
                    hits = Physics.OverlapBox(centre + transform.right * castDistance, new Vector3(2,1,1) * castDistance, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected to the right when turning left.");
                    foreach (Collider collider in hits)
                    {
                        if (collider == this.GetComponent<Collider>()) continue;
                        AVController other = collider.gameObject.GetComponent<AVController>();

                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, transform.right);
                        if (dot < 0) // other car is travelling left, i.e. towards this car
                        {
                            if (other.isAtIntersection || other.isAtTrafficLight) continue;

                            // ignore cars that don't impede movement
                            if (((int)currentBearing + 1) % 4 == (int)other.currentBearing)
                            {
                                UnityEngine.Debug.Log("Turning Left: Ignoring car coming from clockwise direction");
                                continue;
                            }
                            else if ((((int)currentBearing + 4) - 1) % 4 == (int)other.currentBearing && (other.turning == turningDirection.left || other.turning == turningDirection.right))
                            {
                                UnityEngine.Debug.Log("Turning Left: Ignoring car coming from anti-clokwise direction turning left or right");
                                continue;
                            }
                            else if (Math.Abs(currentBearing - other.currentBearing) == 2 && (other.turning == turningDirection.left || other.turning == turningDirection.forward))
                            {
                                UnityEngine.Debug.Log("Turning Left: Ignoring car coming in opposite direction going forward or turning left");
                                continue;
                            }

                            // ignore cars which give way to you
                            if (other.braking) continue;

                            // ignore cars with lower priority
                            if (other.roadPriority < roadPriority) continue;

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    }                
                }
                else if (turning == turningDirection.forward)
                {
                    // check to the right
                    hits = Physics.OverlapBox(centre + transform.right * castDistance, new Vector3(2,1,1) * castDistance, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected to the right when going forward.");
                    foreach (Collider collider in hits)
                    {
                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, transform.right);
                        if (dot < 0)
                        {
                            if (collider == this.GetComponent<Collider>()) continue;
                            AVController other = collider.gameObject.GetComponent<AVController>();
                            if (other.isAtIntersection || other.isAtTrafficLight) continue;

                            if (other.braking) continue;

                        


                            // ignore cars with lower priority
                            if (other.roadPriority < roadPriority) continue;

                            

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    }

                    // check to the left
                    hits = Physics.OverlapBox(centre - transform.right * 20, new Vector3(2,1,1) * 20, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected to the left when going forward.");
                    foreach (Collider collider in hits)
                    {
                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, -transform.right);
                        if (dot > 0)
                        {
                            if (collider == this.GetComponent<Collider>()) continue;
                            AVController other = collider.gameObject.GetComponent<AVController>();
                            if (other.isAtIntersection || other.isAtTrafficLight) continue;

                            if (other.braking) continue;
                            if (other.roadPriority < roadPriority) continue;

                            // ignore cars that don't impede movement
                            //if (other.roadPriority == roadPriority && (other.turning == AVController.direction.forward || other.turning == AVController.direction.left)) continue;

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    }

                    // check forward
                    /*hits = Physics.OverlapBox(centre + transform.forward * 5, new Vector3(1, 1, 2) * 5, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    foreach (Collider collider in hits)
                    {
                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, transform.forward);
                        if (dot < 0)
                        {
                            if (collider == this.GetComponent<Collider>()) continue;
                            AVController other = collider.gameObject.GetComponent<AVController>();
                            //if (other.inIntersection) continue;

                            //if (other.braking) continue;

                            if (currentBearing == other.currentBearing)
                            {
                                UnityEngine.Debug.Log("Going forward: Car in front going in the same direction");

                                foreach (AxleInfo axleInfo in axleInfos)
                                {
                                    axleInfo.leftWheel.brakeTorque = maxMotorTorque;
                                    axleInfo.rightWheel.brakeTorque = maxMotorTorque;
                                }
                                braking = true;
                                GetComponent<Rigidbody>().Sleep();
                                return;
                            }
                            else
                            {
                                UnityEngine.Debug.Log("Going forward: Hitting a car so not clear");
                                clear = false;
                            }


                        }
                    }*/
                }
                else // turning right
                {
                    // check to the right
                    hits = Physics.OverlapBox(centre + transform.right * 20, new Vector3(2,1,1) * 20, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected to the right when turning right.");
                    foreach (Collider collider in hits)
                    {
                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, transform.right);
                        if (dot < 0)
                        {
                            if (collider == this.GetComponent<Collider>()) continue;
                            AVController other = collider.gameObject.GetComponent<AVController>();
                            if (other.isAtIntersection || other.isAtTrafficLight) continue;
                          
                            if (other.braking) continue;
                            if (other.roadPriority < roadPriority) continue;

                            // ignore cars that don't impede movement
                            //if (other.roadPriority == roadPriority && other.turning == AVController.direction.right) continue;

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    } 

                    // check to the left
                    hits = Physics.OverlapBox(centre - transform.right * 20, new Vector3(2,1,1) * 20, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected to the left when turning right.");
                    foreach (Collider collider in hits)
                    {
                        if (collider == this.GetComponent<Collider>()) continue;
                        AVController other = collider.gameObject.GetComponent<AVController>();

                        if (Math.Abs(currentBearing - other.currentBearing) == 2 && other.turning == AVController.turningDirection.right)
                        {
                            UnityEngine.Debug.Log("Turning Right: Ignoring car coming in opposite direction turning right");
                            continue;
                        }

                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, -transform.right);
                        if (dot > 0)
                        {
                            if (other.isAtIntersection || other.isAtTrafficLight) continue;
                           
                            if (other.braking) continue;


                            

                            if (other.roadPriority < roadPriority) continue;

                            // ignore cars that don't impede movement
                            //if (other.roadPriority == roadPriority && other.turning == AVController.direction.right) continue;

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    }  

                    // check forward
                    hits = Physics.OverlapBox(centre + transform.forward * 20, new Vector3(1,1,2) * 20, transform.rotation, layer, QueryTriggerInteraction.Ignore);
                    UnityEngine.Debug.Log(hits.Length + " cars detected forward when turning right.");
                    foreach (Collider collider in hits)
                    {
                        if (collider == this.GetComponent<Collider>()) continue;
                        AVController other = collider.gameObject.GetComponent<AVController>();

                        if (Math.Abs(currentBearing - other.currentBearing) == 2 && other.turning == AVController.turningDirection.right)
                        {
                            UnityEngine.Debug.Log("Turning Right: Ignoring car coming in opposite direction turning right");
                            continue;
                        }

                        Vector3 direction = collider.transform.forward;
                        float dot = Vector3.Dot(direction, transform.forward);
                        if (dot < 0)
                        {
                            if (other.isAtIntersection) continue;
                            
                            if (other.braking) continue;


                            if (other.roadPriority < roadPriority) continue;

                            // ignore cars that don't impede movement
                            //if (other.roadPriority == roadPriority && other.turning == AVController.direction.right) continue;

                            if (other.stopTime < 1 || other.stopTime > stopTime)
                            {
                                clear = false;
                            }
                        }                        
                    }
                }

                if (clear)
                {
                    isAtIntersection = true;
                    stopTime = UnityEngine.Random.Range(0f, 1f);
                    StartCoroutine("Intersection");
                }
                else
                {
                    stopTime += 1;
                    if (!stopped)
                    {
                        StartCoroutine("Stop");
                    }
                }
            }
        }

        //Update wheel positions based on speed and angle
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position) + new Vector3(-drivingOffset, 0, 0);
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            currentSpeed = 2 * Mathf.PI * axleInfo.leftWheel.radius * axleInfo.leftWheel.rpm * 60 / 1000;

            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = newSteer;
                axleInfo.rightWheel.steerAngle = newSteer;
            }

            if (axleInfo.motor)
            {
                if (currentSpeed < maxSpeed)
                {
                    axleInfo.leftWheel.motorTorque = maxMotorTorque;
                    axleInfo.rightWheel.motorTorque = maxMotorTorque;
                }
                else
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.rightWheel.motorTorque = 0;
                }
            }

            lastSpeed = currentSpeed;
            
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        if (currentSpeed == 0 && !isAtTrafficLight)
        {
            consecutiveFramesWithNoSpeed++;
            if (consecutiveFramesWithNoSpeed > minConsecutiveFramesWithNoSpeed)
            {
                var newFps = 1 / Time.unscaledDeltaTime;
                if (newFps >= 25)
                {
                    fps = newFps;
                    UnityEngine.Debug.Log("FPS:" + fps);
                }

                var consecutiveSecondsWithNoSpeed = consecutiveFramesWithNoSpeed / fps;
                UnityEngine.Debug.Log("Consecutive seconds with no speed:" + consecutiveSecondsWithNoSpeed);
                if (consecutiveSecondsWithNoSpeed >= minConsecutiveSecondsWithNoSpeed)
                {
                    var randomNumber = UnityEngine.Random.Range(minConsecutiveSecondsWithNoSpeed, maxConsecutiveSecondsWithNoSpeed);
                    if (randomNumber < consecutiveFramesWithNoSpeed)
                    {
                        if (nodes.Count > 1)
                        {
                            currentNode--;

                            gameObject.transform.position = nodes[currentNode].position;
                            gameObject.transform.LookAt(nodes[currentNode + 1]);
                            gameObject.transform.position = transform.TransformPoint(new Vector3(-drivingOffset, 0, 0));
                            UnityEngine.Debug.Log("Have been stationary for awhile, moving back to previous node");
                        }

                        consecutiveFramesWithNoSpeed = 0;
                    }
                }
            }
        }
        else
        {
            consecutiveFramesWithNoSpeed = 0;
        }

        CheckWaypointDistance();
        UpdateSteeringWheel();
    }

    /// <summary>
    /// Gets the priority of the next segment of road where vehicles on lower priority roads will need to give way to vehicles on higher priority roads
    /// 
    /// lowest prioirty 
    /// 1 - one way E-W
    /// 2 - one way N-S
    /// 3 - two way E-W
    /// 4 - two way N-S
    /// </summary>
    /// <returns>The priority of the next segment of road to travel</returns>
    private void getBearingAndPriority()
    {
        if (currentNode >= pathList.Count - 2 || currentNode == 0)
        {
            roadPriority = 5;
            return;
        }

        Graph.Node currNode = g.nodeIdNodeDict[pathList[currentNode]];
        Graph.Node prevNode = g.nodeIdNodeDict[pathList[currentNode -1]];

        double startLat = prevNode.lat;
        double startLon = prevNode.lon;
        double endLat = currNode.lat;
        double endLon = currNode.lon;

        startLat = startLat * Math.PI/180;
        startLon = startLon * Math.PI/180;
        endLat = endLat * Math.PI/180;
        endLon = endLon * Math.PI/180;

        double x = Math.Sin(endLon-startLon) * Math.Cos(endLat);
        double y = Math.Cos(startLat)*Math.Sin(endLat) - Math.Sin(startLat)*Math.Cos(endLat)*Math.Cos(endLat-startLat);
        double theta = Math.Atan2(y,x);
        double brng = (theta*180/Math.PI + 360) % 360;
        if (brng == 0) 
        {
            brng = 360;
        }

        if (brng >= 315 || brng < 45)
        {
            currentBearing = bearing.north;
        }
        else if (brng >= 45 && brng < 135)
        {
            currentBearing = bearing.east;
        }
        else if (brng >= 135 && brng < 225)
        {
            currentBearing = bearing.south;
        }
        else
        {
            currentBearing = bearing.west;
        }

        bool isOneWay = onOneWay();
        if (currentBearing == bearing.east || currentBearing == bearing.west)
        {
            roadPriority = isOneWay ? 1 : 3;
        }
        else
        {
            roadPriority = isOneWay ? 2 : 5;
        }
    }

    /// <summary>
    /// Determines if a node is an intersection 
    /// currently defining intersection as node with multiple neighbours 
    /// @TODO merging when turning left onto one way main road or roundabout
    /// </summary>
    /// <param name="nodeID">The Node Id of the node to check</param>
    /// <returns>True if the node is an intersection</returns>
    private bool isIntersection(string nodeID)
    {
        Graph.Node node = g.nodeIdNodeDict[nodeID];
        return node.neighbours.Count > 2 || (node.neighbours.Count == 2 && node.neighbours[0].isOneWay);
    }

    /// <summary>
    /// calculate which way the next turn is 
    /// </summary>
    private void setNextTurn()
    {
        if (currentNode >= nodes.Count - 2 || currentNode == 0)
        {
            return;
        }

        Transform prevNode = nodes[currentNode-1];
        Transform currNode = nodes[currentNode];
        Transform nextNode = nodes[currentNode+1];

        Vector3 forwardDir = currNode.position - prevNode.position;
        forwardDir.Normalize();

        Vector3 nextDir = nextNode.position - currNode.position;
        nextDir.Normalize();

        Vector3 leftDir = Vector3.Cross(forwardDir, transform.up).normalized;

        float dot = Vector3.Dot(leftDir, nextDir);
        if (dot < -0.5)
        {
            turning = turningDirection.right;
        }
        else if (dot > 0.5)
        {
            turning = turningDirection.left;
        }
        else 
        {
            turning = turningDirection.forward;
        }
    }

    /// <summary>
    /// reset inIntersection after a few seconds have passed @todo: There should be a better way to reset this
    /// </summary>
    /// <returns></returns>
    IEnumerator Intersection()
    {
        yield return new WaitForSeconds(6);
        isAtIntersection = false;
    }

    /// <summary>
    /// Stops vehicles
    /// </summary>
    /// <returns></returns>
    IEnumerator Stop()
    {
        GetComponent<Rigidbody>().velocity = new Vector3(0,0,0);
        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.leftWheel.brakeTorque = maxMotorTorque;
            axleInfo.rightWheel.brakeTorque = maxMotorTorque;
        }
        stopped = true;
        yield return new WaitForSeconds(1);
        stopped = false;
        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.leftWheel.brakeTorque = 0;
            axleInfo.rightWheel.brakeTorque = 0;
        }
    }

    /// <summary>
    /// Finds the corresponding visual wheel and applies the transform
    /// </summary>
    /// <param name="collider">WheelCollider of the steering wheel</param>
    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) 
        {
            return;
        }
     
        Transform visualWheel = collider.transform.GetChild(0);
     
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
     
        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    /// <summary>
    /// Updates the car's steering wheel
    /// </summary>
    private void UpdateSteeringWheel()
    {
        //Only update player's wheel
        if (spawn != null)
        {
            return;
        }

        float carAngle = transform.rotation.eulerAngles.y;
        Vector3 currentRotation = steeringWheel.localRotation.eulerAngles;

        if (carAngle + 0.3 < pastCarAngle)
        {
            //Car turning left, update wheel left
            steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, 60.0f * Time.deltaTime * 4f);
        } 
        else if (carAngle - 0.3 > pastCarAngle)
        {
            //Car turning right, update wheel right
            steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, -60.0f * Time.deltaTime * 4f);
        } 
        else 
        {
            //Car going straight rotate wheel back to centre
            if (currentRotation.z < 1 || currentRotation.z > 359)
            {
                //Do nothing wheel is straight
            }
            else if (currentRotation.z < 180)
            {
                //Wheel is left, update wheel to turn right to center
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, -60.0f * Time.deltaTime * 0.5f);
            }
            else 
            {
                //Wheel is right, update wheel to turn left to center
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, 60.0f * Time.deltaTime * 0.5f);
            }
        }

        //Clamp rotation of steering wheel between range
        currentRotation = steeringWheel.localRotation.eulerAngles;
        if (currentRotation.z < 180)
        {
            currentRotation.z = Mathf.Clamp(currentRotation.z, 0, rotationRange);
        }
        else
        {
            currentRotation.z = Mathf.Clamp(currentRotation.z, 360 - rotationRange, 360);
        }

        //Update steering wheel positiong and car current angle
        steeringWheel.localRotation = Quaternion.Euler(currentRotation);
        pastCarAngle = carAngle;
    }
}
