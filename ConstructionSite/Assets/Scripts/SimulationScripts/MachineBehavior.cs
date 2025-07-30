using UnityEngine;
using System.Collections;
using TaskManagement;
using System;
using System.Collections.Generic;
using UnityEngine.AI;// Include your task management namespace if needed

public abstract class MachineBehavior : MonoBehaviour
{
    public string MachineType; // Set this for each machine type (e.g., "Excavator", "Loader", "Truck")
    public int MachineID;
    public string AssignedZone;
    private static int idCounter = 0;

    public string MachineName = "Default Machine"; // Set this for each machine (e.g., "Excavator 1", "Loader 2", "Truck 3")
    public bool Working { get; set; } = false;
    public bool OnOff { get; set; } = false;
    public float UsageRate { get; set; } = 0.0f;
    public float DowntimeRate { get; set; } = 0.0f;
    public float DistanceTraveled { get; set; } = 0.0f;
    public float FuelConsumed { get; set; } = 0.0f;
    public float ActiveTime { get; set; } = 0.0f;
    public float tankCapacity { get; set; } = 0.0f;

    public bool Moving { get; set; } = false;

    public Queue<TaskData> Tasks { get; set; } = new Queue<TaskData>();
    private float workingTime = 0.0f;

    private NavMeshAgent agent;

    public List<MachineLogEntry> LogEntries = new List<MachineLogEntry>();
    private float lastLogTime = 0f;

    private FreeFlyCamera Freecamera;

    private void Awake()
    {
        MachineID = idCounter++;
        Debug.LogError($"Machine {MachineName} Awake");
        StartCoroutine(ActiveTimeCounter());
        agent = GetComponent<NavMeshAgent>();
        OnOff = true;
        Freecamera = FindObjectOfType<FreeFlyCamera>();
    }


    protected virtual void LogState(float simulationTime)
    {
        // Create a log entry for the current state
        MachineLogEntry entry = new MachineLogEntry
        {
            Timestamp = simulationTime,
            MachineName = MachineName,
            MachineType = MachineType,
            Position = transform.position,
            DistanceTraveled = DistanceTraveled,
            FuelConsumed = FuelConsumed,
            UsageRate = UsageRate,
            IsWorking = Working,
            IsMoving = Moving,
            CurrentTask = Tasks.Count > 0 ? Tasks.Peek().TaskType : "None"
        };

        LogEntries.Add(entry);
    }

    private void Update()
    {
        if (Freecamera == null)
        {
            // find the FreeFlyCamera component in the scene
            Freecamera = FindObjectOfType<FreeFlyCamera>();
        }
        float simulationTime = Freecamera.currentSimulationTime;
        if (simulationTime - lastLogTime > 1f) // Log every 1 second
        {
            LogState(simulationTime);
            lastLogTime = simulationTime;
        }
    }

    private IEnumerator ActiveTimeCounter()
    {
        while (true)
        {
            float deltaTime = Time.deltaTime;
            ActiveTime += deltaTime;

            if (Working)
            {
                workingTime += deltaTime;
            }

            // Calculate the usage rate as the percentage of time the machine is working
            UsageRate = (workingTime / ActiveTime) * 100.0f;

            // Calculate the downtime rate as the percentage of time the machine is not working
            DowntimeRate = 100.0f - UsageRate;

            yield return null;
        }
    }

    public IEnumerator FuelConsumptionCounter(float litersPerHour)
    {
        while (true)
        {
            if (FuelConsumed >= tankCapacity)
            {
                Debug.LogError($"Machine {MachineName} is out of fuel");
                Working = false;
                Moving = false;
                OnOff = false;
                break;
            }

            float litersPerSecond = litersPerHour / 3600.0f;
            if (Moving)
            {
                // If machine moving consume at base rate
                FuelConsumed += litersPerSecond * Time.deltaTime;
                yield return null;
            }
            if (Working)
            {
                // If machine working consume at 1.3 times the rate
                FuelConsumed += litersPerSecond * 1.3f * Time.deltaTime;
                yield return null;
            }

            // if machine is not moving or working consume at 0.2 times the rate
            FuelConsumed += litersPerSecond * 0.2f * Time.deltaTime;

            yield return null;
        }
    }

    public abstract IEnumerator ExecuteTask(TaskData task);

    public abstract void OnExitZone(String zoneName);
    public abstract void OnEnterZone();


}


