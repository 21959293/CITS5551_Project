using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gta_background_traffic_v2;
using System;

public class SmartPathing
{
    public Graph parentGraph { get; set; }

    /// <summary>
    ///     TrafficWithoutUnity() class constructor.
    /// </summary>
    /// <param name="graph"> The road network graph (<see cref="Graph"/>), type Graph.</param>
    public SmartPathing(Graph graph)
    {
        this.parentGraph = graph;
    }

    /// <summary>
    ///     Uses Djikstra's algorithm to find the shortest path, between two graph nodes
    ///     with the specified node IDs. 
    ///     Takes into account congestion.
    ///     Throws ArgumentException if nodes are not in the graph.
    /// </summary>
    /// <param name="startID"> A node ID that denotes the origin location, type string. </param>
    /// <param name="endID"> A node ID that denotes the destination location, type string.</param>
    /// <returns>
    ///     An ordered list of strings that denote each node ID of nodes to be visited from origin to destination.
    /// </returns>
    public List<string> PathToFrom(string startID, string endID)
    {
        if (!parentGraph.nodeIdNodeDict.ContainsKey(startID) || !parentGraph.nodeIdNodeDict.ContainsKey(endID))
        {
            throw new ArgumentException("StartID and EndID must be valid node IDs for this graph");
        }

        List<string> nodeIDs = new List<string>();

        //List of current shortest known travel times to each node from the source
        Dictionary<string, double> timeTo = new Dictionary<string, double>();

        //Tracks which nodes still need to be visited, nodes are removed as they are visited
        List<string> nodesToVisit = new List<string>();

        foreach (Graph.Node n0 in parentGraph.nodes)
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
            string v = parentGraph.getMinDistNode(timeTo, nodesToVisit);
            nodesToVisit.Remove(v);

            foreach (Graph.Node.Neighbour neighbour in parentGraph.nodeIdNodeDict[v].neighbours)
            {
                //if the time to v + time from v to its neighbour is shorted than the shortest known time from source to neighbours, update the shortest time to neighbour
                double travelCost = CongestionCost(v, neighbour.neighbourId, neighbour.distance);
                if ((timeTo[v] + travelCost) < timeTo[neighbour.neighbourId])
                {
                    timeTo[neighbour.neighbourId] = timeTo[v] + travelCost;
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
    /// Helper method to add congestion modifier
    /// </summary>
    /// <param name="startID">String representing the initial node</param>
    /// <param name="endID">String representing the end node</param>
    /// <param name="distance">Double representing the distance</param>
    /// <returns></returns>
    private double CongestionCost(string startID, string endID, double distance)
    {
        //Change modifier as needed to change cost of traffic
        double modifier = 0.1;
        int numCars;
        if (parentGraph.carsOnRoute.ContainsKey((startID, endID)))
        {
            numCars = parentGraph.carsOnRoute[(startID, endID)];
        }
        else
        {
            numCars = 0;
        }
        return (distance + (numCars * modifier));
    }

}
