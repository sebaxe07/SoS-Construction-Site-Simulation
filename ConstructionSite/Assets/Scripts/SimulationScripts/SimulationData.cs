using UnityEngine;
using System.Collections.Generic;

public class SimulationData : MonoBehaviour
{
    public static SimulationData Instance;

    // A dictionary to store positions for machines in multiple zones
    public Dictionary<string, ZoneMachinePositions> ZoneMachines = new Dictionary<string, ZoneMachinePositions>();
    // Store the currently selected zone name
    public string SelectedZone;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist this object across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

[System.Serializable]
public class ZoneMachinePositions
{
    public Vector3 ExcavatorPosition;
    public Vector3 LoaderPosition;
    public Vector3 TruckPosition;

    public ZoneMachinePositions(Vector3 excavator, Vector3 loader, Vector3 truck)
    {
        ExcavatorPosition = excavator;
        LoaderPosition = loader;
        TruckPosition = truck;
    }
}
