using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteadyVehicle : Machine
{
    // Attributes
    private bool immovable;

    // private Tuple<double, double> position;

    // Constructor
    public SteadyVehicle(int machineID, string typeId, string name, string manufacturer, string machineType, string engineType,
                        double energyConsumption, double maintenanceCost, bool immovable, int assignedWorkerID)
                        : base(machineID, typeId, name, manufacturer, machineType, engineType, energyConsumption, maintenanceCost, assignedWorkerID)
    {
        this.immovable = immovable;
    }

    // Getter & Setter
    public bool Immovable
    {
        get { return immovable; }
        set { immovable = value; }
    }

    /*
    public bool Position {
        get { return position; }
        set { position = value; }
    }
    */
}