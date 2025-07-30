using UnityEngine;
using TaskManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public DiggerBehavior excavatorPrefab;
    public LoaderBehavior wheelLoaderPrefab;
    public TruckBehavior truckPrefab;

    private Dictionary<string, List<GameObject>> zoneMachines = new Dictionary<string, List<GameObject>>();

    public void StartSimulation()
    {
        Debug.Log("Starting SimulationManager...");

        // Spawn machines for all zones from SimulationData
        if (SimulationData.Instance != null)
        {
            foreach (var zoneEntry in SimulationData.Instance.ZoneMachines)
            {
                string zoneName = zoneEntry.Key;
                ZoneMachinePositions positions = zoneEntry.Value;

                Debug.Log($"Spawning machines for Zone {zoneName}");
                SpawnMachinesForZone(zoneName, positions.ExcavatorPosition, positions.LoaderPosition, positions.TruckPosition);
            }
        }

        // Assign tasks to machines by zones
        AssignZoneTasksToMachines();
    }

    public void SpawnMachinesForZone(string zoneName, Vector3 excavatorPosition, Vector3 loaderPosition, Vector3 truckPosition)
    {
        if (!zoneMachines.ContainsKey(zoneName))
        {
            zoneMachines[zoneName] = new List<GameObject>();
        }

        // Find a new position for the excavator
        GameObject ZoneEnd = GameObject.Find(zoneName);

        // find the node inside the zone
        Transform[] objlst = ZoneEnd.GetComponentsInChildren<Transform>();


        var endNodes = ZoneEnd.GetComponentsInChildren<Transform>().Where(t => t.CompareTag("GateTrigger")).ToList();
        if (endNodes.Count == 0)
        {
            Debug.LogWarning($"No gate nodes found in zone {ZoneEnd}");
        }
        else
        {
            Debug.Log($"Found {endNodes.Count} gate nodes in zone {ZoneEnd}");
            Transform spawnNode = endNodes[0];

            Vector3 TargetPositionExcavator = spawnNode.position;
            Vector3 TargetPositionLoader = spawnNode.position;
            Vector3 TargetPositionTruck = spawnNode.position;
            // offset the targe position a bit in direction to the center of the zone (gameobject)

            Vector3 offsetDirectionExcavator = (excavatorPosition - spawnNode.position).normalized; // Away from gate
            Vector3 offsetDirectionLoader = (loaderPosition - spawnNode.position).normalized; // Away from gate
            Vector3 offsetDirectionTruck = (truckPosition - spawnNode.position).normalized; // Away from gate

            TargetPositionExcavator += offsetDirectionExcavator * 50;
            TargetPositionLoader += offsetDirectionLoader * 50;
            TargetPositionTruck += offsetDirectionTruck * 50;
            Debug.Log($"New target position for excavator: {TargetPositionExcavator}");
            Debug.Log($"New target position for loader: {TargetPositionLoader}");
            Debug.Log($"New target position for truck: {TargetPositionTruck}");
            excavatorPosition = TargetPositionExcavator;
            loaderPosition = TargetPositionLoader;
            truckPosition = TargetPositionTruck;
        }

        // Spawn excavator
        if (excavatorPrefab != null)
        {
            GameObject excavator = Instantiate(excavatorPrefab.gameObject, excavatorPosition, Quaternion.identity);
            excavator.SetActive(true); // Ensure the excavator is active
            MachineBehavior excavatorBehavior = excavator.GetComponent<MachineBehavior>();
            if (excavatorBehavior == null)
            {
                Debug.LogError("Excavator does not have a MachineBehavior component!");
            }
            else
            {
                excavatorBehavior.MachineType = "Excavator";
                zoneMachines[zoneName].Add(excavator);
                Debug.Log($"Spawned excavator for Zone {zoneName} at {excavatorPosition}");
                // Find the ZoneManager for the zone and add the excavator to the zone
                GameObject zoneManager = GameObject.Find(zoneName);
                if (zoneManager != null)
                {
                    ZoneController zoneController = zoneManager.GetComponent<ZoneController>();
                    if (zoneController != null)
                    {
                        zoneController.AddObjectToZone(excavator);
                    }
                    else
                    {
                        Debug.LogError("ZoneManager does not have a ZoneController component!");
                    }
                }
                else
                {
                    Debug.LogError("ZoneManager not found for Zone " + zoneName);
                }
            }
        }
        else
        {
            Debug.LogError("Excavator prefab is not assigned!");
        }

        // Spawn loader
        if (wheelLoaderPrefab != null)
        {
            GameObject loader = Instantiate(wheelLoaderPrefab.gameObject, loaderPosition, Quaternion.identity);
            loader.SetActive(true); // Ensure the loader is active
            MachineBehavior loaderBehavior = loader.GetComponent<MachineBehavior>();
            if (loaderBehavior == null)
            {
                Debug.LogError("Loader does not have a MachineBehavior component!");
            }
            else
            {
                loaderBehavior.MachineType = "Loader";
                zoneMachines[zoneName].Add(loader);
                Debug.Log($"Spawned loader for Zone {zoneName} at {loaderPosition}");
                // Find the ZoneManager for the zone and add the loader to the zone
                GameObject zoneManager = GameObject.Find(zoneName);
                if (zoneManager != null)
                {
                    ZoneController zoneController = zoneManager.GetComponent<ZoneController>();
                    if (zoneController != null)
                    {
                        zoneController.AddObjectToZone(loader);
                    }
                    else
                    {
                        Debug.LogError("ZoneManager does not have a ZoneController component!");
                    }
                }
                else
                {
                    Debug.LogError("ZoneManager not found for Zone " + zoneName);
                }

            }
        }
        else
        {
            Debug.LogError("Loader prefab is not assigned!");
        }

        // Spawn truck
        if (truckPrefab != null)
        {
            GameObject truck = Instantiate(truckPrefab.gameObject, truckPosition, Quaternion.identity);
            truck.SetActive(true); // Ensure the truck is active
            MachineBehavior truckBehavior = truck.GetComponent<MachineBehavior>();
            if (truckBehavior == null)
            {
                Debug.LogError("Truck does not have a MachineBehavior component!");
            }
            else
            {
                truckBehavior.MachineType = "Truck";
                zoneMachines[zoneName].Add(truck);
                Debug.Log($"Spawned truck for Zone {zoneName} at {truckPosition}");
                // Find the ZoneManager for the zone and add the truck to the zone
                GameObject zoneManager = GameObject.Find(zoneName);
                if (zoneManager != null)
                {
                    ZoneController zoneController = zoneManager.GetComponent<ZoneController>();
                    if (zoneController != null)
                    {
                        zoneController.AddObjectToZone(truck);
                    }
                    else
                    {
                        Debug.LogError("ZoneManager does not have a ZoneController component!");
                    }
                }
                else
                {
                    Debug.LogError("ZoneManager not found for Zone " + zoneName);
                }
            }
        }
        else
        {
            Debug.LogError("Truck prefab is not assigned!");
        }
    }


    private void AssignZoneTasksToMachines()
    {
        if (SimulationData.Instance == null)
        {
            Debug.LogError("SimulationData instance not found!");
            return;
        }

        foreach (var zoneEntry in SimulationData.Instance.ZoneMachines)
        {
            string zoneName = zoneEntry.Key;
            if (!zoneMachines.ContainsKey(zoneName))
            {
                Debug.LogWarning($"No machines found for Zone {zoneName}");
                continue;
            }

            List<GameObject> machines = zoneMachines[zoneName];
            TaskAllocator.AllocateTasksToMachinesInZone(zoneName, machines);
        }
    }

    public void StartMachineTaskProcessing(GameObject machine, Queue<TaskData> tasks)
    {
        Debug.LogError($"Starting task processing for machine {machine.name} with {tasks.Count} tasks");

        StartCoroutine(ProcessMachineTasks(machine, tasks));
    }

    private IEnumerator ProcessMachineTasks(GameObject machine, Queue<TaskData> tasks)
    {
        while (tasks.Count > 0)
        {
            TaskData currentTask = tasks.Dequeue();
            Debug.LogError($"Processing task: {currentTask.TaskType} for machine {machine.name}");

            MachineBehavior machineBehavior = machine.GetComponent<MachineBehavior>();
            if (machineBehavior != null)
            {
                yield return StartCoroutine(machineBehavior.ExecuteTask(currentTask));
            }
            else
            {
                Debug.LogError($"Machine {machine.name} does not have a MachineBehavior component!");
            }

            Debug.LogError($"Task {currentTask.TaskType} completed for machine {machine.name}");
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ClearZoneMachines(string zoneName)
    {
        if (zoneMachines.ContainsKey(zoneName))
        {
            foreach (var machine in zoneMachines[zoneName])
            {
                Destroy(machine);
            }

            zoneMachines[zoneName].Clear();
            Debug.Log($"Cleared all machines for Zone {zoneName}");
        }
    }
}
