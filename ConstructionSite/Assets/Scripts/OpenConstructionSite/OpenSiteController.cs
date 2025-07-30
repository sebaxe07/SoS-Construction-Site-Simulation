using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // Per IEnumerator
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json; // Make sure to include this namespace
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class OpenSiteController : MonoBehaviour
{

    public Camera cam;
    public LineRenderer lineRenderer;
    public Material lineMaterial;
    public Terrain mapTerrain;
    public string jsonFilePath;
    public Canvas mainCanvas;
    // Card prefab for zone information display
    public GameObject zoneCard;
    // Sidebar content panel
    public Transform sidebarPanel;
    public Button configLiveSite;
    private bool buttonDisabled = false;
    // The construction site data
    private ConstructionSiteLoader constructionSite;
    private ConstructionSiteDataInfo constructionSiteData;
    public SelectedSiteData selectedSiteData;
    public ConstructionSite selectedSite;
    // The main perimeter of the construction site
    private Layout mainPerimeter;

    private List<LineRenderer> zoneRenderers = new List<LineRenderer>();

    // Initialize the dictionary
    private Dictionary<string, Color> colorDictionary = new Dictionary<string, Color>
        {
            { "Demolition", new Color(0.588f, 0.569f, 0.565f, 1f) },
            { "Concrete", new Color(0.569f, 0.867f, 0.918f, 1f) },
            { "Excavation", new Color(0.851f, 0.451f, 0.792f, 1f) },
            { "Building", new Color(0.918f, 0.808f, 0.373f, 1f) }
        };

    // MQTT Variables
    private MQTTManager mqttManager = new MQTTManager();
    private int numACKReceived = 0;
    private const float UPDATE_RATE = 0.1f;
    private Queue<MQTTMessage> messageQueue = new Queue<MQTTMessage>();
    private Dictionary<string, GameObject> activePrefabs = new Dictionary<string, GameObject>();
    public GameObject prefabExcavator;
    public GameObject prefabWheelLoader;
    public GameObject prefabTruck;


    void Start()
    {
        // Initialize the construction site perimeter
        InitializeMainPerimeter();
        PopulateSidebar();
        TaskManagement.TaskManager.ClearAllTasks();
        SimulationData.Instance.ZoneMachines.Clear();
        StartCoroutine(UpdateMachinePositionsUI());
    }

    void Update()
    {

    }

    // Draw the perimeter of the site and the zones within the site
    public void InitializeMainPerimeter()
    {
        // Load the construction site data from JSON, you can use this for testing purposes so you don't have to go through view construction sites
        //constructionSite = ConstructionSiteLoader.LoadFromJson(jsonFilePath);

        // Load all of the construction sites from json
        LoadJsonData();

        // Load the active construction site
        selectedSite = null;

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
            Debug.Log(selectedSite.Name);
        }
        else
        {
            Debug.LogError("Selected site not found.");
        }

        constructionSite = ActiveSiteManager.Instance.CurrentSite;

        if (constructionSite == null)
        {
            Debug.LogError("No ConstructionSiteLoader found in ActiveSiteManager!");
        }

        mainPerimeter = selectedSite.Layout;
        if (mainPerimeter == null || mainPerimeter.Vertices == null || mainPerimeter.Vertices.Count == 0)
        {
            Debug.LogError("Failed to load perimeter data from JSON file.");
            return;
        }
        // Print out construction site with all it's parameters and lists
        //Debug.Log(jsonFilePath);
        //Debug.Log(constructionSite.ToString());
        //Debug.Log(mainPerimeter);
        RenderPerimeter();
        RenderZones();
        RenderRoads();

    }

    private void LoadJsonData()
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

    // Draw the main perimeter of the site
    void RenderPerimeter()
    {
        // List<Vector3> renderPoints = mainPerimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));
        List<Vector3> renderPoints = mainPerimeter.Vertices.ConvertAll(vertex => vertex.ToVector3());

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

    // Accessor for main perimeter vertices as PolygonData
    // public PolygonData GetMainPerimeterVertices()
    // {
    //     return mainPerimeter;
    // }

    //public void PopulateSidebar()
    //{
    //    // Enable Realtime site configuration button
    //    // if (constructionSite.IsConfigured)
    //    // {
    //    //     // Hide button 
    //    // }

    //    foreach (Zone zone in mainPerimeter.Zones)
    //    {
    //        // Instantiate the card prefab
    //        GameObject card = Instantiate(zoneCard, sidebarPanel);

    //        if (card == null)
    //        {
    //            Debug.LogError("Failed to instantiate zoneCard prefab.");
    //            return;
    //        }

    //        // Find the MainSection of the card
    //        Transform mainSection = card.transform.Find("MainSection");
    //        if (mainSection == null)
    //        {
    //            Debug.LogError("MainSection object not found in card prefab.");
    //            continue;
    //        }

    //        Transform expandedSection = card.transform.Find("ExpandedSection");
    //        if (expandedSection == null)
    //        {
    //            Debug.LogError("ExpandedSection object not found in card prefab.");
    //            continue;
    //        }

    //        // Fetching elements inside the card

    //        // Main section
    //        TMP_Text zoneNameText = mainSection.Find("ZoneName").GetComponent<TMP_Text>();
    //        TMP_Text taskTypeText = mainSection.Find("ZoneType").GetComponent<TMP_Text>();
    //        Image statusCircle = mainSection.Find("ColorStatus").GetComponent<Image>();

    //        // Expanded section
    //        TMP_Text zoneIdText = expandedSection.Find("ZoneID").GetComponent<TMP_Text>();
    //        TMP_Text zoneAreaText = expandedSection.Find("ZoneArea").GetComponent<TMP_Text>();
    //        TMP_Text zoneMachinesText = expandedSection.Find("NumberOfMachines").GetComponent<TMP_Text>();

    //        // Filling the elements with the zone data
    //        zoneNameText.text = zone.Name;
    //        taskTypeText.text = zone.Type;
    //        // if (zone.Active)
    //        // {
    //        //     statusCircle.color = Color.green;
    //        // }
    //        // else
    //        // {
    //        //     statusCircle.color = Color.red;
    //        // }
    //        // zoneIdText.text = $"Zone id: {zone.ZoneID}"; ;
    //        // zoneAreaText.text = $"Zone area: {zone.Size}";
    //        // zoneMachinesText.text = $"Number of machines: {zone.NumberOfMachines}";

    //        // Add an EventTrigger component 
    //        EventTrigger eventTrigger = mainSection.gameObject.AddComponent<EventTrigger>();

    //        // Create a new entry for the Pointer Click event
    //        EventTrigger.Entry entry = new EventTrigger.Entry
    //        {
    //            eventID = EventTriggerType.PointerClick
    //        };

    //        // Add a listener to toggle the expanded section
    //        entry.callback.AddListener((data) => { ToggleExpand(card); });

    //        // Add the entry to the EventTrigger
    //        eventTrigger.triggers.Add(entry);

    //        // Connect to RealTime Data
    //        // if (constructionSite.IsConfigured)
    //        // {
    //        //     // TODO: Subscribe to topic to get the data
    //        // }
    //    }
    //}

    public async void PopulateSidebar()
    {
        // Enable Realtime site configuration button
        if (selectedSite.IsConfigured)
        {
            Destroy(configLiveSite.gameObject);
            buttonDisabled = true;
        }
        foreach (Zone zone in mainPerimeter.Zones)
        {
            // Instantiate the card prefab
            GameObject card = Instantiate(zoneCard, sidebarPanel);

            if (card == null)
            {
                Debug.LogError("Failed to instantiate zoneCard prefab.");
                return;
            }

            // Find the MainSection and ExpandedSection
            Transform mainSection = card.transform.Find("MainSection");
            Transform expandedSection = card.transform.Find("ExpandedSection");

            if (mainSection == null || expandedSection == null)
            {
                Debug.LogError("MainSection or ExpandedSection not found in card prefab.");
                continue;
            }

            // Fetch UI elements inside the card
            TMP_Text zoneNameText = mainSection.Find("ZoneName").GetComponent<TMP_Text>();
            TMP_Text taskTypeText = mainSection.Find("ZoneType").GetComponent<TMP_Text>();
            TMP_Text zoneIdText = expandedSection.Find("ZoneID").GetComponent<TMP_Text>();
            TMP_Text zoneAreaText = expandedSection.Find("ZoneArea").GetComponent<TMP_Text>();
            TMP_Text zoneMachinesText = expandedSection.Find("NumberOfMachines").GetComponent<TMP_Text>();

            // Populate card data
            zoneNameText.text = zone.Name;
            taskTypeText.text = zone.Type;
            //zoneIdText.text = $"Zone ID: {zone.ZoneID}";
            //zoneAreaText.text = $"Zone Area: {zone.Size}";
            //zoneMachinesText.text = $"Number of Machines: {zone.NumberOfMachines}";

            // Add buttons to the ExpandedSection
            AddButtonsToExpandedSection(expandedSection, zone);

            // Add an EventTrigger to toggle the expanded section
            EventTrigger eventTrigger = mainSection.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => { ToggleExpand(card); });
            eventTrigger.triggers.Add(entry);
        }
        // Connect to RealTime Data
        if (selectedSite.IsConfigured)
        {
            var isConnected = await mqttManager.ConnectToBroker();
            if (!isConnected)
            {
                Debug.LogError("Could not connect to broker");
            }
            var isSubscribed = await mqttManager.SubscribeToTopic(selectedSite.SiteStatusTopic, HandleMessageReceived);
            if (!isSubscribed)
            {
                Debug.LogError("Could not subscribe to topic: " + selectedSite.SiteStatusTopic);
            }
            Debug.Log("Subscribed to topic: " + selectedSite.SiteStatusTopic);
            foreach (var zone in selectedSite.Layout.Zones)
            {
                isSubscribed = await mqttManager.SubscribeToTopic(zone.ZoneStatusTopic, HandleMessageReceived);
                if (!isSubscribed)
                {
                    Debug.LogError("Could not subscribe to topic: " + zone.ZoneStatusTopic);
                }
            }
        }
    }

    private void AddButtonsToExpandedSection(Transform expandedSection, Zone zone)
    {
        // Set the height of the ExpandedSection to 290
        RectTransform expandedSectionRT = expandedSection.GetComponent<RectTransform>();
        if (expandedSectionRT != null)
        {
            expandedSectionRT.sizeDelta = new Vector2(expandedSectionRT.sizeDelta.x, 290f);
        }
        // Configure Vertical Layout Group (Padding and Spacing)
        VerticalLayoutGroup layoutGroup = expandedSection.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = expandedSection.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.spacing = 5f; // Set spacing between elements
        float buttonWidth = 200f; // Width of each button
        float buttonHeight = 50f; // Height of each button
        float buttonSpacing = 5f; // Spacing between buttons

        // Create Button 1
        GameObject button1 = new GameObject("Button1");
        RectTransform rt1 = button1.AddComponent<RectTransform>();
        rt1.SetParent(expandedSection, false); // Attach to ExpandedSection
        rt1.sizeDelta = new Vector2(buttonWidth, buttonHeight); // Set button size
        rt1.anchoredPosition = new Vector2(0, -10f); // Position button at the top
        Button btn1 = button1.AddComponent<Button>();
        Image btn1Image = button1.AddComponent<Image>();
        btn1Image.color = new Color(0.75f, 0.75f, 0.75f); // Gray background color

        // Add Text to Button 1
        GameObject text1 = new GameObject("Text");
        RectTransform text1RT = text1.AddComponent<RectTransform>();
        text1RT.SetParent(button1.transform, false); // Attach to Button1
        TextMeshProUGUI btn1Text = text1.AddComponent<TextMeshProUGUI>();
        btn1Text.text = "Constellation";
        btn1Text.alignment = TextAlignmentOptions.Center;
        btn1Text.fontSize = 18;
        btn1Text.color = Color.black; // Black text color

        btn1.onClick.AddListener(() =>
        {
            Debug.Log($"Button 1 clicked for Zone: {zone.Name}");
            PerformAction1(zone);
        });

        // Create Button 2
        GameObject button2 = new GameObject("Button2");
        RectTransform rt2 = button2.AddComponent<RectTransform>();
        rt2.SetParent(expandedSection, false); // Attach to ExpandedSection
        rt2.sizeDelta = new Vector2(buttonWidth, buttonHeight); // Set button size
        rt2.anchoredPosition = new Vector2(0, -(buttonHeight + buttonSpacing)); // Position button below Button 1
        Button btn2 = button2.AddComponent<Button>();
        Image btn2Image = button2.AddComponent<Image>();
        btn2Image.color = new Color(0.75f, 0.75f, 0.75f); // Gray background color

        // Add Text to Button 2
        GameObject text2 = new GameObject("Text");
        RectTransform text2RT = text2.AddComponent<RectTransform>();
        text2RT.SetParent(button2.transform, false); // Attach to Button2
        TextMeshProUGUI btn2Text = text2.AddComponent<TextMeshProUGUI>();
        btn2Text.text = "Task Builder";
        btn2Text.alignment = TextAlignmentOptions.Center;
        btn2Text.fontSize = 18;
        btn2Text.color = Color.black; // Black text color

        btn2.onClick.AddListener(() =>
        {
            Debug.Log($"Button 2 clicked for Zone: {zone.Name}");
            PerformAction2(zone);
        });
    }
    private Vector3 GetZoneCenter(Zone zone)
    {
        if (zone.Vertices == null || zone.Vertices.Count == 0)
        {
            Debug.LogError("Zone has no vertices!");
            return Vector3.zero;
        }

        Vector3 center = Vector3.zero;

        // Sum all vertex positions
        foreach (var vertex in zone.Vertices)
        {
            center += vertex.ToVector3();
        }

        // Divide by the number of vertices to get the average
        center /= zone.Vertices.Count;

        // Adjust the height if necessary
        center.y += 1f; // Raise slightly above ground level 

        return center;
    }


    private void PerformAction1(Zone zone)
    {
        Debug.Log($"Performing Action 1 on Zone: {zone.Name}");

        // Calculate the center of the zone
        Vector3 zoneCenter = GetZoneCenter(zone);

        // Offset the positions slightly to avoid overlapping
        Vector3 excavatorPosition = zoneCenter + new Vector3(-5, 0, 0); // To the left
        Vector3 loaderPosition = zoneCenter + new Vector3(5, 0, 0); // To the right
        Vector3 truckPosition = zoneCenter + new Vector3(0, 0, 5); // Behind

        // Add the positions for this zone to the SimulationData singleton
        if (SimulationData.Instance.ZoneMachines.ContainsKey(zone.Name))
        {
            Debug.LogWarning($"Zone {zone.Name} already has machines assigned. Overwriting positions.");
        }
        SimulationData.Instance.ZoneMachines[zone.Name] = new ZoneMachinePositions(excavatorPosition, loaderPosition, truckPosition);

        Debug.Log($"Machines assigned for Zone {zone.Name} at: Excavator {excavatorPosition}, Loader {loaderPosition}, Truck {truckPosition}");
    }


    private void PerformAction2(Zone zone)
    {
        Debug.Log($"Performing Action 2 on Zone: {zone.Name}");

        // Save the selected zone name to SimulationData for later use
        if (SimulationData.Instance != null)
        {
            SimulationData.Instance.SelectedZone = zone.Name;
            Debug.Log($"Saved selected zone: {zone.Name}");
        }
        else
        {
            Debug.LogError("SimulationData instance not found!");
            return;
        }

        // Find SceneLoader dynamically
        SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader not found in the scene!");
            return;
        }

        // Load the "TaskBuilder" scene additively
        sceneLoader.LoadAdditiveScene("TaskBuilder");
    }

    public async void ConfigureLiveSite()
    {
        var transformedZones = new List<object>();
        var zoneID = 1;
        foreach (var zone in selectedSite.Layout.Zones)
        {
            var transformedZone = new
            {
                id = zoneID.ToString(), // TODO: must be added a ZoneID in ConstructionSiteData with the value generated in the Zone creation
                vertexs = zone.Vertices.Select(v => new
                {
                    x = (float)v.X,
                    z = (float)v.Z
                }).ToList()
            };
            zoneID++;
            transformedZones.Add(transformedZone);
        }

        var output = new
        {
            zones = transformedZones
        };

        var jsonOutput = JsonConvert.SerializeObject(output, Formatting.Indented);

        // Connect to broker
        var isConnected = await mqttManager.ConnectToBroker();
        if (!isConnected)
        {
            Debug.LogError("Could not connect to broker");
        }

        // Subscribe to site topic and publish zones configuration
        mqttManager.PublishMessage(selectedSite.SiteConfigTopic, jsonOutput);
        Debug.Log("Configuration published on topic: " + selectedSite.SiteConfigTopic);

        var isSubscribed = await mqttManager.SubscribeToTopic(selectedSite.SiteStatusTopic, HandleMessageReceived);
        if (!isSubscribed)
        {
            Debug.LogError("Could not subscribe to topic: " + selectedSite.SiteStatusTopic);
        }
        Debug.Log("Subscribed to topic: " + selectedSite.SiteStatusTopic);

        selectedSite.IsConfigured = true;
    }

    private async void HandleMessageReceived(string topic, string message)
    {
        // TODO: Create the JSON based on the complex task defined
        int json = 0;
        string jsonZone1 = File.ReadAllText("Assets/Zone1Test.json");
        string jsonZone2 = File.ReadAllText("Assets/Zone2Test.json");

        if (message == "ACK")
        {
            if (numACKReceived == 0)
            {
                foreach (var zone in selectedSite.Layout.Zones)
                {
                    // TODO: Build the JSON based on complex task definition
                    string jsonToSend = "";
                    var isSubscribed = false;
                    string topicConf = "";              // This part has to be automated using the generated topic in the json due to simulation
                    string topicStatus = "";            // This part has to be automated using the generated topic in the json due to simulation
                    if (json == 0)
                    {
                        topicConf = "/companies/0/sites/1/zones/1/config";
                        topicStatus = "/companies/0/sites/1/zones/1/status";
                        jsonToSend = jsonZone1;
                        Debug.Log(jsonZone1);
                        json++;
                    }
                    else
                    {
                        topicConf = "/companies/0/sites/1/zones/2/config";
                        topicStatus = "/companies/0/sites/1/zones/2/status";
                        jsonToSend = jsonZone2;
                        Debug.Log(jsonZone2);
                    }

                    mqttManager.PublishMessage(topicConf, jsonToSend);                                          // topicConf will be changed with zone.ZoneConfigTopic
                    isSubscribed = await mqttManager.SubscribeToTopic(topicStatus, HandleMessageReceived);      // topicStatus will be changed with zone.ZoneStatusTopic
                    if (!isSubscribed)
                    {
                        Debug.LogError("Could not subscribe to topic: " + zone.ZoneStatusTopic);
                    }
                    numACKReceived++;
                }
            }
            else
            {
                Debug.Log("ACK form topic: " + topic);
            }
        }
        else
        {
            MQTTMessage data = JsonConvert.DeserializeObject<MQTTMessage>(message);
            if (!data.IsMessageInQueue(messageQueue))
            {
                messageQueue.Enqueue(data);
                Debug.Log("Message added");
            }

            Debug.Log("Data: " + data.MachineId + ", " + data.Task + ", " + data.Coord[0] + " - " + data.Coord[1]);
        }
    }

    private IEnumerator UpdateMachinePositionsUI()
    {
        while (true)
        {
            if (messageQueue.Count > 0)
            {
                MQTTMessage message = messageQueue.Dequeue();

                // Controllo se esiste nella mappa
                if (activePrefabs.ContainsKey(message.MachineId))
                {
                    Destroy(activePrefabs[message.MachineId]);
                    activePrefabs.Remove(message.MachineId);
                }

                GameObject prefab = null;
                // TODO: Must be automatic based on machine id identify the type take it from the task once a constellation is assigned
                switch (message.MachineId)
                {
                    case "M001":
                        prefab = prefabExcavator;
                        break;
                    case "M002":
                        prefab = prefabTruck;
                        break;
                    case "M003":
                    case "M004":
                        prefab = prefabWheelLoader;
                        break;
                }

                float x = message.Coord[0];
                float z = message.Coord[1];

                Vector3 position = new Vector3(x, 500f, z);
                GameObject instantiatedPrefab = Instantiate(prefab, position, Quaternion.identity);
                activePrefabs[message.MachineId] = instantiatedPrefab;
            }
            else
            {
                if (numACKReceived > 0 && !buttonDisabled)
                {
                    Destroy(configLiveSite.gameObject);
                    buttonDisabled = true;
                }
            }
            yield return new WaitForSeconds(UPDATE_RATE);   // Wait for 10 seconds
        }
    }


    // Toggle the expanded section of the card
    public void ToggleExpand(GameObject card)
    {
        Transform expandedSection = card.transform.Find("ExpandedSection");
        expandedSection.gameObject.SetActive(!expandedSection.gameObject.activeSelf);
    }

    // Draw the perimeter of each zone
    private void RenderZones()
    {
        selectedSite.Layout.Zones.ForEach(zone => RenderZonePerimeter(zone));
    }

    // Draw the perimeter of the zone
    private void RenderZonePerimeter(Zone zone)
    {
        float elevationOffset = 6f; // Set the y-coordinate for the zone perimeter
                                    // Create a new LineRenderer for each zone perimeter
                                    // List<Vector3> zoneVertices = zone.Perimeter.points.ConvertAll(point => new Vector3(point.x, point.y, point.z));
        List<Vector3> zoneVertices = zone.Vertices.ConvertAll(point => point.ToVector3());
        GameObject lineObject = new GameObject("Zone Perimeter");
        LineRenderer zoneLineRenderer = lineObject.AddComponent<LineRenderer>();

        zoneLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        zoneLineRenderer.startColor = Color.red;
        zoneLineRenderer.endColor = Color.red;
        zoneLineRenderer.startWidth = 10f;
        zoneLineRenderer.endWidth = 10f;

        List<Vector3> elevatedVertices = new List<Vector3>();
        foreach (var point in zoneVertices)
        {
            // Apply the offset to the y-coordinate
            elevatedVertices.Add(new Vector3(point.x, point.y + elevationOffset, point.z));
        }
        if (elevatedVertices.Count > 0)
        {
            elevatedVertices.Add(elevatedVertices[0]);
        }

        // Set the elevated positions to the LineRenderer
        zoneLineRenderer.positionCount = elevatedVertices.Count;
        zoneLineRenderer.SetPositions(elevatedVertices.ToArray());

        // Keep reference to the LineRenderer so the zone remains rendered
        zoneRenderers.Add(zoneLineRenderer);

        // Create the filled polygon
        GameObject polygonObject = new GameObject("Polygon");
        polygonObject.AddComponent<PolygonFiller>().CreatePolygon(zoneVertices, colorDictionary[zone.Type], zone.Name);

    }

    private void RenderRoads()
    {
        // List<Vector3> renderPoints = selectedSite.RoadSystem.Nodes.ConvertAll(node => node.Position.ToVector3());


        // lineRenderer.material = lineMaterial;
        // lineRenderer.startColor = Color.yellow;
        // lineRenderer.endColor = Color.yellow;
        // lineRenderer.startWidth = 10f; // Adjust as needed
        // lineRenderer.endWidth = 10f;
        // lineRenderer.positionCount = renderPoints.Count;
        // lineRenderer.SetPositions(renderPoints.ToArray());

        // Debug.Log("Roads rendered.");


        List<RoadNode> nodes = selectedSite.RoadSystem.Nodes;
        List<RoadEdge> edges = selectedSite.RoadSystem.Edges;

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
        }

    }

}
