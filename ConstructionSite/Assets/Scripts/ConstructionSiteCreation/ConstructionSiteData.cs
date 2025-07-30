using System;
using System.Collections.Generic;
using UnityEngine;

/// @brief Represents data for a construction site.
[System.Serializable]
public class ConstructionSiteData
{
    public string inputField1;
    public string inputField2;
    public string inputField3;

    public PolygonData mainPerimeter;
    public List<PolygonData> zonePerimeters;

    /// @brief Constructor for ConstructionSiteData.
    /// @param field1 First input field.
    /// @param field2 Second input field.
    /// @param field3 Third input field.
    /// @param mainPerimeter Main perimeter of the construction site.
    /// @param zonePerimeters List of zone perimeters.
    public ConstructionSiteData(string field1, string field2, string field3, PolygonData mainPerimeter, List<PolygonData> zones)
    {
        inputField1 = field1;
        inputField2 = field2;
        inputField3 = field3;
        this.mainPerimeter = mainPerimeter;
        this.zonePerimeters = zones;
    }
}

/// @brief Represents a point in 3D space.
[System.Serializable]
public class PointData
{
    public float x;
    public float y;
    public float z;

    /// @brief Constructor for PointData.
    /// @param x X coordinate.
    /// @param y Y coordinate.
    /// @param z Z coordinate.
    public PointData(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// @brief Converts PointData to Vector3.
    /// @return A Vector3 representation of the point.
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    /// @brief Converts a Vector3 to PointData.
    /// @param vector The Vector3 to convert.
    /// @return A PointData representation of the vector.
    public static PointData FromVector3(Vector3 vector)
    {
        return new PointData(vector.x, vector.y, vector.z);
    }

    /// @brief Implicit conversion from PointData to Vector3.
    /// @param v The PointData to convert.
    /// @return A Vector3 representation of the point.
    public static implicit operator Vector3(PointData v)
    {
        throw new NotImplementedException();
    }
}

/// @brief Represents a polygon made up of points.
[System.Serializable]
public class PolygonData
{
    public List<PointData> points;
    public List<Gate> gates;
    public string ZoneConfigTopic;
    public string ZoneStatusTopic;
    public String Name;
    public String Type;
    /// @brief Constructor for PolygonData.
    /// @param vertices List of vertices defining the polygon.
    public PolygonData(List<Vector3> vertices)
    {
        points = new List<PointData>();
        gates = new List<Gate>();
        foreach (var vertex in vertices)
        {
            points.Add(PointData.FromVector3(vertex));
        }
    }



    /// @brief Converts the polygon to a list of Vector3 points.
    /// @return A list of Vector3 points representing the polygon.
    public List<Vector3> ToVector3List()
    {
        List<Vector3> vectorList = new List<Vector3>();
        foreach (var point in points)
        {
            vectorList.Add(point.ToVector3());
        }
        return vectorList;
    }

    public void AddGateToZone(int startVertex, int endVertex)
    {
        if (gates == null)
        {
            gates = new List<Gate>();
        }
        gates.Add(new Gate(startVertex, endVertex));
    }



    //Return the indexes of the sides with the gates
    public List<int> GetGatesIndexes()
    {
        List<int> indexes = new List<int>();
        foreach (var gate in gates)
        {
            indexes.Add(gate.StartVertex);
        }
        return indexes;
    }



    public List<Gate> GetGates()
    {
        return gates;
    }



    /// @brief Gets the number of points in the polygon.
    public int Count => points.Count;
}

[System.Serializable]
public class ZoneData
{
    public string zoneName;
    public string zoneType;
    public PolygonData zonePerimeter;

    public List<Gate> gates;

    public ZoneData(string zoneName, string zoneType, PolygonData zonePerimeter, List<Gate> gates)
    {
        this.zoneName = zoneName;
        this.zoneType = zoneType;
        this.zonePerimeter = zonePerimeter;
        this.gates = gates;
    }


    public class Gate
    {
        public int startVertex;
        public int endVertex;
    }
}



