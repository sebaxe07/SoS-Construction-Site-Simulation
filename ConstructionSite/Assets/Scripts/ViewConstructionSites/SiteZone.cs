using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// @brief Represents a zone within a construction site.
[System.Serializable]
public class SiteZone
{
    [SerializeField] private int zoneID; ///< @brief Unique identifier for the zone.
    [SerializeField] private int siteID; ///< @brief Identifier for the site to which the zone belongs.
    [SerializeField] private string name; ///< @brief Name of the zone.
    [SerializeField] private double size; ///< @brief Size of the zone.
    [SerializeField] private string zoneType; ///< @brief Type of the zone.
    [SerializeField] private bool active; ///< @brief Indicates if the zone is active.
    [SerializeField] private int numberOfMachines; ///< @brief Number of machines in the zone.
    [SerializeField] private PolygonData perimeter; ///< @brief Perimeter of the zone.
    [SerializeField] private List<Vector3> gates; ///< @brief List of gate positions in the zone.
    [SerializeField] private string zoneConfigTopic;
    [SerializeField] private string zoneStatusTopic;

    /// @brief Constructor to initialize a SiteZone object.
    /// @param zoneID Unique identifier for the zone.
    /// @param siteID Identifier for the site to which the zone belongs.
    /// @param name Name of the zone.
    /// @param size Size of the zone.
    /// @param zoneType Type of the zone.
    /// @param active Indicates if the zone is active.
    /// @param numberOfMachines Number of machines in the zone.
    /// @param perimeter Perimeter of the zone.
    /// @param gates List of gate positions in the zone.
    public SiteZone(int zoneID, int siteID, string name, double size, string zoneType, bool active, int numberOfMachines, PolygonData perimeter, List<Vector3> gates, string zoneConfigTopic, string zoneStatusTopic)
    {
        this.zoneID = zoneID;
        this.siteID = siteID;
        this.name = name;
        this.size = size;
        this.zoneType = zoneType;
        this.active = active;
        this.numberOfMachines = numberOfMachines;
        this.perimeter = perimeter;
        this.gates = gates;
        this.zoneConfigTopic = zoneConfigTopic;
        this.zoneStatusTopic = zoneStatusTopic;
    }

    // Getter & Setter
    /// @brief Gets or sets the unique identifier for the zone.
    public int ZoneID
    {
        get { return zoneID; }
        set { zoneID = value; }
    }

    /// @brief Gets or sets the identifier for the site to which the zone belongs.
    public int SiteID
    {
        get { return siteID; }
        set { siteID = value; }
    }

    /// @brief Gets or sets the name of the zone.
    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    /// @brief Gets or sets the size of the zone.
    public double Size
    {
        get { return size; }
        set { size = value; }
    }

    /// @brief Gets or sets the type of the zone.
    public string ZoneType
    {
        get { return zoneType; }
        set { zoneType = value; }
    }

    /// @brief Gets or sets the number of machines in the zone.
    public int NumberOfMachines
    {
        get { return numberOfMachines; }
        set { numberOfMachines = value; }
    }

    /// @brief Gets or sets the perimeter of the zone.
    public PolygonData Perimeter
    {
        get { return perimeter; }
        set { perimeter = value; }
    }

    /// @brief Gets or sets the list of gate positions in the zone.
    public List<Vector3> Gates
    {
        get { return gates; }
        set { gates = value; }
    }

    /// @brief Gets or sets the active status of the zone.
    public bool Active
    {
        get { return active; }
        set { active = value; }
    }
    public string ZoneConfigTopic
    {
        get { return zoneConfigTopic; }
        set { zoneConfigTopic = value; }
    }
    public string ZoneStatusTopic
    {
        get { return zoneStatusTopic; }
        set { zoneStatusTopic = value; }
    }
}
