using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
*   This script manages the creation of zones within the construction site perimeter.
*/
public class ZoneManager : MonoBehaviour
{
    // State management
    private bool isCreatingZone = false;

    // Perimeter vertices and selected zone points
    private PolygonData mainPerimeter;

    private List<Vector3> zonePoints = new List<Vector3>();
    private List<Vector3> zonePerimeter = new List<Vector3>();

    /// @brief Container for zone details UI elements.
    public GameObject zoneDetailsContainer;
    public GameObject zoneDetailsPrefab;
    public Button createZoneButton;
    // Reference to ConstructionSiteManager to end zone creation
    public ConstructionSiteManager constructionSiteManager;
    public PerimeterManager perimeterManager;
    public SnapManager snapManager;
    public Button createLastZoneButton;// Reference to the "Create New Zone" button
    private List<PolygonData> savedZones = new List<PolygonData>();
    private List<GameObject> zoneObjects = new List<GameObject>();
    public Material lineMaterial;

    public GameObject vertexMarkerPrefab; // Prefab for highlighting vertices (e.g., a small sphere)
    private List<GameObject> activeMarkers = new List<GameObject>(); // Track active markers

    private bool createLastZoneButtonClicked = false;

    /// @brief Initializes the ZoneManager.
    void Start()
    {
        // Other initialization code...

        if (createLastZoneButton != null)
        {
            createLastZoneButton.onClick.AddListener(OnCreateLastZoneButtonClick);
            createLastZoneButton.gameObject.SetActive(false);
        }
    }

    /// @brief Highlights the vertices of the created zone.
    private void HighlightZoneVertices()
    {
        // Clear any existing markers
        ClearVertexMarkers();

        // Create markers for each vertex in the zone perimeter
        foreach (Vector3 vertex in zonePerimeter)
        {
            GameObject marker = Instantiate(vertexMarkerPrefab, vertex, Quaternion.identity);
            activeMarkers.Add(marker);
        }

        Debug.Log("Highlighted " + zonePerimeter.Count + " vertices.");
    }

    /// @brief Clears all active vertex markers.
    private void ClearVertexMarkers()
    {
        foreach (GameObject marker in activeMarkers)
        {
            Destroy(marker);
        }
        activeMarkers.Clear();
    }

    /// @brief Sets the main perimeter vertices from the PerimeterManager.
    /// @param mainPerimeter The main perimeter data.
    public void SetMainPerimeterVertices(PolygonData mainPerimeter)
    {
        this.mainPerimeter = mainPerimeter;
        Debug.Log("ZoneManager: Main perimeter updated with " + mainPerimeter.points.Count + " points.");
    }

    /// @brief Checks if the perimeter vertices are set.
    /// @returns True if the perimeter vertices are set, otherwise false.
    public bool PerimeterVerticesSet()
    {
        return mainPerimeter != null && mainPerimeter.points != null && mainPerimeter.points.Count > 0;
    }

    /// @brief Begins the zone creation process.
    public void BeginZoneCreation()
    {
        isCreatingZone = true;
        zonePoints.Clear();
        zonePerimeter.Clear();
        Debug.Log("Zone creation started. Select points within the perimeter.");

        if (createLastZoneButton != null)
        {
            createLastZoneButton.gameObject.SetActive(true);
        }
    }

    /// @brief Ends the zone creation process.
    public void EndZoneCreation()
    {
        isCreatingZone = false;

        zonePoints.Clear();
        zonePerimeter.Clear();
        ClearVertexMarkers();
        constructionSiteManager.EndZoneCreation();
        Debug.Log("Zone creation ended.");

        // Hide the createLastZoneButton
        if (createLastZoneButton != null)
        {
            createLastZoneButton.gameObject.SetActive(false);
        }
    }

