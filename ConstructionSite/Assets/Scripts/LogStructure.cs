using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MachineLogEntry
{
    public float Timestamp; // Simulation time
    public string MachineName;
    public string MachineType;
    public Vector3 Position; // Machine's position at the time
    public float DistanceTraveled;
    public float FuelConsumed;
    public float UsageRate;
    public bool IsWorking;
    public bool IsMoving;
    public string CurrentTask; // Optional, for task details
}


[System.Serializable]
public class SimulationLog
{
    public float SimulationStartTime;
    public float SimulationEndTime;
    public List<ZoneLog> ZoneLogs = new List<ZoneLog>();
}


[System.Serializable]
public class ZoneLog
{
    public string ZoneName; // Name of the zone (e.g., "A0", "A1")
    public List<MachineLogEntry> MachineLogs = new List<MachineLogEntry>();
}

