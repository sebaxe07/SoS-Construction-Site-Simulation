using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;

/**
 * @class ConstructionSiteManager
 * @brief Manages the construction site, including perimeter and zone creation, and saving data to JSON.
 */
public class ConstructionSiteManager : MonoBehaviour
{
    // References to perimeterManager
    public PerimeterManager perimeterManager;
    // References to snapManager
    public SnapManager snapManager;
    // References to zoneManager
    public ZoneManager zoneManager;
    // References to roadManager
    public RoadManager roadManager;
    // Reference to the camera
    public Camera cam;

    public Button createZoneButton;
    public Button createRoadButton;
    public TMP_InputField nameInputField;
    public TMP_InputField cityInputField;
    public TMP_InputField addressInputField;
    public Button saveButton;
    private ConstructionSiteData data;
    public SceneLoader SceneLoader;
    public GameObject ErrorScreen;
    public GameObject LoadingScreen;
    public Button errorButton;
    public SelectedSiteData selectedSiteData;

    private float perimeterArea;

    // State management
    private bool isZoneCreationActive = false;
    private bool isGateCreationActive = false;

    private bool isRoadCreationActive = false;

    private int zoneIndex = -1;

    /**
     * @brief Initializes the construction site and sets up UI interactions.
     */
    private void Start()
    {
        InitializeSite();
        SetupUI();
    }

    /**
     * @brief Initializes the construction site by loading and rendering the perimeter.
     */
    public void InitializeSite()
    {
        if (perimeterManager != null)
        {
            // Load and render the perimeter
            perimeterManager.InitializeMainPerimeter();

            // Manually set the perimeter data in ZoneManager and SnapManager
            var loadedPerimeter = perimeterManager.GetMainPerimeterVertices();
            perimeterArea = PolygonUtils.CalculateArea(loadedPerimeter.points.ConvertAll(p => p.ToVector3()));
            //transform the perimeter area in m^2 to km^2
            // perimeterArea = perimeterArea / 1000000;

            if (zoneManager != null)
            {
                zoneManager.SetMainPerimeterVertices(loadedPerimeter);
            }


        }

        Debug.Log("Construction site initialized.");
    }



    /**
     * @brief Sets up the UI by adding listeners to buttons.
     */
    private void SetupUI()
    {
        createRoadButton.onClick.AddListener(StartRoadCreation);
        createZoneButton.onClick.AddListener(StartZoneCreation);
        saveButton.onClick.AddListener(SaveDataToJson);
        errorButton.onClick.AddListener(() => ErrorScreen.SetActive(false));
    }

    /**
     * @brief Saves the construction site data to a JSON file.
     */
    private void SaveDataToJson()
    {
        // Show loading screen
        LoadingScreen.SetActive(true);

        // Retrieve main perimeter as a PolygonData object
        PolygonData mainPerimeterPoints = perimeterManager.GetOriginalMainPerimeter();

        // Retrieve all zone perimeters as a list of PolygonData
        List<PolygonData> zones = zoneManager.GetZones();

        // Capture input field values
        string name = nameInputField.text;
        string city = cityInputField.text;
        string address = addressInputField.text;

        // Validate input fields
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(city) || string.IsNullOrEmpty(address))
        {
            LoadingScreen.SetActive(false);
            ErrorScreen.SetActive(true);
            return;
        }

        // Validate main perimeter
        if (mainPerimeterPoints.points.Count < 3)
        {
            LoadingScreen.SetActive(false);
            ErrorScreen.SetActive(true);
            return;
        }

        // Validate zones
        if (zones.Count == 0)
        {
            LoadingScreen.SetActive(false);
            ErrorScreen.SetActive(true);
            return;
        }

        // Create the data object with all the required information
        data = new ConstructionSiteData(name, city, address, mainPerimeterPoints, zones);
        // ConstructionSiteData data = new ConstructionSiteData("SimCom", "Stockholm", "Slakthusgatan 6, 121 62 Johanneshov, Sweden", mainPerimeterPoints, zonePerimeterPoints);

        // Serialize to JSON
        string json = JsonUtility.ToJson(data, true);

        // Specify file path
        string filePath = Application.dataPath + "/SavedConstructionSiteData.json";

