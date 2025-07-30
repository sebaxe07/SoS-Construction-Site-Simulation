using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Excavator : MovingVehicle
{
    // Attributes
    private double bucketCapacity;
    public double BucketCapacity
    {
        get { return bucketCapacity; }
        set { bucketCapacity = value; }
    }
    private bool isBucketFull = false;

    // Constructor
    public Excavator(int machineID, string typeId, string name, string manufacturer, string machineType, string engineType, double energyConsumption,
                    double maintenanceCost, string wheelType, double fuelCapacity, double maxSpeed, double avgSpeed, double bucketCapacity, int assignedWorkerID)
                    : base(machineID, typeId, name, manufacturer, machineType, engineType, energyConsumption, maintenanceCost, wheelType,
                    fuelCapacity, maxSpeed, avgSpeed, assignedWorkerID)
    {
        this.bucketCapacity = bucketCapacity;
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
    public void dig() { } //Tuple<double, double> position) { }
    public void emptyBucket() { } //Tuple<double, double> position) { }
    private void rotateCabin(double angle) { }
}

