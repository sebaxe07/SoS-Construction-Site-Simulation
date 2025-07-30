using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MovingVehicle
{
    // Attributes
    private double loadCapacity;
    public double LoadCapacity
    {
        get { return loadCapacity; }
        set { loadCapacity = value; }
    }
    private bool isTruckBedFull = false;

    // Constructor
    public Truck(int machineID, string typeId, string name, string manufacturer, string machineType, string engineType, double energyConsumption,
                    double maintenanceCost, string wheelType, double fuelCapacity, double maxSpeed, double avgSpeed, double loadCapacity, int assignedWorkerID)
                    : base(machineID, typeId, name, manufacturer, machineType, engineType, energyConsumption, maintenanceCost, wheelType,
                    fuelCapacity, maxSpeed, avgSpeed, assignedWorkerID)
    {
        this.loadCapacity = loadCapacity;
    }

    // Methods Override - Abstract Machine
    public override void PerformTask() { } // Task task);
    public override void ScheduleMaintenance() { }
    public override void ReportMalfunction() { }
    public override void updateState(string newState) { }
    public override float TotalConsumption(float timePeriod) { return 0.1f; }

    // Methods Override - Abstract Moving Vehicle
    public override void Refuel() { }
    public override void MoveFromTo() { } //Path path);
    public override void Move() { }
    public override void Steer(float angle) { }
    public override void Stop() { }

    // Methods
    public void transportMaterial() { } //Path path, double material) { } 
    public void loadBed() { }
    public void deloadBed() { }
}