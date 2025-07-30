using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using Unity.AI.Navigation;
using System.Linq;
using System;

public class RoadGenerator : MonoBehaviour
{
    public SelectedSiteData selectedSiteData;
    public GameObject NodePrefab; // Assign a Cube prefab for nodes
    public Color EdgeColor = Color.blue;
    public Color LaneColorUp = Color.green;
    public Color LaneColorDown = Color.red;
    public Color PathColor = Color.magenta;

    public bool VisualizeAllEdges = true;
    public bool VisualizeLanes = true;

    public String StartNodeId = "0";
    public String EndNodeId = "1";

    private GameObject RoadSystem;

    public ShortestPathFinder shortestPathFinder;
    public float NodeOffset = 1f;

    private ConstructionSiteDataInfo constructionSiteData;

    private RoadSystem roadSystem;

    public void StartRoadGeneration()
    {
        LoadRoadSystem();
        VisualizeRoadSystem();
    }

    private void LoadRoadSystem()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "siteData.json");

        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            constructionSiteData = JsonUtility.FromJson<ConstructionSiteDataInfo>(jsonText);
            Debug.Log("File loaded successfully.");
        }
        else
        {
            Debug.LogError($"File not found at: {filePath}");
        }
    }

    private void VisualizeRoadSystem()
    {
        if (constructionSiteData == null || constructionSiteData.ConstructionSites == null)
            return;

        ConstructionSite selectedSite = null;

        if (selectedSiteData.siteIndex >= 0 && selectedSiteData.siteIndex < constructionSiteData.ConstructionSites.Count)
        {
            selectedSite = constructionSiteData.ConstructionSites[selectedSiteData.siteIndex];
        }
        else if (!string.IsNullOrEmpty(selectedSiteData.siteName))
        {
            selectedSite = constructionSiteData.ConstructionSites.Find(site => site.Name == selectedSiteData.siteName);
        }

        if (selectedSite != null)
        {
            if (selectedSite.RoadSystem == null || selectedSite.RoadSystem.Nodes == null || selectedSite.RoadSystem.Edges == null)
                return;


            roadSystem = selectedSite.RoadSystem;
            VisualizeNodes(selectedSite.RoadSystem.Nodes);
            VisualizeEdges(selectedSite.RoadSystem.Nodes, selectedSite.RoadSystem.Edges);
        }
        else
        {
            Debug.LogError("Selected site not found.");
        }

    }

    private void VisualizeNodes(List<RoadNode> nodes)
    {
        foreach (var node in nodes)
        {
            Vector3 position = offsetNode(node.Position.ToVector3());
            //Debug.LogWarning($"Node {node.Id} at {position}");
            GameObject nodeObject = Instantiate(NodePrefab, position, Quaternion.identity);
            nodeObject.name = $"Node_{node.Id}";

            // Add the node to the RoadSystem object as a child
            if (RoadSystem == null)
            {
                RoadSystem = new GameObject("RoadSystem");
            }
            nodeObject.transform.parent = RoadSystem.transform;


            // Check if the node is over any Gate Collider and if so, change the color
            var gateTriggers = GameObject.FindGameObjectsWithTag("GateTrigger");
            foreach (var gateTrigger in gateTriggers)
            {
                if (nodeObject.GetComponent<Collider>().bounds.Intersects(gateTrigger.GetComponent<Collider>().bounds))
                {
                    nodeObject.GetComponent<Renderer>().material.color = Color.yellow;
                    // Set the node as child of the parent of the parent of gate trigger
                    nodeObject.transform.parent = gateTrigger.transform.parent.parent;
                    // Change the tag of the node to "GateNode"
                    nodeObject.tag = "GateNode";
                    break;
                }
            }
        }
    }

    private void VisualizeEdges(List<RoadNode> nodes, List<RoadEdge> edges)
    {
        var nodeDictionary = nodes.ToDictionary(n => n.Id, n => n);

        foreach (var edge in edges)
        {
            if (!nodeDictionary.ContainsKey(edge.StartNode) || !nodeDictionary.ContainsKey(edge.EndNode))
            {
                Debug.LogWarning($"Edge references invalid node IDs: Start {edge.StartNode}, End {edge.EndNode}");
                continue;
            }

            Vector3 startPosition = offsetNode(nodeDictionary[edge.StartNode].Position.ToVector3());
            Vector3 endPosition = offsetNode(nodeDictionary[edge.EndNode].Position.ToVector3());

            if (VisualizeAllEdges)
            {
                // Draw a line between the nodes
                GameObject edgeObject = new GameObject($"Edge_{edge.Id}_({edge.StartNode}_{edge.EndNode})");
                LineRenderer lineRenderer = edgeObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.5f;
                lineRenderer.endWidth = 0.5f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startPosition);
                lineRenderer.SetPosition(1, endPosition);
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = EdgeColor;
                lineRenderer.endColor = EdgeColor;

                // Add the edge to the RoadSystem object as a child
                if (RoadSystem == null)
                {
                    RoadSystem = new GameObject("RoadSystem");
                }
                edgeObject.transform.parent = RoadSystem.transform;
            }

            if (!VisualizeLanes)
                continue;

            // Calculate the direction vector of the edge
            Vector3 directionVector = (endPosition - startPosition).normalized;
            Vector3 perpendicularVector = Vector3.Cross(directionVector, Vector3.up);
            // Draw the lanes
            foreach (var lane in edge.Lanes)
            {
                Vector3 laneOffset = perpendicularVector * edge.Width * 0.5f * lane.Direction;
                Vector3 laneStartPosition = startPosition + laneOffset;
                Vector3 laneEndPosition = endPosition + laneOffset;

                GameObject laneObject = new GameObject($"Lane_{lane.Id}_Edge_{edge.Id}");
                LineRenderer laneLineRenderer = laneObject.AddComponent<LineRenderer>();
                laneLineRenderer.startWidth = 1f;
                laneLineRenderer.endWidth = 1f;
                laneLineRenderer.positionCount = 2;
                laneLineRenderer.SetPosition(0, laneStartPosition);
                laneLineRenderer.SetPosition(1, laneEndPosition);
                laneLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                laneLineRenderer.startColor = lane.Direction > 0 ? LaneColorUp : LaneColorDown;
                laneLineRenderer.endColor = lane.Direction > 0 ? LaneColorDown : LaneColorUp;

                // Add the lane to the RoadSystem object as a child
                if (RoadSystem == null)
                {
                    RoadSystem = new GameObject("RoadSystem");
                }
                laneObject.transform.parent = RoadSystem.transform;

            }
        }
    }

    private Vector3 offsetNode(Vector3 position)
    {

        int layerMask = ~LayerMask.GetMask("Road");
        // raycast to find the ground and offset the node in Y by 1
        RaycastHit hit;


        if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            position.y = hit.point.y + NodeOffset;
            return position;
        }
        else
        {
            return position;
        }

    }

    public List<String> GetPathToDestination(String ZoneStartName, String ZoneEndName, GameObject machine)
    {
        // Search on the hierarchy for the zone object
        GameObject ZoneStart = GameObject.Find(ZoneStartName);
        GameObject ZoneEnd = GameObject.Find(ZoneEndName);
        if (ZoneEnd == null)
        {
            Debug.LogError($"Zone {ZoneEndName} not found.");
            return null;
        }

        if (ZoneStart == null)
        {
            Debug.LogError($"Zone {ZoneStartName} not found.");
            return null;
        }

        // Search for the nodes that are children of the zone object by tag
        var endNodes = ZoneEnd.GetComponentsInChildren<Transform>().Where(t => t.CompareTag("GateNode")).ToList();
        if (endNodes.Count == 0)
        {
            Debug.LogWarning($"No gate nodes found in zone {ZoneEnd}");
            return null;
        }

        // Find the closest node to the machine
        Transform closestEndNode = endNodes[0];
        float minEndDistance = Vector3.Distance(machine.transform.position, closestEndNode.position);
        foreach (var node in endNodes)
        {
            float distance = Vector3.Distance(machine.transform.position, node.position);
            if (distance < minEndDistance)
            {
                minEndDistance = distance;
                closestEndNode = node;
            }
        }



        // Repeat the process for the start zone
        var startNodes = ZoneStart.GetComponentsInChildren<Transform>().Where(t => t.CompareTag("GateNode")).ToList();
        if (startNodes.Count == 0)
        {
            Debug.LogWarning($"No gate nodes found in zone {ZoneStart}");
            return null;
        }

        Transform closestStartNode = startNodes[0];
        float minStartDistance = Vector3.Distance(machine.transform.position, closestStartNode.position);
        foreach (var node in startNodes)
        {
            float distance = Vector3.Distance(machine.transform.position, node.position);
            if (distance < minStartDistance)
            {
                minStartDistance = distance;
                closestStartNode = node;
            }
        }


        Debug.LogWarning("Starting node: " + closestStartNode.name + " Ending node: " + closestEndNode.name);

        // Remove the "node_" prefix from the node names
        closestStartNode.name = closestStartNode.name.Replace("Node_", "");
        closestEndNode.name = closestEndNode.name.Replace("Node_", "");

        // Find the shortest path between the nodes
        List<String> path = shortestPathFinder.FindShortestPath(closestStartNode.name, closestEndNode.name, roadSystem);

        if (path == null)
        {
            Debug.LogWarning("No path found between nodes 0 and 1");
            return null;
        }

        Debug.LogWarning("Shortest path found");

        // Visualize the path
        VisualizeShortestPath(roadSystem.Nodes, path);

        // Return the path
        return path;

    }


    private void FindShortestPath()
    {
        if (constructionSiteData == null || constructionSiteData.ConstructionSites == null)
            return;

        foreach (var site in constructionSiteData.ConstructionSites)
        {
            if (site.RoadSystem == null || site.RoadSystem.Nodes == null || site.RoadSystem.Edges == null)
                continue;

            List<String> path = shortestPathFinder.FindShortestPath(StartNodeId, EndNodeId, site.RoadSystem);
            if (path == null)
            {
                Debug.LogWarning("No path found between nodes 0 and 1");
                continue;
            }

            Debug.LogWarning("Shortest path found:");
            foreach (var nodeId in path)
            {
                Debug.LogWarning($"Node {nodeId}");
            }
            VisualizeShortestPath(site.RoadSystem.Nodes, path);
        }
    }

    // Visualize the shortest path by drawing a line between the nodes
    private void VisualizeShortestPath(List<RoadNode> nodes, List<String> path)
    {
        if (path == null || path.Count < 2)
            return;

        for (int i = 0; i < path.Count - 1; i++)
        {
            String startNodeId = path[i];
            String endNodeId = path[i + 1];

            RoadNode startNode = nodes.Find(n => n.Id == startNodeId);
            RoadNode endNode = nodes.Find(n => n.Id == endNodeId);

            if (startNode == null || endNode == null)
                continue;

            Vector3 startPosition = offsetNode(startNode.Position.ToVector3());
            Vector3 endPosition = offsetNode(endNode.Position.ToVector3());

            GameObject pathObject = new GameObject($"Path_{startNodeId}_{endNodeId}");
            LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = PathColor;
            lineRenderer.endColor = PathColor;
        }
    }
}