    /// @brief Adds a point to the zone and validates it.
    /// @param point The point to add.
    public void AddZonePoint(Vector3 point)
    {
        if (!isCreatingZone)
            return;

        Debug.Log("Zone point added: " + point);
        zonePoints.Add(point);
        HighlightSelectedPoint(point); // Highlight each point when added
        Debug.Log("Zone points: " + zonePoints.Count);

        if (zonePoints.Count == 1)
        {
            // Check if the first point is on the perimeter
            if (!PolygonUtils.IsPointOnPerimeter(point, mainPerimeter))
            {
                Debug.LogWarning("ZoneManager: First point is not on the perimeter.");
                zonePoints.RemoveAt(0);
                RemoveLastMarker();
            }


        }
        else
        {
            Debug.Log("Checking if the point is on the perimeter.");
            if (PolygonUtils.IsPointOnPerimeter(point, mainPerimeter) && zonePoints.Count > 1)
            {
                Debug.Log("Another point on the perimeter found. Creating zone with these points.");
                CreateComplexZone();
                if (validZonePerimeter())
                {
                    SaveZone();
                    mainPerimeter = perimeterManager.GenerateUpdatedMainPerimeter(zonePoints);
                    RenderZonePerimeter(savedZones.Count - 1);
                }
                EndZoneCreation();
            }
            else if (!PolygonUtils.IsPointInPerimeter(point, mainPerimeter))
            {
                Debug.Log("Point is outside the perimeter. Removing the last point.");
                zonePoints.RemoveAt(zonePoints.Count - 1);
                RemoveLastMarker();
            }
        }
    }

    public void AddGateToZone(int closestSideIndex, int zoneIndex)
    {
        // Add gate to the zone
        Debug.Log("Gate added to zone " + zoneIndex + " at side " + closestSideIndex);
        savedZones[zoneIndex].AddGateToZone(closestSideIndex, (closestSideIndex + 1) % savedZones[zoneIndex].Count);
        RendererManager rendererManager = FindObjectOfType<RendererManager>();
        zoneObjects[zoneIndex] = rendererManager.RenderGates(savedZones[zoneIndex], zoneIndex);
        Debug.Log("Zone " + zoneIndex + " has " + savedZones[zoneIndex].gates.Count + " gates.");
        constructionSiteManager.EndGateCreation();
    }


    /// @brief Validates the zone perimeter.
    /// @returns True if the zone perimeter is valid, otherwise false.
    private bool validZonePerimeter()
    {
        //Check if the perimeter doesn't have any sharp angles
        for (int i = 0; i < zonePerimeter.Count; i++)
        {
            Vector3 previous = zonePerimeter[(i - 1 + zonePerimeter.Count) % zonePerimeter.Count];
            Vector3 current = zonePerimeter[i];
            Vector3 next = zonePerimeter[(i + 1) % zonePerimeter.Count];
            if (!PolygonUtils.IsAngleValid(previous, current, next, 30, 325))
            {
                Debug.LogWarning("ZoneManager: Invalid zone perimeter. Sharp angle detected.");
                return false;
            }
        }
        //Check if the perimeter is self-intersecting
        if (PolygonUtils.SelfIntersecting(zonePerimeter))
        {
            Debug.LogWarning("ZoneManager: Invalid zone perimeter. Self-intersecting perimeter detected.");
            return false;
        }

        //Check if the perimeter doesn't intersect with another zone    
        // foreach (PolygonData zone in savedZones)
        // {
        //     if (PolygonUtils.PolygonsIntersect(zonePerimeter, zone.points.ConvertAll(point => new Vector3(point.x, point.y, point.z))))
        //     {
        //         Debug.LogWarning("ZoneManager: Invalid zone perimeter. Intersecting with another zone.");
        //         return false;
        //     }
        // }

        return true;
    }

