using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * @brief Manages snapping functionality for construction site creation.
 */
public class SnapManager : MonoBehaviour
{
    // Snapping parameters
    public float snapRadiusVertices;
    public float snapRadiusEdges;
    public GameObject highlightObject; // Temporary highlight for snap points

    // Perimeter vertices
    private PolygonData perimeter;

    // Fields to store highlighted points
    private Vector3? highlightedVertex = null;
    private Vector3? highlightedSidePoint = null;
    private bool isGateCreationActive = false;
    private int highlightedSideIndex = -1;

    public Color normalColorSide = Color.yellow;
    public Color highlightedColorSide = Color.white;
    private int activeZoneIndex = -1;
    private bool isRoadCreationActive = false;
    public List<GameObject> gates = new List<GameObject>();
    public RoadSystem roadSystem;


    private List<LineRenderer> sideLineRenderers = new List<LineRenderer>();
    // Store selected highlights for clicked points
    private List<GameObject> selectedHighlights = new List<GameObject>();

    /**
     * @brief Sets the main perimeter vertices for snapping.
     * @param mainPerimeter The perimeter data to set.
     */
    public void SetMainPerimeterVertices(PolygonData mainPerimeter)
    {
        this.perimeter = mainPerimeter;
        if (isGateCreationActive)
        {
            Debug.Log("SnapManager: Line renderers created for perimeter sides.");

            SetLineRenderers();
        }
        Debug.Log("SnapManager: Main perimeter updated with " + mainPerimeter.points.Count + " points.");
    }

    public void SetLineRenderers()
    {
        // Here, create and setup your line renderers for each side of the perimeter
        sideLineRenderers.Clear();

        GameObject zoneObeject = GameObject.Find("Zone Perimeter " + activeZoneIndex);
        if (zoneObeject != null)
        {
            foreach (Transform child in zoneObeject.transform)
            {
                sideLineRenderers.Add(child.GetComponent<LineRenderer>());
                Debug.Log("SnapManager: Line renderers added for perimeter sides." + sideLineRenderers.Count);
            }
        }
    }

    public void SetGateCreationActive(bool active, int zoneIndex)
    {
        isGateCreationActive = active;
        activeZoneIndex = zoneIndex;

    }

    // Get the nearest snapped point if within snapRadius
    public Vector3 GetSnappedPoint(Vector3 mousePosition)
    {
        if (perimeter == null || perimeter.points == null)
        {
            Debug.LogWarning("SnapManager: mainPerimeter is not set.");
            return mousePosition; // Return original mouse position if no perimeter is set
        }
        // Check for nearby vertices first
        foreach (var pointData in perimeter.points)
        {
            Vector3 vertex = new Vector3(pointData.x, pointData.y, pointData.z);
            Vector3 vertexOnXZ = new Vector3(vertex.x, 0, vertex.z);

            if (Vector3.Distance(mousePosition, vertexOnXZ) <= snapRadiusVertices)
            {
                HighlightSnapPoint(vertex, Color.yellow);
                return vertex;
            }
        }

        // Check for nearby points on perimeter edges
        Vector3 closestPointOnEdge = new Vector3(mousePosition.x, 0, mousePosition.z); // Ensure y is zeroed for snapping calculations
        float closestDistance = snapRadiusEdges;

        for (int i = 0; i < perimeter.points.Count; i++)
        {
            // Convert PointData to Vector3 for calculations
            Vector3 start = new Vector3(perimeter.points[i].x, 0, perimeter.points[i].z);
            Vector3 end = new Vector3(perimeter.points[(i + 1) % perimeter.points.Count].x, 0, perimeter.points[(i + 1) % perimeter.points.Count].z);

            // Get the closest point on the 2D line segment (x-z plane)
            Vector3 closestPoint = GetClosestPointOnLine(start, end, closestPointOnEdge);

            float distanceToEdge = Vector3.Distance(closestPointOnEdge, closestPoint);

            if (distanceToEdge < closestDistance)
            {
                closestDistance = distanceToEdge;

                // Set the y-coordinate based on the average height of start and end points
                closestPoint.y = (perimeter.points[i].y + perimeter.points[(i + 1) % perimeter.points.Count].y) / 2;

                closestPointOnEdge = closestPoint;
                HighlightSnapPoint(closestPointOnEdge, Color.magenta);
            }
        }

        // Return the closest point on the edge if it’s within snap radius
        if (closestDistance < snapRadiusEdges)
        {
            return closestPointOnEdge;
        }

        // Reset highlight if no point was within snapping distance
        ResetHighlight();
        return mousePosition;
    }

    public int GetSnappedSide(Vector3 mousePosition)
    {
        // This function should return the index of the closest side to the mouse position
        int closestSideIndex = -1;
        float closestDistance = 20f;
        for (int i = 0; i < perimeter.points.Count; i++)
        {
            if (!perimeter.GetGatesIndexes().Contains(i))
            {
                Vector3 start = perimeter.points[i].ToVector3();
                Vector3 end = perimeter.points[(i + 1) % perimeter.points.Count].ToVector3();

                Vector3 closestPoint = GetClosestPointOnLine(start, end, mousePosition);
                float distance = Vector3.Distance(mousePosition, closestPoint);

                if (distance < closestDistance)
                {

                    // closestDistance = distance;
                    closestSideIndex = i;
                }
            }
        }
        if (closestSideIndex != -1)
        {
            // Highlight the closest side
            ResetSideHighlight();
            HighlightSide(closestSideIndex);
        }
        else
        {
            // Reset highlight if no side is close enough
            ResetSideHighlight();
        }

        return closestSideIndex;
    }

