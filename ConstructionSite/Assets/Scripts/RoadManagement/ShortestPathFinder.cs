using System;
using System.Collections.Generic;
using UnityEngine;

public class ShortestPathFinder : MonoBehaviour
{
    public List<String> FindShortestPath(String startNodeId, String endNodeId, RoadSystem roadSystem)
    {
        if (roadSystem == null || roadSystem.Nodes == null || roadSystem.Edges == null)
            return null;

        var nodes = roadSystem.Nodes;
        var edges = roadSystem.Edges;

        // Initialize the distance and previous node dictionaries
        Dictionary<string, float> distances = new Dictionary<string, float>();
        Dictionary<string, string> previousNodes = new Dictionary<string, string>();
        HashSet<string> unvisitedNodes = new HashSet<string>();

        foreach (var node in nodes)
        {
            distances[node.Id] = float.MaxValue;
            previousNodes[node.Id] = null;
            unvisitedNodes.Add(node.Id);
        }

        distances[startNodeId] = 0;

        while (unvisitedNodes.Count > 0)
        {
            // Get the node with the smallest distance
            string currentNodeId = GetNodeWithSmallestDistance(unvisitedNodes, distances);
            Debug.Log("Current Node: " + currentNodeId);
            unvisitedNodes.Remove(currentNodeId);

            if (currentNodeId == endNodeId)
                break;

            var currentNode = nodes.Find(n => n.Id == currentNodeId);
            foreach (var edge in edges)
            {
                if (edge.StartNode != currentNodeId && edge.EndNode != currentNodeId)
                    continue;

                string neighborNodeId = (edge.StartNode == currentNodeId) ? edge.EndNode : edge.StartNode;
                if (!unvisitedNodes.Contains(neighborNodeId))
                    continue;

                // Check if the direction of the lane allows travel from currentNodeId to neighborNodeId
                bool canTravel = false;
                foreach (var lane in edge.Lanes)
                {
                    if ((lane.Direction == 1 && edge.StartNode == currentNodeId) ||
                        (lane.Direction == -1 && edge.EndNode == currentNodeId))
                    {
                        canTravel = true;
                        break;
                    }
                }

                if (!canTravel)
                    continue;

                float newDist = distances[currentNodeId] + edge.Length;
                if (newDist < distances[neighborNodeId])
                {
                    distances[neighborNodeId] = newDist;
                    previousNodes[neighborNodeId] = currentNodeId;
                }
            }
        }

        // Reconstruct the shortest path
        List<string> path = new List<string>();
        string current = endNodeId;
        while (current != null)
        {
            path.Insert(0, current);
            current = previousNodes[current];
        }

        if (path[0] != startNodeId)
            return null; // No path found

        return path;
    }

    private string GetNodeWithSmallestDistance(HashSet<string> unvisitedNodes, Dictionary<string, float> distances)
    {
        float smallestDistance = float.MaxValue;
        string smallestNodeId = null;

        foreach (var nodeId in unvisitedNodes)
        {
            if (distances[nodeId] < smallestDistance)
            {
                smallestDistance = distances[nodeId];
                smallestNodeId = nodeId;
            }
        }

        return smallestNodeId;
    }
}