    /// @brief Creates a complex zone from the selected points.
    private void CreateComplexZone()
    {
        zonePerimeter.AddRange(zonePoints);
        if (zonePerimeter.Count < 2)
        {
            Debug.LogError("Not enough points to create a zone. A zone requires at least 3 points.");
            return;
        }

        // Add the mainPerimeter's vertices to zonePerimeter
        List<Vector3> pointsToAdd = new List<Vector3>();
        List<Vector3> mainPerimeterPoints = mainPerimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));

        int firstZoneIndex = PolygonUtils.FindClosestSegmentIndex(mainPerimeterPoints, zonePerimeter[0]);
        int lastZoneIndex = PolygonUtils.FindClosestSegmentIndex(mainPerimeterPoints, zonePerimeter[zonePerimeter.Count - 1]);
        if (firstZoneIndex != lastZoneIndex)
        {
            for (int i = (firstZoneIndex + 1) % mainPerimeterPoints.Count; i != ((lastZoneIndex + 1) % mainPerimeter.Count); i = (i + 1) % mainPerimeterPoints.Count)
            {
                pointsToAdd.Add(mainPerimeterPoints[i]);
            }
            //Add the point to the zonePerimeter list in reverse order
            pointsToAdd.Reverse();
            zonePerimeter.AddRange(pointsToAdd);
        }
        // Ensure the last point is not the same as the first point
        if (zonePerimeter[zonePerimeter.Count - 1] == zonePerimeter[0])
        {
            Debug.Log("Removing the last point.");
            zonePerimeter.RemoveAt(zonePerimeter.Count - 1);
        }
    }

    /// @brief Saves the zone perimeter as a PolygonData object.
    private void SaveZone()
    {
        PolygonData zoneData = new PolygonData(zonePerimeter);
        string zoneId = Guid.NewGuid().ToString();
        zoneData.ZoneConfigTopic = "/companies/0/sites/1/zones/" + zoneId + "/config";
        zoneData.ZoneStatusTopic = "/companies/0/sites/1/zones/" + zoneId + "/status";
        savedZones.Add(zoneData);
        Debug.Log("Zone saved with " + zonePerimeter.Count + " points.");
        HighlightZoneVertices();

        if (zoneDetailsPrefab != null && zoneDetailsContainer != null)
        {
            // foreach (Transform child in zoneDetailsContainer.transform)
            // {
            //     Destroy(child.gameObject);
            // }

            PolygonData zone = zoneData;
            GameObject zoneDetailsInstance = Instantiate(zoneDetailsPrefab, zoneDetailsContainer.transform);
            zoneDetailsInstance.name = "ZoneDetails " + (savedZones.Count - 1);
            ZoneDetails zoneDetails = zoneDetailsInstance.GetComponent<ZoneDetails>();
            if (zoneDetails != null)
            {
                zoneDetails.SetZoneData(zone);
                // Here you can set more properties on zoneDetails as needed
                zoneDetails.SetZoneManager(this);
                zoneDetails.SetZoneIndex(savedZones.Count - 1); // Example of setting additional properties
                zoneDetails.SetZoneName("A" + (savedZones.Count - 1));
            }
            else
            {
                Debug.LogError("ZoneDetails component not found on the instantiated prefab.");
            }


        }
    }

    public void ChangeZoneColor(int zoneIndex, Color color)
    {
        if (zoneIndex < 0 || zoneIndex >= zoneObjects.Count)
        {
            Debug.LogWarning("ZoneManager: Invalid zone index.");
            return;
        }

        GameObject zoneObejct = zoneObjects[zoneIndex];
        if (zoneObejct != null)
        {
            foreach (Transform child in zoneObejct.transform)
            {
                LineRenderer lr = child.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startColor = color;
                    lr.endColor = color;
                }
            }
        }

    }



    // Render the zone perimeter using a new LineRenderer each time
    public void RenderZonePerimeter(int zoneIndex)
    {
        // Use RendererManager to render the perimeter
        RendererManager rendererManager = FindObjectOfType<RendererManager>();

        PolygonData polygonData = savedZones[zoneIndex];
        GameObject newRenderer = rendererManager.RenderZonePerimeter(polygonData, zoneIndex);
        zoneObjects.Add(newRenderer);
    }

    public List<PolygonData> GetZones()
    {
        return savedZones;
    }
    /// @brief Gets the vertices of the saved zone perimeters.
    /// @returns A list of saved zone perimeters.
    public List<PolygonData> GetZonePerimeterVertices()
    {
        // for (int i = 0; i < savedZones.Count; i++)
        // {
        //     Debug.Log(zoneDetailsContainer.transform.GetChild(i).GetComponent<ZoneDetails>().zoneNameInputField.text);
        //     savedZones[i].Name = zoneDetailsContainer.transform.GetChild(i).GetComponent<ZoneDetails>().zoneNameInputField.text;
        //     savedZones[i].Type = zoneDetailsContainer.transform.GetChild(i).GetComponent<ZoneDetails>().zoneTypeDropdown.options[zoneDetailsContainer.transform.GetChild(i).GetComponent<ZoneDetails>().zoneTypeDropdown.value].text;
        // }
        return savedZones;
    }

    //return the saved zone at the given index
    public PolygonData GetZonePerimeter(int index)
    {
        if (index < 0 || index >= savedZones.Count)
        {
            Debug.LogWarning("ZoneManager: Invalid zone index.");
            return null;
        }
        return savedZones[index];
    }

    // Method to create a zone from the main perimeter
    /// @brief Creates a zone from the main perimeter.
    public void CreateLastZoneFromMainPerimeter()
    {
        if (mainPerimeter == null || mainPerimeter.points == null || mainPerimeter.points.Count == 0)
        {
            Debug.LogWarning("Main perimeter is not set or empty.");
            return;
        }

        zonePerimeter.Clear();
        foreach (var point in mainPerimeter.points)
        {
            zonePerimeter.Add(new Vector3(point.x, point.y, point.z));
        }

        if (validZonePerimeter())
        {
            SaveZone();
            //Update main Perimeter
            RenderZonePerimeter(savedZones.Count - 1);
            Debug.Log("Zone created from main perimeter.");
            mainPerimeter = null;
            perimeterManager.ClearMainPerimeter();
            snapManager.ClearMainPerimeter();
            // Hide the createLastZoneButton
            if (createLastZoneButton != null && createZoneButton != null)
            {
                createZoneButton.gameObject.SetActive(false);
                createLastZoneButton.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Invalid zone perimeter.");
        }
        EndZoneCreation();

    }

    /// @brief Handles the click event for the "Create Last Zone" button.
    private void OnCreateLastZoneButtonClick()
    {
        createLastZoneButtonClicked = true;
        CreateLastZoneFromMainPerimeter();
    }

    /// @brief Checks if the "Create Last Zone" button was clicked.
    /// @returns True if the button was clicked, otherwise false.
    public bool IsCreateLastZoneButtonClicked()
    {
        return createLastZoneButtonClicked;
    }

    /// @brief Resets the state of the "Create Last Zone" button click.
    public void ResetCreateLastZoneButtonClicked()
    {
        createLastZoneButtonClicked = false;
    }

    private void HighlightSelectedPoint(Vector3 point)
    {
        if (vertexMarkerPrefab != null)
        {
            GameObject marker = Instantiate(vertexMarkerPrefab, point, Quaternion.identity);
            marker.GetComponent<Renderer>().material.color = Color.magenta;
            marker.SetActive(true);
            activeMarkers.Add(marker);
        }
    }

    private void RemoveLastMarker()
    {
        if (activeMarkers.Count > 0)
        {
            Destroy(activeMarkers[activeMarkers.Count - 1]);
            activeMarkers.RemoveAt(activeMarkers.Count - 1);
        }
    }

    public List<GameObject> GetGates()
    {
        List<GameObject> gates = new List<GameObject>();
        foreach (GameObject zone in zoneObjects)
        {
            foreach (Transform side in zone.transform)
            {
                foreach (Transform gate in side)
                {
                    gates.Add(gate.gameObject);
                }
            }
        }


        return gates;
    }
}