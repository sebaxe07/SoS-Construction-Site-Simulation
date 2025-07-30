using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovingVehicle : Machine
{
    // Attributes
    private string plateNumber = "";
    private string wheelType;
    private double fuelCapacity;
    private double fuelLevel;
    private double maxSpeed;
    private double avgSpeed;

    // Constructor
    public MovingVehicle(int machineID, string typeId, string name, string manufacturer, string machineType, string engineType, double energyConsumption,
                        double maintenanceCost, string wheelType, double fuelCapacity, double maxSpeed, double avgSpeed, int assignedWorkerID)
                        : base(machineID, typeId, name, manufacturer, machineType, engineType, energyConsumption, maintenanceCost, assignedWorkerID)
    {
        this.wheelType = wheelType;
        this.fuelCapacity = fuelCapacity;
        fuelLevel = fuelCapacity;
        this.maxSpeed = maxSpeed;
        this.avgSpeed = avgSpeed;
    }

    // Methods
    public abstract void Refuel();
    public abstract void MoveFromTo(); //Path path);
    public abstract void Move();
    public abstract void Steer(float angle);
    public abstract void Stop();

    // Getter & Setter
    public string PlateNumber
    {
        get { return plateNumber; }
        set { plateNumber = value; }
    }
    public string WheelType
    {
        get { return wheelType; }
        set { wheelType = value; }
    }

    public double FuelCapacity
    {
        get { return fuelCapacity; }
        set { fuelCapacity = value; }
    }

    public double FuelLevel
    {
        get { return fuelLevel; }
        set { fuelLevel = value; }
    }

    public double MaxSpeed
    {
        get { return maxSpeed; }
        set { maxSpeed = value; }
    }

    public double AvgSpeed
    {
        get { return avgSpeed; }
        set { avgSpeed = value; }
    }
}