    // Method to highlight a side
    private void HighlightSide(int sideIndex)
    {
        if (highlightedSideIndex != -1)
        {
            // Reset the previous side's color
            sideLineRenderers[highlightedSideIndex].startColor = normalColorSide;
            sideLineRenderers[highlightedSideIndex].endColor = normalColorSide;
        }

        // Set the new side's color
        sideLineRenderers[sideIndex].startColor = highlightedColorSide;
        sideLineRenderers[sideIndex].endColor = highlightedColorSide;
        highlightedSideIndex = sideIndex;
        Debug.Log("Highlighting side index: " + sideIndex);
    }

    //get the snapped gate
    public (Vector3, string, String) GetSnappedGate(Vector3 mousePosition)
    {
        // This function should return the closest gate, road vertex, or road edge to the mouse position and highlight it
        Vector3 snappedPoint = mousePosition;
        float closestDistance = 20f;
        String nodeId = Guid.Empty.ToString();
        string pointType = "none";
        bool isVertex = false;

        // Check for the closest gate
        LineRenderer closestGate = null;
        for (int i = 0; i < gates.Count; i++)
        {
            Vector3 start = gates[i].GetComponent<LineRenderer>().GetPosition(0);
            Vector3 end = gates[i].GetComponent<LineRenderer>().GetPosition(1);

            Vector3 closestPoint = GetClosestPointOnLine(start, end, mousePosition);
            float distance = Vector3.Distance(mousePosition, closestPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGate = gates[i].GetComponent<LineRenderer>();
                // Get the middle point from the start and end of the gate
                snappedPoint = (start + end) / 2.0f;
                pointType = "gate";
                isVertex = false;
            }
            else
            {
                // Reset the color of the previously highlighted gate
                gates[i].GetComponent<LineRenderer>().startColor = Color.white;
                gates[i].GetComponent<LineRenderer>().endColor = Color.white;
            }
        }

        // Check for the closest road vertex
        foreach (var roadNode in roadSystem.Nodes)
        {
            Vector3 vertex = roadNode.Position.ToVector3();
            float distance = Vector3.Distance(mousePosition, vertex);

            if (distance < 40f)
            {
                nodeId = roadNode.Id;
                closestDistance = distance;
                snappedPoint = vertex;
                pointType = "vertex";
                isVertex = true;
            }
        }

        // Check for the closest point on road edges if no close vertex is found
        if (!isVertex)
        {
            foreach (var roadEdge in roadSystem.Edges)
            {
                Vector3 start = roadSystem.Nodes.Find(node => node.Id == roadEdge.StartNode).Position.ToVector3();
                Vector3 end = roadSystem.Nodes.Find(node => node.Id == roadEdge.EndNode).Position.ToVector3();

                Vector3 closestPoint = GetClosestPointOnLine(start, end, mousePosition);
                float distance = Vector3.Distance(mousePosition, closestPoint);

                if (distance < closestDistance)
                {
                    nodeId = roadEdge.Id;
                    closestDistance = distance;
                    snappedPoint = closestPoint;
                    pointType = "edge";
                    isVertex = false;
                }
            }
        }

        // Highlight the closest gate, road vertex, or road edge
        if (closestDistance < 20f)
        {
            HighlightSnapPoint(snappedPoint, isVertex ? Color.yellow : Color.magenta);
            if (closestGate != null)
            {
                closestGate.startColor = Color.green;
                closestGate.endColor = Color.green;
            }
        }
        else
        {
            ResetHighlight();
        }

        return (snappedPoint, pointType, nodeId);
    }

    //set the roads
    public void SetRoadSystem(RoadSystem roadSystem)
    {
        this.roadSystem = roadSystem;
        Debug.Log("SnapManager: Road system updated with " + roadSystem.Nodes.Count + " nodes and " + roadSystem.Edges.Count + " edges.");
    }


    // Find the closest point on a line segment from 'start' to 'end'
    /**
     * @brief Finds the closest point on a line segment from 'start' to 'end'.
     * @param start The start point of the line segment.
     * @param end The end point of the line segment.
     * @param point The point to find the closest point to.
     * @return The closest point on the line segment.
     */
    private Vector3 GetClosestPointOnLine(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 lineDirection = end - start;
        float lineLengthSquared = lineDirection.sqrMagnitude;

        if (lineLengthSquared == 0) return start; // If start and end are the same point

        // Project point onto the line segment, and clamp to the segment bounds
        float t = Vector3.Dot(point - start, lineDirection) / lineLengthSquared;
        t = Mathf.Clamp01(t);

        return start + t * lineDirection;
    }

    /**
     * @brief Activates the highlight object at the snap point.
     * @param position The position to highlight.
     */
    private void HighlightSnapPoint(Vector3 position, Color color)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(true);
            highlightObject.transform.position = position;
            highlightObject.GetComponent<Renderer>().material.color = color;
        }
    }

    /**
     * @brief Resets the highlight object and clears snap points.
     */
    private void ResetHighlight()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        highlightedVertex = null;
        highlightedSidePoint = null;

    }

    // Reset the highlight of the side
    private void ResetSideHighlight()
    {
        if (highlightedSideIndex != -1)
        {
            // Reset the color of the previously highlighted side
            sideLineRenderers[highlightedSideIndex].startColor = normalColorSide;
            sideLineRenderers[highlightedSideIndex].endColor = normalColorSide;
            highlightedSideIndex = -1;
        }
    }


    /**
     * @brief Clears the main perimeter data.
     */
    public void ClearMainPerimeter()
    {
        perimeter = null;
        Debug.Log("SnapManager: Main perimeter cleared.");
    }

    public void SetGates(List<GameObject> gates)
    {
        this.gates = gates;
        Debug.Log("SnapManager: Gates updated with " + gates.Count + " gates.");
    }

    internal void SetRoadCreationActive(bool v)
    {
        isRoadCreationActive = v;



    }
}
