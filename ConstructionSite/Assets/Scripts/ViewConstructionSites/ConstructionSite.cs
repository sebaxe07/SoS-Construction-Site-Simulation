using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

/// @brief Class for loading and managing construction site data.
[System.Serializable]
public class ConstructionSiteLoader
{
    // Attributes
    [SerializeField] private int siteID; ///< @brief The unique identifier for the construction site.
    [SerializeField] private string name; ///< @brief The name of the construction site.
    [SerializeField] private string address; ///< @brief The address of the construction site.
    [SerializeField] private double area; ///< @brief The area of the construction site in square meters.
    [SerializeField] private string status; ///< @brief The current status of the construction site.
    [SerializeField] private DateTime startDate; ///< @brief The start date of the construction project.
    [SerializeField] private DateTime endDate; ///< @brief The end date of the construction project.
    [SerializeField] private PolygonData perimeter; ///< @brief The perimeter of the construction site.
    [SerializeField] private List<SiteZone> zones; ///< @brief The list of zones within the construction site.
    [SerializeField] private string siteConfigTopic;
    [SerializeField] private string siteStatusTopic;
    [SerializeField] private bool isConfigured;

    // Constructor
    /// @brief Initializes a new instance of the ConstructionSiteLoader class.
    /// @param siteID The unique identifier for the construction site.
    /// @param name The name of the construction site.
    /// @param address The address of the construction site.
    /// @param area The area of the construction site in square meters.
    /// @param status The current status of the construction site.
    /// @param perimeter The perimeter of the construction site.
    /// @param startDate The start date of the construction project.
    /// @param endDate The end date of the construction project.
    /// @param zones The list of zones within the construction site.
    public ConstructionSiteLoader(int siteID, string name, string address, double area, string status, PolygonData perimeter, string siteConfigTopic, string siteStatusTopic, bool isConfigured, DateTime startDate = default, DateTime endDate = default, List<SiteZone> zones = null)
    {
        this.siteID = siteID;
        this.name = name;
        this.address = address;
        this.area = area;
        this.status = status;
        this.startDate = startDate;
        this.endDate = endDate;
        this.perimeter = perimeter;
        this.zones = zones;
        this.siteConfigTopic = siteConfigTopic;
        this.siteStatusTopic = siteStatusTopic;
        this.isConfigured = isConfigured;
    }

    // Methods
    //public void UpdateSiteDetails(string[] data);
    //public void Simulate();
    //public void Realtime();
    //public void ReportIssue(string issue);
    //public void AddZone(Zone zone);
    //public void InspectZone(Zone zone);

