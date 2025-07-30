using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PerimeterManager : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Material lineMaterial;
    public string jsonFilePath;
    public Terrain mapTerrain;

    public ZoneManager zoneManager; // Reference to ZoneManager
    public SnapManager snapManager; // Reference to SnapManager

    private PolygonData mainPerimeter;
    private PolygonData orginalMainPerimeter;

    /// @brief Initializes the main perimeter by loading and rendering it.
    public void InitializeMainPerimeter()
    {
        LoadPerimeterFromJson(jsonFilePath);
        RenderPerimeter();
    }

    /// @brief Loads perimeter data from a JSON file.
    /// @param relativePath The relative path to the JSON file.
    private void LoadPerimeterFromJson(string relativePath)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Perimeter_DONOTOUCH!!!");

        if (jsonFile != null)
        {
            string json = jsonFile.text;

            mainPerimeter = JsonUtility.FromJson<PolygonData>(json);
            orginalMainPerimeter = JsonUtility.FromJson<PolygonData>(json); // Assuming you want to keep the original as well
            foreach (var point in mainPerimeter.points)
            {
                point.y = GetYFromTerrainData(point.x, point.z);
            }

            orginalMainPerimeter = mainPerimeter;

            Debug.Log("Loaded perimeter data with adjusted heights for " + mainPerimeter.points.Count + " points.");

        }
        else
        {
            Debug.LogError("JSON file not found at path: " + relativePath);
        }
    }

    /// @brief Gets the original main perimeter.
    /// @return The original main perimeter as PolygonData.
    public PolygonData GetOriginalMainPerimeter()
    {
        return orginalMainPerimeter;
    }

    /// @brief Gets the Y-coordinate from terrain data for given X and Z coordinates.
    /// @param x The X-coordinate.
    /// @param z The Z-coordinate.
    /// @return The Y-coordinate adjusted by terrain data.
    public float GetYFromTerrainData(float x, float z)
    {
        TerrainData terrainData = mapTerrain.terrainData;
        Vector3 terrainPos = mapTerrain.transform.position;

        int mapX = Mathf.FloorToInt((x - terrainPos.x) / terrainData.size.x * terrainData.heightmapResolution);
        int mapZ = Mathf.FloorToInt((z - terrainPos.z) / terrainData.size.z * terrainData.heightmapResolution);

        float y = terrainData.GetHeight(mapX, mapZ);
        return y + terrainPos.y + 6f;
    }

    /// @brief Generates an updated main perimeter based on zone points.
    /// @param zonePoints The list of zone points.
    /// @return The updated main perimeter as PolygonData.
    public PolygonData GenerateUpdatedMainPerimeter(List<Vector3> zonePoints)
    {
        List<Vector3> newMainPerimeter = new List<Vector3>();
        List<Vector3> oldPerimeterPoints = mainPerimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));


        int firstZoneIndex = FindClosestSegmentIndex(oldPerimeterPoints, zonePoints[0]);
        int lastZoneIndex = FindClosestSegmentIndex(oldPerimeterPoints, zonePoints[zonePoints.Count - 1]);
        // Debug.Log("PerimeterManagger: " + firstZoneIndex + " " + lastZoneIndex);

        bool isWrapAround = lastZoneIndex < firstZoneIndex;
        if (isWrapAround)
        {
            // Debug.Log("Wrap around");
            for (int i = (lastZoneIndex + 1) % oldPerimeterPoints.Count; i != ((firstZoneIndex + 1) % mainPerimeter.Count); i = (i + 1) % oldPerimeterPoints.Count)

            {
                Debug.Log(i);
                newMainPerimeter.Add(oldPerimeterPoints[i]);
            }
            newMainPerimeter.AddRange(zonePoints);
        }
        else
        {
            // Debug.Log("No wrap around");
            for (int i = 0; i <= firstZoneIndex; i++)
            {
                newMainPerimeter.Add(oldPerimeterPoints[i]);
            }
            newMainPerimeter.AddRange(zonePoints);
            for (int i = lastZoneIndex + 1; i < oldPerimeterPoints.Count; i++)
            {
                newMainPerimeter.Add(oldPerimeterPoints[i]);
            }
        }

        // Debug.Log(newMainPerimeter.Count);
        // Update the main perimeter
        mainPerimeter = new PolygonData(newMainPerimeter);
        snapManager.SetMainPerimeterVertices(mainPerimeter);
        RenderPerimeter();


        return mainPerimeter;
    }

    /// @brief Checks if two edges intersect in 2D space (ignoring y-coordinate).
    /// @param a1 The first point of the first edge.
    /// @param a2 The second point of the first edge.
    /// @param b1 The first point of the second edge.
    /// @param b2 The second point of the second edge.
    /// @return True if the edges intersect, false otherwise.
    private static bool EdgesIntersectIgnoreY(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        // Project Vector3 into 2D (x, z) for calculations
        float det = (a2.x - a1.x) * (b2.z - b1.z) - (a2.z - a1.z) * (b2.x - b1.x);
        if (Mathf.Approximately(det, 0)) return false; // Parallel lines

        float t = ((b1.x - a1.x) * (b2.z - b1.z) - (b1.z - a1.z) * (b2.x - b1.x)) / det;
        float u = ((b1.x - a1.x) * (a2.z - a1.z) - (b1.z - a1.z) * (a2.x - a1.x)) / det;

        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }

    /// @brief Gets the excluded points from the original perimeter that are not in the new perimeter.
    /// @param originalPerimeter The original perimeter points.
    /// @param newPerimeter The new perimeter points.
    /// @param firstZoneIndex The index of the first zone point.
    /// @return The list of excluded points.
    private List<Vector3> GetExcludedPoints(List<Vector3> originalPerimeter, List<Vector3> newPerimeter, int firstZoneIndex)
    {
        List<Vector3> excludedPoints = new List<Vector3>();
        bool inside = false;
        Debug.Log(firstZoneIndex);
        for (int i = (firstZoneIndex + 1) % originalPerimeter.Count; i < originalPerimeter.Count && !inside; i = (i + 1) % originalPerimeter.Count)
        {

            if (newPerimeter.Contains(originalPerimeter[i]))
            {
                inside = true;
            }
            else
                excludedPoints.Add(originalPerimeter[i]);


        }

        return excludedPoints;
    }

    /// @brief Finds the index of the segment in mainPerimeter that is closest to a given point.
    /// @param mainPerimeterPoints The list of main perimeter points.
    /// @param zonePoint The zone point to find the closest segment to.
    /// @return The index of the closest segment.
    private int FindClosestSegmentIndex(List<Vector3> mainPerimeterPoints, Vector3 zonePoint)
    {
        int closestSegmentIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < mainPerimeterPoints.Count; i++)
        {
            Vector3 start = mainPerimeterPoints[i];
            Vector3 end = mainPerimeterPoints[(i + 1) % mainPerimeterPoints.Count];

            // Find the closest point on the segment (start, end) to zonePoint
            Vector3 closestPointOnSegment = GetClosestPointOnLine(start, end, zonePoint);
            float distance = Vector3.Distance(closestPointOnSegment, zonePoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSegmentIndex = i;
            }
        }

        return closestSegmentIndex;
    }

    /// @brief Returns the closest point on a line segment to a given point.
    /// @param start The start point of the line segment.
    /// @param end The end point of the line segment.
    /// @param point The point to find the closest point to.
    /// @return The closest point on the line segment.
    private Vector3 GetClosestPointOnLine(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 lineDirection = end - start;
        float lineLengthSquared = lineDirection.sqrMagnitude;

        if (lineLengthSquared == 0) return start; // If start and end are the same point

        float t = Vector3.Dot(point - start, lineDirection) / lineLengthSquared;
        t = Mathf.Clamp01(t); // Clamps t to the segment range [0,1]

        return start + t * lineDirection;
    }

    /// @brief Renders the perimeter using LineRenderer.
    void RenderPerimeter()
    {
        List<Vector3> renderPoints = mainPerimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));

        // Add the first point at the end of the list just for rendering purposes
        if (renderPoints.Count > 1)
        {
            renderPoints.Add(renderPoints[0]);
        }

        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = 10f; // Adjust as needed
        lineRenderer.endWidth = 10f;
        lineRenderer.positionCount = renderPoints.Count;
        lineRenderer.SetPositions(renderPoints.ToArray());

        Debug.Log("Perimeter rendered with loop closure for display purposes.");
    }

    /// @brief Accessor for main perimeter vertices as PolygonData.
    /// @return The main perimeter vertices as PolygonData.
    public PolygonData GetMainPerimeterVertices()
    {
        return mainPerimeter;
    }

    /// @brief Clears the main perimeter and the rendered perimeter.
    public void ClearMainPerimeter()
    {
        mainPerimeter = null;
        lineRenderer.positionCount = 0; // Clear the rendered perimeter
        Debug.Log("PerimeterManager: Main perimeter cleared.");
    }
}
