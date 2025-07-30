using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Machine
{
    // Attributes
    private int assignedWorkerID = 0;
    private int machineID;

    private string typeId;
    private string name;
    private string manufacturer;
    private string machineType;
    private string engineType;
    private double energyConsumption;
    private double maintenanceCost;
    private bool isAvailable = true;

    //private Worker assignedWorker;
    //private string State;

    // Constructor
    public Machine(int machineID, string typeId, string name, string manufacturer, string machineType,
                    string engineType, double energyConsumption, double maintenanceCost, int assignedWorkerID)
    {

        this.machineID = machineID;
        this.typeId = typeId;
        this.name = name;
        this.manufacturer = manufacturer;
        this.machineType = machineType;
        this.engineType = engineType;
        this.energyConsumption = energyConsumption;
        this.maintenanceCost = maintenanceCost;
        this.assignedWorkerID = assignedWorkerID;
    }

    // Methods
    public abstract void PerformTask(); // Task task);
    public abstract void ScheduleMaintenance();
    public abstract void ReportMalfunction();
    public abstract void updateState(string newState);
    public abstract float TotalConsumption(float timePeriod);

    // Getter & Setter
    public int AssignedWorkerID
    {
        get { return assignedWorkerID; }
        set { assignedWorkerID = value; }
    }

    public int MachineID
    {
        get { return machineID; }
        set { machineID = value; }
    }

    public string TypeId
    {
        get { return typeId; }
        set { typeId = value; }
    }

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public string Manufacturer
    {
        get { return manufacturer; }
        set { manufacturer = value; }
    }

    public string MachineType
    {
        get { return machineType; }
        set { machineType = value; }
    }

    public string EngineType
    {
        get { return engineType; }
        set { engineType = value; }
    }

    public double EnergyConsumption
    {
        get { return energyConsumption; }
        set { energyConsumption = value; }
    }

    public double MaintenanceCost
    {
        get { return maintenanceCost; }
        set { maintenanceCost = value; }
    }

    public bool IsAvailable
    {
        get { return isAvailable; }
        set { isAvailable = value; }
    }
}