        // Write to file
        File.WriteAllText(filePath, json);

        Debug.Log("Data saved to " + filePath);

        MappingToCSDI();


        // Load the next scene
        LoadingScreen.SetActive(false);
        SceneLoader.LoadScene("OpenConstructionSite");
    }

    private void MappingToCSDI()
    {
        ConstructionSiteDataInfo csdi = new ConstructionSiteDataInfo();
        // Check if there exist a siteData.json file
        string filePath = Path.Combine(Application.persistentDataPath, "siteData.json");
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            csdi = JsonUtility.FromJson<ConstructionSiteDataInfo>(jsonText);
            Debug.Log("File loaded successfully.");
        }
        else
        {
            Debug.LogError($"File not found at: {filePath}");
            // Populate the ConstructionSiteDataInfo object with ConstructionSiteData objects
            csdi.ConstructionSites = new List<ConstructionSite>();
        }

        // float area = PolygonUtils.CalculateAreaOfPolygon(data.mainPerimeter);

        // Populate the ConstructionSite object with data
        ConstructionSite site = new()
        {
            Name = data.inputField1,
            Address = data.inputField3,
            City = data.inputField2,
            State = "",
            Zip = "",
            Phone = "",
            Dimensions = perimeterArea.ToString() + " m^2",
            SiteConfigTopic = "/companies/0/sites/1/config",
            SiteStatusTopic = "/companies/0/sites/1/status",
            IsConfigured = false,
            Layout = new Layout
            {
                NumberOfVertices = data.mainPerimeter.Count,
                Vertices = data.mainPerimeter.points.ConvertAll(p => Vertex.FromVector3(p.ToVector3())),
                Zones = data.zonePerimeters.ConvertAll(z => new Zone
                {
                    ZoneConfigTopic = z.ZoneConfigTopic,
                    ZoneStatusTopic = z.ZoneStatusTopic,
                    Name = z.Name,
                    Type = z.Type,
                    Vertices = z.points.ConvertAll(p => Vertex.FromVector3(p.ToVector3())),
                    Gates = z.gates
                })
            }
        };

        // Retrieve the road system from RoadManager
        RoadSystem roadSystem = roadManager.GetRoadSystem();

        // Map the road system data to the ConstructionSite object
        site.RoadSystem = new RoadSystem
        {
            Nodes = roadSystem.Nodes.ConvertAll(n => new RoadNode
            {
                Id = n.Id,
                Position = n.Position,
                ConnectedEdges = n.ConnectedEdges
            }),
            Edges = roadSystem.Edges.ConvertAll(e => new RoadEdge
            {
                Id = e.Id,
                StartNode = e.StartNode,
                EndNode = e.EndNode,
                Width = 7,
                Length = e.Length,
                // Lanes does not exist on e so we need to create it here with both direction as default
                Lanes = new List<Lane> { new() { Id = Guid.NewGuid().ToString(), Direction = 1 }, new() { Id = Guid.NewGuid().ToString(), Direction = -1 } }
            })
        };

        // Add the mapped site to the ConstructionSites list
        csdi.ConstructionSites.Add(site);

        // Serialize to JSON
        string json = JsonUtility.ToJson(csdi, true);

        // Specify file path
        string filePathSave = Path.Combine(Application.persistentDataPath, "siteData.json");

        // Write to file
        File.WriteAllText(filePathSave, json);

        // Set the selected site to the index of the new site
        selectedSiteData.siteIndex = csdi.ConstructionSites.Count - 1;
        selectedSiteData.siteName = site.Name;
        selectedSiteData.siteName = "";


        Debug.Log("Mapped data saved to " + filePathSave);
    }

    /**
     * @brief Starts the zone creation process.
     */
    private void StartZoneCreation()
    {
        if (!isZoneCreationActive)
        {
            isZoneCreationActive = true;
            zoneManager.BeginZoneCreation();

            if (snapManager != null)
            {
                snapManager.SetMainPerimeterVertices(perimeterManager.GetMainPerimeterVertices());
            }
            //change the text of the button
            createZoneButton.GetComponentInChildren<TMP_Text>().text = "End Zone Creation";
            //hide the road creation button and show last zone creation button
            createRoadButton.gameObject.SetActive(false);


            Debug.Log("Zone creation started. Select points within the perimeter.");
        }
        else
        {
            isZoneCreationActive = false;
            zoneManager.EndZoneCreation();
            createRoadButton.gameObject.SetActive(true);
            //change the text of the button
            createZoneButton.GetComponentInChildren<TMP_Text>().text = "Start Zone Creation";
            Debug.Log("Zone creation ended.");
        }

    }

    /**
     * @brief Starts the road creation process.
     */
    private void StartRoadCreation()
    {
        if (!isRoadCreationActive)
        {
            //change the text of the button
            createRoadButton.GetComponentInChildren<TMP_Text>().text = "End Road Creation";
            isRoadCreationActive = true;
            roadManager.setRoadCreationActive(true);
            snapManager.SetGates(zoneManager.GetGates());
            snapManager.SetRoadSystem(roadManager.GetRoadSystem());
            snapManager.SetRoadCreationActive(true);
            Debug.Log("Road creation started. Select points within the perimeter.");
        }
        else
        {
            //change the text of the button
            createRoadButton.GetComponentInChildren<TMP_Text>().text = "Start Road Creation";
            isRoadCreationActive = false;
            roadManager.setRoadCreationActive(false);
            snapManager.SetRoadCreationActive(false);
            Debug.Log("Road creation ended.");
        }
    }

    public void HandleGateSelection(int zoneIndex)
    {
        isGateCreationActive = true;
        this.zoneIndex = zoneIndex;
        snapManager.SetGateCreationActive(true, zoneIndex);
        snapManager.SetMainPerimeterVertices(zoneManager.GetZonePerimeter(zoneIndex));
        Debug.Log("Gate creation started. Select a side to add a gate.");
    }



    /**
     * @brief Ends the zone creation process.
     */
    public void EndZoneCreation()
    {
        isZoneCreationActive = false;
        createRoadButton.gameObject.SetActive(true);
        //change the text of the button
        createZoneButton.GetComponentInChildren<TMP_Text>().text = "Start Zone Creation";
        Debug.Log("Zone creation ended.");
    }

    /**
     * @brief Updates the zone creation process based on user input.
     */
    private void Update()
    {
        if (zoneManager.PerimeterVerticesSet())
        {
            if (isZoneCreationActive && !zoneManager.IsCreateLastZoneButtonClicked())
            {

                // Cast a ray from the camera through the mouse position to get the world position
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Vector3 mousePosition = hit.point;

                    // Pass the mouse position to SnapManager for continuous snapping feedback
                    snapManager.GetSnappedPoint(mousePosition);
                    // On mouse click, confirm the snap point and highlight it permanently
                    if (Input.GetMouseButtonDown(0))
                    {
                        Debug.Log("Mouse clicked.");
                        Vector3 snappedPoint = snapManager.GetSnappedPoint(mousePosition);
                        // Add the snapped point to the zone manager for zone creation
                        zoneManager.AddZonePoint(snappedPoint);
                    }
                }
            }
            else if (zoneManager.IsCreateLastZoneButtonClicked())
            {
                zoneManager.ResetCreateLastZoneButtonClicked();
            }
        }
        if (isGateCreationActive)
        {

            // Cast a ray from the camera to the mouse position
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 mousePosition = hit.point;

                // Use SnapManager to find the closest side

                int closestSideIndex = snapManager.GetSnappedSide(mousePosition);

                if (Input.GetMouseButtonDown(0) && closestSideIndex != -1)
                {
                    zoneManager.AddGateToZone(closestSideIndex, zoneIndex);


                }
            }
        }
        if (isRoadCreationActive)
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 mousePosition = hit.point;


                // Use SnapManager to find the closest side

                (Vector3 closestGate, string type, String nodeId) = snapManager.GetSnappedGate(mousePosition);

                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("Mouse clicked.");
                    roadManager.AddRoadPoint(closestGate, type, nodeId);
                }


            }
        }

    }


    internal void EndGateCreation()
    {
        isGateCreationActive = false;
        snapManager.SetGateCreationActive(false, -1);
        zoneManager.ChangeZoneColor(zoneIndex, Color.red);

        Debug.Log("Gate creation ended.");
    }




}