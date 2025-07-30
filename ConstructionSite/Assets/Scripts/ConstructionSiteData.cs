using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConstructionSiteDataInfo
{
    public List<ConstructionSite> ConstructionSites;
}

[System.Serializable]
public class ConstructionSite
{
    public string Name;
    public string Address;
    public string City;
    public string State;
    public string Zip;
    public string Phone;
    public string Dimensions;
    public Layout Layout;
    public RoadSystem RoadSystem;
    public string SiteConfigTopic;
    public string SiteStatusTopic;
    public bool IsConfigured = false;


}

[System.Serializable]
public class Layout
{
    public int NumberOfVertices;
    public List<Vertex> Vertices;
    public List<Zone> Zones;
}

[System.Serializable]
public class Zone
{
    public string Name;
    public string Type;
    public string ZoneConfigTopic;
    public string ZoneStatusTopic;
    public List<Vertex> Vertices;
    public List<Gate> Gates;

}

[System.Serializable]
public class Gate
{
    public int StartVertex;
    public int EndVertex;

    public Gate(int startVertex, int endVertex)
    {
        StartVertex = startVertex;
        EndVertex = endVertex;
    }
}

[System.Serializable]
public class Vertex
{
    public float X;
    public float Y;
    public float Z;

    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }

    // Constructor
    public Vertex(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vertex FromVector3(Vector3 vector)
    {
        return new Vertex(vector.x, vector.y, vector.z);
    }
}

[System.Serializable]
public class RoadSystem
{
    public List<RoadNode> Nodes; // List of all road nodes
    public List<RoadEdge> Edges; // List of all edges connecting nodes


}

[System.Serializable]
public class RoadNode
{
    public string Id = Guid.NewGuid().ToString(); // Store Guid as a string for serialization
    public Vertex Position; // The 3D coordinates of the node
    public List<string> ConnectedEdges = new List<string>(); // Store connected edge IDs as strings


}

[System.Serializable]
public class RoadEdge
{
    public string Id = Guid.NewGuid().ToString(); // Store Guid as a string for serialization
    public string StartNode; // Store StartNode as a string ID
    public string EndNode; // Store EndNode as a string ID
    public float Width; // Width of the road
    public float Length; // Length of the road, computed during initialization
    public List<Lane> Lanes; // List of lanes
}

[System.Serializable]
public class Lane
{
    public string Id = Guid.NewGuid().ToString(); // Store Guid as a string for serialization
    public int Direction; // +1 for StartNode->EndNode, -1 for EndNode->StartNode
}