    /// @brief Saves the construction site data to a JSON file.
    /// @param filePath The file path where the JSON file will be saved.
    public void SaveToJson(string filePath)
    {
        string startDateString = startDate.ToString("yyyy-MM-dd");
        string endDateString = endDate.ToString("yyyy-MM-dd");

        // Create an object with DateTime as string for saving
        var saveData = new SaveData
        {
            siteID = siteID,
            name = name,
            address = address,
            area = area,
            status = status,
            startDate = startDateString,
            endDate = endDateString,
            perimeter = perimeter,
            zones = zones
        };

        // Convert saveData to JSON and write to file
        string jsonContent = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, jsonContent);
    }

    /// @brief Loads the construction site data from a JSON file.
    /// @param filePath The file path from where the JSON file will be read.
    /// @return A ConstructionSiteLoader object with the loaded data.
    public static ConstructionSiteLoader LoadFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found at {filePath}");
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonContent);

            // Validate and convert startDate
            DateTime parsedStartDate = DateTime.MinValue;
            if (!string.IsNullOrEmpty(loadedData.startDate) && DateTime.TryParse(loadedData.startDate, out var tempStartDate))
            {
                parsedStartDate = tempStartDate;
            }
            else
            {
                Debug.LogError("Invalid or missing startDate in JSON data.");
            }

            // Validate and convert endDate
            DateTime parsedEndDate = DateTime.MinValue;
            if (!string.IsNullOrEmpty(loadedData.endDate) && DateTime.TryParse(loadedData.endDate, out var tempEndDate))
            {
                parsedEndDate = tempEndDate;
            }
            else
            {
                Debug.LogError("Invalid or missing endDate in JSON data.");
            }

            // Convert the saved string dates back to DateTime
            ConstructionSiteLoader site = new ConstructionSiteLoader
            (
                loadedData.siteID,
                loadedData.name,
                loadedData.address,
                loadedData.area,
                loadedData.status,
                loadedData.perimeter,
                loadedData.siteConfigTopic,
                loadedData.siteStatusTopic,
                loadedData.isConfigured,
                parsedStartDate,  // Convert string back to DateTime
                parsedEndDate,      // Convert string back to DateTime
                loadedData.zones
            );

            return site;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }

    /// @brief Saves a list of construction sites to a JSON file.
    /// @param sites The list of construction sites to be saved.
    /// @param filePath The file path where the JSON file will be saved.
    public static void SaveSitesToJson(List<ConstructionSiteLoader> sites, string filePath)
    {
        // Create a list of SaveData objects for serialization
        var saveDataList = new List<SaveData>();
        foreach (var site in sites)
        {
            saveDataList.Add(new SaveData
            {
                siteID = site.siteID,
                name = site.name,
                address = site.address,
                area = site.area,
                status = site.status,
                siteConfigTopic = site.siteConfigTopic,
                siteStatusTopic = site.siteStatusTopic,
                isConfigured = site.isConfigured,
                startDate = site.startDate.ToString("yyyy-MM-dd"),
                endDate = site.endDate.ToString("yyyy-MM-dd"),
                perimeter = site.perimeter,
                zones = site.zones
            });
        }

        // Serialize the list of SaveData objects
        string jsonContent = JsonUtility.ToJson(new SaveDataListWrapper { sites = saveDataList }, true);
        File.WriteAllText(filePath, jsonContent);
    }

    /// @brief Loads a list of construction sites from a JSON file.
    /// @param filePath The file path from where the JSON file will be read.
    /// @return A list of ConstructionSiteLoader objects with the loaded data.
    public static List<ConstructionSiteLoader> LoadSitesFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found at {filePath}");
            return null;
        }

        try
        {
            // Read JSON file content
            string jsonContent = File.ReadAllText(filePath);

            // Deserialize the JSON to a wrapper class
            SaveDataListWrapper loadedData = JsonUtility.FromJson<SaveDataListWrapper>(jsonContent);

            // Convert the list of SaveData to a list of ConstructionSiteLoader objects
            var sites = new List<ConstructionSiteLoader>();
            foreach (var siteData in loadedData.sites)
            {
                DateTime.TryParse(siteData.startDate, out DateTime startDate);
                DateTime.TryParse(siteData.endDate, out DateTime endDate);

                sites.Add(new ConstructionSiteLoader(
                    siteData.siteID,
                    siteData.name,
                    siteData.address,
                    siteData.area,
                    siteData.status,
                    siteData.perimeter,
                    siteData.siteConfigTopic,
                    siteData.siteStatusTopic,
                    siteData.isConfigured,
                    startDate,
                    endDate,
                    siteData.zones
                ));
            }

            return sites;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }

    /// @brief Returns a string that represents the current object.
    /// @return A string that represents the current object.
    public override string ToString()
    {
        return $"Site ID: {siteID}, Name: {name}, Address: {address}, Area: {area}, Status: {status}, Start Date: {startDate}, End Date: {endDate}, Perimeter: {perimeter}, Zones: {zones}";
    }

    // Getter & Setter
    public int SiteID
    {
        get { return siteID; }
        set { siteID = value; }
    }

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public string Address
    {
        get { return address; }
        set { address = value; }
    }

    public double Area
    {
        get { return area; }
        set { area = value; }
    }

    public string Status
    {
        get { return status; }
        set { status = value; }
    }

    public DateTime StartDate
    {
        get { return startDate; }
        set { startDate = value; }
    }

    public DateTime EndDate
    {
        get { return endDate; }
        set { endDate = value; }
    }

    public PolygonData Perimeter
    {
        get { return perimeter; }
        set { perimeter = value; }
    }
    public List<SiteZone> Zones
    {
        get { return zones; }
        set { zones = value; }
    }
    public string SiteConfigTopic
    {
        get { return siteConfigTopic; }
        set { siteConfigTopic = value; }
    }
    public string SiteStatusTopic
    {
        get { return siteStatusTopic; }
        set { siteStatusTopic = value; }
    }
    public bool IsConfigured
    {
        get { return isConfigured; }
        set { isConfigured = value; }
    }

    /// @brief Wrapper class for saving a list of sites
    [System.Serializable]
    public class SaveDataListWrapper
    {
        public List<SaveData> sites; ///< @brief The list of SaveData objects representing construction sites.
    }

    /// @brief Nested class for saving (with DateTime as string)
    [System.Serializable]
    public class SaveData
    {
        public int siteID; ///< @brief The unique identifier for the construction site.
        public string name; ///< @brief The name of the construction site.
        public string address; ///< @brief The address of the construction site.
        public double area; ///< @brief The area of the construction site in square meters.
        public string status; ///< @brief The current status of the construction site.
        public string startDate; ///< @brief The start date of the construction project as a string.
        public string endDate; ///< @brief The end date of the construction project as a string.
        public PolygonData perimeter; ///< @brief The perimeter of the construction site.
        public List<SiteZone> zones; ///< @brief The list of zones within the construction site.
        public string siteConfigTopic;
        public string siteStatusTopic;
        public bool isConfigured;
    }
}
