using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class RoadManager : MonoBehaviour
{

    private bool isRoadCreationActive = false;
    public GameObject vertexMarkerPrefab;
    private List<GameObject> activeNodes = new List<GameObject>();
    private List<GameObject> activeRoads = new List<GameObject>();
    private List<RoadNode> roadPoints = new List<RoadNode>();
    RoadSystem roadSystem = new RoadSystem();

    // Start is called before the first frame update
    void Start()
    {
        roadSystem.Nodes = new List<RoadNode>();
        roadSystem.Edges = new List<RoadEdge>();
    }

    public void setRoadCreationActive(bool active)
    {

        isRoadCreationActive = active;
        if (!active)
        {

            roadPoints.Clear();
        }
    }

    internal void AddRoadPoint(Vector3 position, string type, String nodeId)
    {
        if (isRoadCreationActive)
        {
            RoadNode node = new RoadNode();
            //first point of the segment
            if (roadPoints.Count == 0)
            {
                switch (type)
                {
                    case "gate":
                        //create a new node

                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        roadSystem.Nodes.Add(node);
                        roadPoints.Add(node);
                        break;
                    case "none":
                        //create a new node
                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        roadSystem.Nodes.Add(node);
                        roadPoints.Add(node);
                        break;
                    case "edge":
                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        roadPoints.Add(node);
                        SplitEdge(nodeId, node);
                        roadSystem.Nodes.Add(node);
                        break;
                    case "vertex":
                        node = roadSystem.Nodes.Find(n => n.Id == nodeId);
                        roadPoints.Add(node);
                        break;
                    default:
                        break;
                }
            }
            else //second point of the segment
            {
                switch (type)
                {
                    case "gate":
                        //create a new node

                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        roadSystem.Nodes.Add(node);
                        UpdateConnectedEdges(roadPoints.Last(), roadSystem.Nodes.Last());
                        roadPoints.Add(node);

                        EndRoadCreation();
                        break;
                    case "none":
                        //create a new node
                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        roadSystem.Nodes.Add(node);
                        UpdateConnectedEdges(roadPoints.Last(), roadSystem.Nodes.Last());
                        roadPoints.Add(node);

                        EndRoadCreation();
                        break;
                    case "edge":
                        node.Id = Guid.NewGuid().ToString();
                        node.Position = new Vertex(position.x, position.y, position.z);
                        SplitEdge(nodeId, node);
                        roadSystem.Nodes.Add(node);
                        UpdateConnectedEdges(roadPoints.Last(), roadSystem.Nodes.Last());
                        roadPoints.Add(node);

                        EndRoadCreation();
                        break;
                    case "vertex":
                        node = roadSystem.Nodes.Find(n => n.Id == nodeId);
                        UpdateConnectedEdges(roadPoints.Last(), node);
                        roadPoints.Add(node);

                        EndRoadCreation();
                        break;
                    default:
                        break;
                }
                HighlightRoad();

            }
        }
    }


    private void SplitEdge(String edgeId, RoadNode node)
    {
        Debug.Log("Splitting edge");
        //find the edge and delete it
        RoadEdge edge = roadSystem.Edges.Find(e => e.Id == edgeId);
        roadSystem.Edges.Remove(edge);

        //create a new edge 

        RoadEdge edge1 = new RoadEdge();
        edge1.Id = Guid.NewGuid().ToString();
        edge1.StartNode = edge.StartNode;
        edge1.EndNode = node.Id;
        roadSystem.Edges.Add(edge1);
        //create a new edge
        RoadEdge edge2 = new RoadEdge();
        edge2.Id = Guid.NewGuid().ToString();
        edge2.StartNode = node.Id;
        edge2.EndNode = edge.EndNode;
        roadSystem.Edges.Add(edge2);

        //update the connected edges of the start node
        RoadNode startNode = roadSystem.Nodes.Find(n => n.Id == edge.StartNode);
        startNode.ConnectedEdges.Remove(edge.Id);
        startNode.ConnectedEdges.Add(edge1.Id);
        //update the connected edges of the end node
        RoadNode endNode = roadSystem.Nodes.Find(n => n.Id == edge.EndNode);
        endNode.ConnectedEdges.Remove(edge.Id);
        endNode.ConnectedEdges.Add(edge2.Id);
        //update the connected edges of the new node
        node.ConnectedEdges = new List<String> { edge1.Id, edge2.Id };


    }

    private void UpdateConnectedEdges(RoadNode node1, RoadNode node2)
    {
        // Create a new edge
        RoadEdge edge = new RoadEdge
        {
            Id = Guid.NewGuid().ToString(),
            StartNode = node1.Id,
            EndNode = node2.Id
        };
        roadSystem.Edges.Add(edge);

        // Update the connected edges of the start node
        RoadNode startNode = roadSystem.Nodes.Find(n => n.Id == node1.Id);
        if (startNode != null)
        {
            Debug.Log(startNode.ConnectedEdges);
            startNode.ConnectedEdges.Add(edge.Id);
        }
        else
        {
            Debug.LogWarning($"Start node with Id {node1.Id} not found.");
        }

        // Update the connected edges of the end node
        RoadNode endNode = roadSystem.Nodes.Find(n => n.Id == node2.Id);
        if (endNode != null)
        {
            endNode.ConnectedEdges.Add(edge.Id);
        }
        else
        {
            Debug.LogWarning($"End node with Id {node2.Id} not found.");
        }
    }


    private void HighlightRoad()
    {
        //delete all object 

        activeRoads.ForEach(Destroy);

        List<RoadNode> nodes = roadSystem.Nodes;
        List<RoadEdge> edges = roadSystem.Edges;

        foreach (RoadEdge edge in edges)
        {
            Vector3 start = nodes.Find(n => n.Id == edge.StartNode).Position.ToVector3();
            Vector3 end = nodes.Find(n => n.Id == edge.EndNode).Position.ToVector3();

            // Crete a line rendered between the start and end points
            GameObject road = new GameObject("Road " + edge.Id);
            //ad an y offset to the road
            start.y += 3f;
            end.y += 3f;
            LineRenderer lineRenderer = road.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = 5f;
            lineRenderer.endWidth = 5f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.yellow;

            activeRoads.Add(road);
        }

        //hihlight all nodes
        activeNodes.ForEach(Destroy);
        foreach (RoadNode node in nodes)
        {
            HighlightNode(node.Position.ToVector3());
        }
    }






    //hihlight node
    internal void HighlightNode(Vector3 node)
    {
        if (vertexMarkerPrefab != null)
        {
            GameObject marker = Instantiate(vertexMarkerPrefab, node, Quaternion.identity);
            marker.GetComponent<Renderer>().material.color = Color.magenta;
            marker.SetActive(true);
            activeNodes.Add(marker);
        }

    }

    private void EndRoadCreation()
    {

        //delete the first point and move the second point to the first position
        RoadNode lastPoint = roadPoints.Last();
        roadPoints.Clear();
        roadPoints.Add(lastPoint);

        string json = JsonConvert.SerializeObject(roadSystem, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        string filePath = Application.dataPath + "/SavedRoadData.json";
        System.IO.File.WriteAllText(filePath, json);

    }

    public RoadSystem GetRoadSystem()
    {
        return roadSystem;
    }

}