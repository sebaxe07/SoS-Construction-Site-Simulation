using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RendererManager : MonoBehaviour
{
    public Material lineMaterial;  // Assign this material in the inspector

    /**
     * @brief Renders the perimeter of a zone using a LineRenderer.
     * 
     * @param zoneVertices List of vertices defining the zone perimeter.
     * @param lineMaterial Material to be used for the LineRenderer.
     * @return The created LineRenderer for the zone perimeter.
     */
    public GameObject RenderZonePerimeter(PolygonData zone, int zoneIndex)
    {
        float elevationOffset = 6f; // Set the y-coordinate for the zone perimeter
        // Create a new LineRenderer for each zone perimeter
        if (GameObject.Find("Zone Perimeter " + zoneIndex) != null)
        {
            Destroy(GameObject.Find("Zone Perimeter " + zoneIndex));
        }
        GameObject zoneObject = new GameObject("Zone Perimeter " + zoneIndex);

        //create a lineRenderer fo reach side of the zone

        for (int i = 0; i < zone.Count; i++)
        {

            GameObject sideObj = new GameObject("Side" + i);
            LineRenderer lr = sideObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default")); // Use an appropriate shader
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.startWidth = 10f;
            lr.endWidth = 10f;
            //elevate the line renderer slightly above the ground

            Vector3 start = new Vector3(zone.points[i].x, zone.points[i].y + elevationOffset, zone.points[i].z);
            Vector3 end = new Vector3(zone.points[(i + 1) % zone.Count].x, zone.points[(i + 1) % zone.Count].y + elevationOffset, zone.points[(i + 1) % zone.Count].z);
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            sideObj.transform.SetParent(zoneObject.transform);

        }




        return zoneObject;
    }

    public GameObject RenderGates(PolygonData zone, int zoneIndex)
    {
        GameObject zoneObject = GameObject.Find("Zone Perimeter " + zoneIndex);
        for (int i = 0; i < zone.Count; i++)
        {
            GameObject sideObj = zoneObject.transform.Find("Side" + i).gameObject;
            if (zone.GetGatesIndexes().Contains(i))
            {
                //destroy the existing gate objects
                foreach (Transform child in sideObj.transform)
                {
                    Destroy(child.gameObject);
                }

                GameObject gateObj = new GameObject("Gate: " + zoneIndex + " " + i);
                LineRenderer gateLR = gateObj.AddComponent<LineRenderer>();
                gateLR.material = new Material(Shader.Find("Sprites/Default")); // Use an appropriate shader
                gateLR.startColor = Color.white;
                gateLR.endColor = Color.white;
                gateLR.startWidth = 10f;
                gateLR.endWidth = 10f;

                Vector3 start = new Vector3(zone.points[i].x, zone.points[i].y + 5, zone.points[i].z);
                Vector3 end = new Vector3(zone.points[(i + 1) % zone.Count].x, zone.points[(i + 1) % zone.Count].y + 5, zone.points[(i + 1) % zone.Count].z);
                var middlePoint = new Vector3((start.x + end.x) / 2, ((start.y + end.y) / 2) + 5, (start.z + end.z) / 2);
                // Calculate the smaller line's start and end points
                Vector3 gateStart = Vector3.Lerp(start, middlePoint, 0.5f);
                Vector3 gateEnd = Vector3.Lerp(middlePoint, end, 0.5f);
                gateLR.positionCount = 2;
                gateLR.SetPosition(0, gateStart);
                gateLR.SetPosition(1, gateEnd);
                gateObj.transform.SetParent(sideObj.transform);
            }
        }
        return zoneObject;
    }

    public void RenderPerimeter(LineRenderer lineRenderer, PolygonData perimeter)
    {
        List<Vector3> renderPoints = perimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));

        // Add the first point at the end of the list just for rendering purposes
        if (renderPoints.Count > 1)
        {
            renderPoints.Add(renderPoints[0]);
        }

        if (lineMaterial == null)
        {
            Debug.LogError("Line Material is not set. Please assign a material in the inspector or ensure it's loaded properly.");
            return; // Exit if no material to prevent NullReferenceException
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


}
