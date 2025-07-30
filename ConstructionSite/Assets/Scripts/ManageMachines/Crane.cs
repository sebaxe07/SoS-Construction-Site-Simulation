using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crane : SteadyVehicle
{
    // Attributes
    private double loadMaxWeight;
    public double LoadMaxWeight
    {
        get { return loadMaxWeight; }
        set { loadMaxWeight = value; }
    }
    private bool isMaxWeight = false;

    // Constructor
    public Crane(int machineID, string typeId, string name, string manufacturer, string machineType, string engineType,
                        double energyConsumption, double maintenanceCost, bool immovable, double loadMaxWeight, int assignedWorkerID)
                        : base(machineID, typeId, name, manufacturer, machineType, engineType, energyConsumption, maintenanceCost, immovable, assignedWorkerID)
    {
        this.loadMaxWeight = loadMaxWeight;
    }

    // Methods Override - Abstract Machine
    public override void PerformTask() { } // Task task);
    public override void ScheduleMaintenance() { }
    public override void ReportMalfunction() { }
    public override void updateState(string newState) { }
    public override float TotalConsumption(float timePeriod) { return 0.1f; }

    // Methods Override - Abstract Steady Vehicle

    // Methods
    public void liftLoad() { } //Tuple<double, double>position) } { } 
    public void lowerLoad() { } // Tuple<double, double> position) { }
    private void rotateBoom(double angle) { }
}