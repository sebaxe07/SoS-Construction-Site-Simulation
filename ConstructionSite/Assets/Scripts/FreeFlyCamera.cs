using System.Collections;
using System.IO;
using System.Linq;
using TaskManagement;
using TMPro;
using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float movementSpeed = 10.0f;
    public float sprintMultiplier = 2.0f;
    public float lookSpeed = 2.0f;
    public float lookSmoothness = 0.1f; // Smoothing factor for mouse look

    [Header("UI")]
    public GameObject InfoPanel;
    public TextMeshProUGUI MachineNameText;
    public TextMeshProUGUI WorkingText;
    public TextMeshProUGUI UsageRateText;
    public TextMeshProUGUI DowntimeRateText;
    public TextMeshProUGUI DistanceTraveledText;
    public TextMeshProUGUI FuelConsumedText;
    public TextMeshProUGUI FuelLeftText;
    public TextMeshProUGUI ActiveTimeText;
    public GameObject TaskScrollView;
    public Transform ScrollContent;
    public GameObject TaskCard;
    public TextMeshProUGUI SimulationSpeedText;

    private MachineBehavior selectedMachine;

    private Vector2 rotation = Vector2.zero;
    private Vector2 currentRotation = Vector2.zero;
    private Vector2 rotationVelocity = Vector2.zero;

    public float timeScale = 1.0f;

    private SimulationLog simulationLog = new SimulationLog();
    private float simulationStartTime;
    public float currentSimulationTime;

    private bool isPaused = false;
    private void Start()
    {
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InfoPanel.SetActive(false);
        simulationStartTime = Time.time;
        simulationLog.SimulationStartTime = simulationStartTime;
    }

    private void Update()
    {
        // Update the simulation time
        currentSimulationTime = Time.time - simulationStartTime;

        // Mouse Look
        rotation.x += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.y += Input.GetAxis("Mouse Y") * lookSpeed;
        rotation.y = Mathf.Clamp(rotation.y, -90f, 90f); // Clamp vertical rotation

        if (Input.GetKeyDown(KeyCode.M))
        {
            // Find the SceneLoader
            SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
            if (sceneLoader != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Find the ZonePopulator
                ZonePopulator zonePopulator = FindObjectOfType<ZonePopulator>();
                if (zonePopulator != null)
                {
                    zonePopulator.ResetTerrain();

                    // Back to last scene
                    sceneLoader.LoadScene("OpenConstructionSite");

                }
                else
                {
                    sceneLoader.LoadScene("SiteCreation");
                }

            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            InfoPanel.SetActive(!InfoPanel.activeSelf);
            if (updateInfoCoroutine != null)
            {
                StopCoroutine(updateInfoCoroutine);
            }

            if (selectedMachine != null)
            {
                updateInfoCoroutine = StartCoroutine(UpdateMachineInfo(selectedMachine));
            }


        }

        // Time Scale Control
        // Press 0 to pause the simulation
        if (!isPaused)
        {

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Time.timeScale = 0;
                SimulationSpeedText.text = "Simulation Paused";
            }
            // Press 1 to set time scale to 1
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                timeScale = 1.0f;
                Time.timeScale = timeScale;
                SimulationSpeedText.text = "Simulation Speed: 1x";
            }
            // Press 2 to set time scale to 2
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                timeScale = 2.0f;
                Time.timeScale = timeScale;
                SimulationSpeedText.text = "Simulation Speed: 2x";
            }
            // Press 3 to set time scale to 3
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                timeScale = 3.0f;
                Time.timeScale = timeScale;
                SimulationSpeedText.text = "Simulation Speed: 3x";
            }
            // Press 4 to set time scale to 4
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                timeScale = 4.0f;
                Time.timeScale = timeScale;
                SimulationSpeedText.text = "Simulation Speed: 4x";
            }

            // Press to end the simulation and save the log
            if (Input.GetKeyDown(KeyCode.P))
            {
                isPaused = true;
                LogSimulationEnd();
            }

        }

        currentRotation = Vector2.SmoothDamp(currentRotation, rotation, ref rotationVelocity, lookSmoothness);
        transform.localRotation = Quaternion.Euler(-currentRotation.y, currentRotation.x, 0);

        // Keyboard Movement
        float currentSpeed = movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        float moveX = Input.GetAxis("Horizontal") * currentSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * currentSpeed * Time.deltaTime;
        float moveY = 0;

        // Fly Up and Down with Q and E
        if (Input.GetKey(KeyCode.Q)) moveY = -currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) moveY = currentSpeed * Time.deltaTime;

        // If user clicks send a raycast from the camera straight forward to see if it hits a machine
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            // Draw the ray in the editor
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {

                if (hit.collider.TryGetComponent<MachineBehavior>(out MachineBehavior machineBehavior))
                {
                    selectedMachine = machineBehavior;
                    if (updateInfoCoroutine != null)
                    {
                        StopCoroutine(updateInfoCoroutine);
                    }

                    InfoPanel.SetActive(true);
                    updateInfoCoroutine = StartCoroutine(UpdateMachineInfo(machineBehavior));
                }
            }
        }
        transform.Translate(new Vector3(moveX, moveY, moveZ));
    }

    private Coroutine updateInfoCoroutine;

    private IEnumerator UpdateMachineInfo(MachineBehavior machineBehavior)
    {
        while (InfoPanel.activeSelf)
        {
            MachineNameText.text = machineBehavior.MachineName;
            WorkingText.text = machineBehavior.Working ? "Status: Working" : "Status: Idle";
            UsageRateText.text = "Usage Rate: " + machineBehavior.UsageRate.ToString("F2") + "%";
            DowntimeRateText.text = "Downtime Rate: " + machineBehavior.DowntimeRate.ToString("F2") + "%";

            // Display the distance traveled in meters or kilometers depending on the value
            if (machineBehavior.DistanceTraveled >= 1000)
            {
                DistanceTraveledText.text = "Distance Traveled: " + (machineBehavior.DistanceTraveled / 1000).ToString("F2") + "km";
            }
            else
            {
                DistanceTraveledText.text = "Distance Traveled: " + machineBehavior.DistanceTraveled.ToString("F2") + "m";
            }

            // Check the value of Fuel to then display at the correct scale (L, mL, etc.)
            string fuelUnit = "L";
            float fuelValue = machineBehavior.FuelConsumed;
            if (fuelValue < 1000)
            {
                fuelUnit = "mL";
                fuelValue *= 1000;
            }
            FuelConsumedText.text = "Fuel Consumed: " + fuelValue.ToString("F2") + fuelUnit;
            // Update the fuel left trying to not round up the value
            double fuelLeft = machineBehavior.tankCapacity - machineBehavior.FuelConsumed;
            Debug.Log($"Fuel left {fuelLeft}");
            fuelLeft = System.Math.Round(fuelLeft, 10);

            FuelLeftText.text = "Fuel Left: " + fuelLeft.ToString("F2") + "L";

            // Map the active time to a string in the format "HH:MM:SS"
            int hours = (int)machineBehavior.ActiveTime / 3600;
            int minutes = (int)(machineBehavior.ActiveTime % 3600) / 60;
            int seconds = (int)machineBehavior.ActiveTime % 60;
            ActiveTimeText.text = "Active Time: " + hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");

            // Update the task list ScrollView
            if (TaskScrollView != null)
            {
                // Clear existing task items
                foreach (Transform child in ScrollContent)
                {
                    Destroy(child.gameObject);
                }

                // Add task items
                foreach (TaskData task in machineBehavior.Tasks)
                {
                    GameObject taskItem = Instantiate(TaskCard, ScrollContent);
                    Transform mainSection = taskItem.transform.Find("MainSection");

                    TextMeshProUGUI taskTypeText = mainSection.Find("TaskType").GetComponent<TextMeshProUGUI>();
                    taskTypeText.text = task.TaskType;

                    TextMeshProUGUI taskPosText = mainSection.Find("TaskPos").GetComponent<TextMeshProUGUI>();
                    // Display the task position as a string in the format "X:Y:Z" if the task has a position, otherwise display ""
                    taskPosText.text = task.Position != Vector3.zero ? $"X: {task.Position.x}, Y: {task.Position.y}, Z: {task.Position.z}" : task.TargetZone != null ? task.TargetZone : "";

                }
            }

            yield return new WaitForSeconds(1f); // Update every second
        }

    }
    public void LogSimulationEnd()
    {
        simulationLog.SimulationEndTime = Time.time;

        // Collect all machines
        MachineBehavior[] machines = FindObjectsOfType<MachineBehavior>();

        // Group machines by their assigned zones
        var groupedByZones = machines
            .GroupBy(machine => machine.AssignedZone)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Create zone logs
        foreach (var zone in groupedByZones)
        {
            ZoneLog zoneLog = new ZoneLog
            {
                ZoneName = zone.Key
            };

            foreach (var machine in zone.Value)
            {
                zoneLog.MachineLogs.AddRange(machine.LogEntries);
            }

            simulationLog.ZoneLogs.Add(zoneLog);
        }

        // Serialize to JSON
        string json = JsonUtility.ToJson(simulationLog, true);

        ZonePopulator zonePopulator = FindObjectOfType<ZonePopulator>();

        string siteName = zonePopulator != null ? zonePopulator.selectedSite.Name : "Site";
        // Save to a file
        string path = Application.persistentDataPath + $"/{siteName}_SimulationLog.json";
        File.WriteAllText(path, json);

        Debug.Log($"Simulation log saved to {path}");
    }
}