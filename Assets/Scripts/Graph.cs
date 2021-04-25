using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace gta_background_traffic_v2
{
    /// <summary>
    ///     A class that allows us to construct a graph of intersections
    ///     in the map that are defined by the <see cref="Node"/> class.
    /// </summary>
    public class Graph
    {
        // GRAPH PROPERTIES
        public double minLat { get; set; } // Minimum latitude of any node in the graph
        public double minLon { get; set; } // Minimum longitude of any node in the graph
        private double maxLat { get; set; } // Maximum latitude of any node in the graph
        private double maxLon { get; set; } // Maximum longitude of any node in the graph
        public double latRange { get; set; } // Difference between the minimum and maximum latitude
        public double lonRange { get; set; } // Difference between the minimum and maximum longitude
        public List<Way> ways { get; set; } // List of all ways (full streets/roads)
        public List<Node> nodes { get; set; } // List of all nodes (typically intersections or unusual geometry)
        public Dictionary<string, Node> nodeIdNodeDict; // Dictionary that maps node IDs to node objects
        public HashSet<string>[] trafficLightDirections { get; set; } // Array of HashSets that holds all node objects that represent traffic lights for each direction
        public HashSet<string> trafficLights { get; set; } // Hashset that holds all node objects that represent traffic lights
        public Dictionary<(string, string), int> carsOnRoute; // Tracks how many cars are on any edge

        /// <summary>
        ///     Graph() class constructor.
        /// </summary>
        public Graph()
        {
            ways = new List<Way>();
            nodes = new List<Node>();
            nodeIdNodeDict = new Dictionary<string, Node>();
            trafficLightDirections = new HashSet<string>[4];
            for(int i = 0; i < 4; i++)
            {
                trafficLightDirections[i] = new HashSet<string>();
            }
            trafficLights = new HashSet<string>();
            carsOnRoute = new Dictionary<(string, string), int>();
        }

        /// <summary>
        ///     Manually add a node (<see cref="Node"/>) to the graph.
        /// </summary>
        /// <param name="n"> A node to add to the graph. </param>
        public void AddNode(Node n)
        {
            nodes.Add(n);
            nodeIdNodeDict[n.nodeId] = n;
        }

        /// <summary>
        ///     Read in a .Json file that contains the nodes and ways, 
        ///     outputted from the map tool.
        /// </summary>
        /// <param name="fileName"> A .JSON file that contains road data. </param>
        public void ReadInGraphFromJSON(string fileName)
        {
            using (StreamReader r = new StreamReader(fileName))
            {
                string json = r.ReadToEnd();
                dynamic arr = JsonConvert.DeserializeObject(json);



                //var arr = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);



                // Set the first node lat and lon to the graph's global lat and lon
                // so we can check the boundaries of the graph
                minLat = (double)arr["nodes"][0]["lat"];
                minLon = (double)arr["nodes"][0]["lon"];
                maxLat = (double)arr["nodes"][0]["lat"];
                maxLon = (double)arr["nodes"][0]["lon"];
                

                // Loop through all ways in file
                foreach (var way in arr["ways"])
                {
                    string wayId = (string)way["wayId"];
                    string name = (string)way["name"];
                    string highwayType = (string)way["highwayType"];
                    int maxSpeed = Convert.ToInt32((int)way["maxSpeed"]);
                    ways.Add(new Way(wayId, name, highwayType, maxSpeed));
                }

                // Loop through all nodes in file
                foreach (var node in arr["nodes"])
                {
                    string nodeId = (string)node["nodeId"];
                    double lat = Convert.ToDouble((double)node["lat"]);
                    double lon = Convert.ToDouble((double)node["lon"]);
                    string wayId = (string)node["wayId"];
                    int isSignal = (int)node["isSignal"];
                    checkLatLon(lat, lon);
                    Node n = new Node(nodeId, lat, lon, wayId, isSignal);

                    if (isSignal != 0)
                    {
                        trafficLights.Add(nodeId);
                        if (isSignal >= 315 || isSignal < 45)
                        {
                            // North
                            trafficLightDirections[0].Add(nodeId);
                        }
                        else if (isSignal >= 45 && isSignal < 135)
                        {
                            // East
                            trafficLightDirections[1].Add(nodeId);
                        }
                        else if (isSignal >= 135 && isSignal < 225)
                        {
                            // South
                            trafficLightDirections[2].Add(nodeId);
                        }
                        else
                        {
                            // West
                            trafficLightDirections[3].Add(nodeId);
                        }
                    }

                    // Loop through all neighbours in node
                    foreach (var neighbour in node["neighbours"])
                    {
                        string neighbourId = (string)neighbour["neighbourId"];
                        //double neighbourLat = Convert.ToDouble((double)neighbour["lat"]);
                        //double neighbourLon = Convert.ToDouble((double)neighbour["lon"]);
                        double distance = Convert.ToDouble((double)neighbour["distance"]);
                        //double time = Convert.ToDouble((double)neighbour["time"]);
                        bool isOneWay = Convert.ToBoolean((string)neighbour["isOneWay"]);
                        string neighbourWayId = (string)neighbour["wayId"];
                        //n.AddNeighbour(neighbourId, neighbourLat, neighbourLon, distance, time, isOneWay, neighbourWayId);
                        n.AddNeighbour(neighbourId, distance, isOneWay, neighbourWayId);
                    }
                    nodes.Add(n);
                    nodeIdNodeDict[n.nodeId] = n;
                }
            }
            // Calculate the latRange and the lonRange for the graph
            // So we can convert map coordinates to the graph coordinates
            latRange = (double)(maxLat - minLat);
            lonRange = (double)(maxLon - minLon);
        }

        /// <summary>
        ///     Checks a lat and lon coordinate of a new node to store the
        ///     bounds of the section of map which is used to convert from
        ///     lat and lon coords to x and y coords.
        /// </summary>
        /// <param name="lat"> A latitude value, type double. </param>
        /// <param name="lon"> A longitude value, type double. </param>
        private void checkLatLon(double lat, double lon)
        {
            minLat = (lat < minLat) ? lat : minLat;
            maxLat = (lat > maxLat) ? lat : maxLat;
            minLon = (lon < minLon) ? lon : minLon;
            maxLon = (lon > maxLon) ? lon : maxLon;
        }

        /// <summary>
        ///     A class that defines a node (a.k.a intersection) in the map.
        ///     Each node has a map coordinate represented as latitude and longitude,
        ///     and also a list of its corresponding neighbours <see cref="Neighbour"/>.
        /// </summary>
        public class Node
        {
            // NODE PROPERTIES
            public string nodeId { get; set; }
            public double lat { get; set; }
            public double lon { get; set; }
            public string wayId { get; set; }
            public List<Neighbour> neighbours { get; set; }
            public int isSignal { get; set; }

            /// <summary>
            ///     Node() class constructor.
            /// </summary>
            /// <param name="nodeId"> The nodes ID value, type string. </param>
            /// <param name="lat"> The nodes latitude coordinate value, type double.</param>
            /// <param name="lon"> The nodes longitude coordinate value, type double. </param>
            /// <param name="wayId"> The way identifier that the node is associated with. </param>
            public Node(string nodeId, double lat, double lon, string wayId, int isSignal)
            {
                this.nodeId = nodeId;
                this.lat = lat;
                this.lon = lon;
                this.wayId = wayId;
                this.isSignal = isSignal;
                neighbours = new List<Neighbour>();
            }

            /// <summary>
            ///     Adds a single neighbour to a node in the graph, by node id.
            /// </summary>
            /// <param name="neighbourId"> The nodes ID value, type string. </param>
            /// <param name="lat"> The nodes latitude coordinate value, type double.</param>
            /// <param name="lon"> The nodes longitude coordinate value, type double. </param>
            /// <param name="distance"> The nodes distance from its parent node, type double. </param>
            /// <param name="time"> The nodes time value to go from its parent to itself in seconds, type double.</param>
            /// <param name="isOneWay"> Denotes whether or not the parent node to itself is a one way road, type boolean. </param>
            /// <param name="wayId"> The way ID that the parent node to itself belongs to, type string. </param>
            // public void AddNeighbour(string neighbourId, double lat, double lon, double distance, double time, bool isOneWay, string wayId)
            // {
            //     neighbours.Add(new Neighbour(neighbourId, lat, lon, distance, time, isOneWay, wayId));
            // }
            public void AddNeighbour(string neighbourId, double distance, bool isOneWay, string wayId)
            {
                neighbours.Add(new Neighbour(neighbourId, distance, isOneWay, wayId));
            }

            /// <summary>
            ///     The Neighbour class defines its own coordinates and the
            ///     the associated costs to traverse the path, from the original node.
            /// </summary>
            public class Neighbour
            {
                // NEIGHBOUR PROPERTIES
                public string neighbourId;
                //public double lat;
                //public double lon;
                public double distance;
                //public double time;
                public bool isOneWay;
                public string wayId;

                /// <summary>
                ///     Neighbour() class constructor.
                /// </summary>
                /// <param name="neighbourId"> The nodes ID value, type string. </param>
                /// <param name="lat"> The nodes latitude coordinate value, type double.</param>
                /// <param name="lon"> The nodes longitude coordinate value, type double. </param>
                /// <param name="distance"> The nodes distance from its parent node, type double. </param>
                /// <param name="time"> The nodes time value to go from its parent to itself in seconds, type double.</param>
                /// <param name="isOneWay"> Denotes whether or not the parent node to itself is a one way road, type boolean. </param>
                /// <param name="wayId"> The way ID that the parent node to itself belongs to, type string. </param>
                // public Neighbour(string neighbourId, double lat, double lon, double distance, double time, bool isOneWay, string wayId)
                // {
                //     this.neighbourId = neighbourId;
                //     this.lat = lat;
                //     this.lon = lon;
                //     this.distance = distance;
                //     this.time = time;
                //     this.isOneWay = isOneWay;
                //     this.wayId = wayId;
                // }
                public Neighbour(string neighbourId, double distance, bool isOneWay, string wayId)
                {
                    this.neighbourId = neighbourId;
                    this.distance = distance;
                    this.isOneWay = isOneWay;
                    this.wayId = wayId;
                }
            }
        }

        /// <summary>
        ///     A way is essentially a set of nodes <see cref="Node"/>,
        ///     that make a path. Ways make up each individual 'sub-section' 
        ///     of road in the entire map.
        /// </summary>
        public class Way
        {
            // WAY PROPERTIES
            public string wayId { get; set; }
            public string name { get; set; }
            public string highwayType { get; set; }
            public int maxSpeed { get; set; }

            /// <summary>
            ///     Way() class constructor.
            /// </summary>
            /// <param name="wayId"> The way ID that the parent node to itself belongs to, type string. </param>
            /// <param name="name"> The way's real-world road name, type string. </param>
            /// <param name="highwayType"> The way's road type, type string.</param>
            /// <param name="maxSpeed"> The way's max speed allowed, type int. </param>
            public Way(string wayId, string name, string highwayType, int maxSpeed)
            {
                this.wayId = wayId;
                this.name = name;
                this.highwayType = highwayType;
                this.maxSpeed = maxSpeed;
            }
        }

        // An extension that will not allow the path to consider a node given
        public List<string> PathToFromExclude(string startID, string endID, string excludeID)
        {
            if (!nodeIdNodeDict.ContainsKey(startID) || !nodeIdNodeDict.ContainsKey(endID))
            {
                throw new ArgumentException("StartID and EndID must be valid node IDs for this graph");
            }

            List<string> nodeIDs = new List<string>();

            //List of current shortest known travel times to each node from the source
            Dictionary<string, double> timeTo = new Dictionary<string, double>();

            //Tracks which nodes still need to be visited, nodes are removed as they are visited
            List<string> nodesToVisit = new List<string>();

            foreach (Node n0 in nodes)
            {
                //Distance to all nodes except the source start as infinity
                timeTo[n0.nodeId] = double.PositiveInfinity;
                if (n0.nodeId != startID)
                {
                    nodesToVisit.Add(n0.nodeId);
                }
            }

            timeTo[startID] = 0;
            nodesToVisit.Add(startID);

            //Tracks the node immediately before a node on the currently known shortest path, used to reconstruct the path at the end of the algorithm
            Dictionary<string, string> pathParents = new Dictionary<string, string>();
            pathParents[startID] = null;

            while (nodesToVisit.Count > 0)
            {
                string v = getMinDistNode(timeTo, nodesToVisit);
                nodesToVisit.Remove(v);

                foreach (Node.Neighbour neighbour in nodeIdNodeDict[v].neighbours)
                {
                    if (neighbour.neighbourId == excludeID) continue;
                    //if the time to v + distance from v to its neighbour is shorted than the shortest known distance from source to neighbours, update the shortest distance to neighbour
                    if ((timeTo[v] + neighbour.distance) < timeTo[neighbour.neighbourId])
                    {
                        timeTo[neighbour.neighbourId] = timeTo[v] + neighbour.distance;
                        pathParents[neighbour.neighbourId] = v;
                    }
                }
            }
            List<string> path = new List<string>();

            // Reconstruct path from end node to start node
            string n = endID;
            while (pathParents.ContainsKey(n) && pathParents[n] != null)
            {
                path.Add(n);
                n = pathParents[n];
            }
            path.Add(startID); // Add the starting point
            path.Reverse(); // Reverse so it goes start->finish
            return path;
        }



        /// <summary>
        ///     Uses Djikstra's algorithm to find the shortest path, time-wise, between two graph nodes
        ///     with the specified node IDs. Throws ArgumentException if nodes are not in the graph.
        /// </summary>
        /// <param name="startID"> A node ID that denotes the origin location, type string. </param>
        /// <param name="endID"> A node ID that denotes the destination location, type string.</param>
        /// <returns>
        ///     An ordered list of strings that denote each node ID of nodes to be visited from origin to destination.
        /// </returns>
        public List<string> PathToFrom(string startID, string endID)
        {
            if (!nodeIdNodeDict.ContainsKey(startID) || !nodeIdNodeDict.ContainsKey(endID))
            {
                throw new ArgumentException("StartID and EndID must be valid node IDs for this graph");
            }

            List<string> nodeIDs = new List<string>();

            //List of current shortest known travel times to each node from the source
            Dictionary<string, double> timeTo = new Dictionary<string, double>();

            //Tracks which nodes still need to be visited, nodes are removed as they are visited
            List<string> nodesToVisit = new List<string>();

            foreach (Node n0 in nodes)
            {
                //Distance to all nodes except the source start as infinity
                timeTo[n0.nodeId] = double.PositiveInfinity;
                if (n0.nodeId != startID)
                {
                    nodesToVisit.Add(n0.nodeId);
                }
            }

            timeTo[startID] = 0;
            nodesToVisit.Add(startID);

            //Tracks the node immediately before a node on the currently known shortest path, used to reconstruct the path at the end of the algorithm
            Dictionary<string, string> pathParents = new Dictionary<string, string>();
            pathParents[startID] = null;

            while (nodesToVisit.Count > 0)
            {
                string v = getMinDistNode(timeTo, nodesToVisit);
                nodesToVisit.Remove(v);

                foreach (Node.Neighbour neighbour in nodeIdNodeDict[v].neighbours)
                {
                    //if the time to v + distance from v to its neighbour is shorted than the shortest known distance from source to neighbours, update the shortest distance to neighbour
                    if ((timeTo[v] + neighbour.distance) < timeTo[neighbour.neighbourId])
                    {
                        timeTo[neighbour.neighbourId] = timeTo[v] + neighbour.distance;
                        pathParents[neighbour.neighbourId] = v;
                    }
                }
            }
            List<string> path = new List<string>();

            // Reconstruct path from end node to start node
            string n = endID;
            while (pathParents.ContainsKey(n) && pathParents[n] != null)
            {
                path.Add(n);
                n = pathParents[n];
            }
            path.Add(startID); // Add the starting point
            path.Reverse(); // Reverse so it goes start->finish
            return path;
        }

        //Picks the node with the lowest known distance to it that hasn't been visited yet, basically works around implementing
        // a priority queue
        public string getMinDistNode(Dictionary<string, double> timeTo, List<string> unvisitedNodes)
        {
            if (unvisitedNodes.Count == 0)
            {
                throw new System.ArgumentException("Unvisited nodes list cannot be empty");
            }

            double min = double.PositiveInfinity;
            string minNode = null;

            foreach (string n in unvisitedNodes)
            {
                if (timeTo[n] < min)
                {
                    min = timeTo[n];
                    minNode = n;
                }
            }

            if (minNode == null)
            {
                minNode = unvisitedNodes[0];
            }
            return minNode;
        }

        //Add a single car to a route
        public void AddCongestion(string startID, string endID)
        {
            if (carsOnRoute.ContainsKey((startID, endID)))
            {
                carsOnRoute[(startID, endID)] = carsOnRoute[(startID, endID)] + 1;
            }
            else
            {
                carsOnRoute[(startID, endID)] = 1;
            }
        }

        //Remove a single car from a route
        //Never goes negative
        public void RemoveCongestion(string startID, string endID)
        {
            if (carsOnRoute.ContainsKey((startID, endID)))
            {
                if (carsOnRoute[(startID, endID)]>=1)
                {
                    carsOnRoute[(startID, endID)] = carsOnRoute[(startID, endID)] - 1;
                }
                else
                {
                    carsOnRoute[(startID, endID)] = 0;
                }
            }
        }
    }
}
