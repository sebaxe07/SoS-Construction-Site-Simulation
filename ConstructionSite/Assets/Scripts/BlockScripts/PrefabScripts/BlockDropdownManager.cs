using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;

public class BlockDropdownManager : MonoBehaviour
{
    [Header("Dropdown References")]
    [SerializeField] private TMP_Dropdown sourceDropdown;  // First dropdown
    [SerializeField] private TMP_Dropdown targetDropdown;  // Second dropdown (if exists)

    [Header("Block Type")]
    [SerializeField] private BlockType blockType;

    private static SelectedSiteData selectedSiteData;


    private void Start()
    {

        InitializeDropdowns();
    }

    public void SetSelectedSiteData(SelectedSiteData data)
    {
        selectedSiteData = data;
        if (gameObject.activeInHierarchy)
        {
            InitializeDropdowns();
        }
    }

    private void InitializeDropdowns()
    {
        switch (blockType)
        {
            case BlockType.Unload:
                SetupMachineTypeDropdowns();
                break;

            case BlockType.MoveZone:
                SetupMachineZoneDropdowns();
                break;

            case BlockType.TurnOn:
            case BlockType.TurnOff:
                SetupSingleMachineDropdown();
                break;

            case BlockType.Move:
                SetupSingleMachineDropdown();
                break;

            case BlockType.Dig:
                SetupSingleMachineDropdown(new[] { MachineType.Excavator });
                break;

            case BlockType.Pickup:
                SetupPickupDropdowns();
                break;
        }
    }

    private void SetupMachineTypeDropdowns()
    {
        if (sourceDropdown != null)
        {
            // For source, only allow machines that can unload
            var sourceOptions = new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck }
                .Select(m => m.ToString())
                .ToList();
            sourceDropdown.ClearOptions();
            sourceDropdown.AddOptions(sourceOptions);
        }

        if (targetDropdown != null)
        {
            // For target, only allow machines that can receive unload
            var targetOptions = new[] { MachineType.Loader, MachineType.Truck, MachineType.Truck }
                .Select(m => m.ToString())
                .ToList();
            targetDropdown.ClearOptions();
            targetDropdown.AddOptions(targetOptions);
        }
    }

    private void SetupMachineZoneDropdowns()
    {
        if (sourceDropdown != null)
        {
            // For source, only allow machines that can move zones
            var options = (System.Enum.GetValues(typeof(MachineType)) as MachineType[])
                .Select(m => m.ToString())
                .ToList();
            sourceDropdown.ClearOptions();
            sourceDropdown.AddOptions(options);
        }

        if (targetDropdown != null)
        {
            var zoneData = GetZoneData();
            Console.WriteLine("Zone Data:", zoneData);
            // For target, only allow zones
            targetDropdown.ClearOptions();
            targetDropdown.AddOptions(zoneData);
        }
    }

    private void SetupSingleMachineDropdown(MachineType[] allowedTypes = null)
    {
        if (sourceDropdown != null)
        {
            var options = (allowedTypes ?? System.Enum.GetValues(typeof(MachineType)) as MachineType[])
                .Select(m => m.ToString())
                .ToList();
            sourceDropdown.ClearOptions();
            sourceDropdown.AddOptions(options);
        }
    }

    private void SetupPickupDropdowns()
    {
        if (sourceDropdown != null)
        {
            // Set up machine types that can pickup
            var machineOptions = new[] { MachineType.Loader, MachineType.Excavator }
                .Select(m => m.ToString())
                .ToList();
            sourceDropdown.ClearOptions();
            sourceDropdown.AddOptions(machineOptions);
        }

        if (targetDropdown != null)
        {
            // Set up pickup object types
            var objectOptions = System.Enum.GetValues(typeof(MaterialType))
                .Cast<MaterialType>()
                .Select(o => o.ToString())
                .ToList();
            targetDropdown.ClearOptions();
            targetDropdown.AddOptions(objectOptions);
        }
    }


    // Method to get zone data
    public List<string> GetZoneData()
    {
        if (selectedSiteData == null)
        {
            Debug.LogError("SelectedSiteData is null!");
            return new List<string>();
        }
        ConstructionSiteDataInfo constructionSiteData = LoadJsonData();
        Layout selectedSiteLayout = null;

        if (selectedSiteData.siteIndex >= 0 && selectedSiteData.siteIndex < constructionSiteData.ConstructionSites.Count)
        {
            selectedSiteLayout = constructionSiteData.ConstructionSites[selectedSiteData.siteIndex].Layout;
        }
        else if (!string.IsNullOrEmpty(selectedSiteData.siteName))
        {
            selectedSiteLayout = constructionSiteData.ConstructionSites.Find(site => site.Name == selectedSiteData.siteName).Layout;
        }

        if (selectedSiteLayout == null)
        {
            Debug.LogError("Selected site layout not found in construction site data!");
            return new List<string>();
        }

        return selectedSiteLayout.Zones.Select(z => z.Name).ToList();



    }

    public ConstructionSiteDataInfo LoadJsonData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "siteData.json");
        ConstructionSiteDataInfo constructionSiteData = null;
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            constructionSiteData = JsonUtility.FromJson<ConstructionSiteDataInfo>(jsonText);
            Debug.Log("File loaded successfully.");
        }
        else
        {
            Debug.LogError($"File not found at: {filePath}");
        }

        return constructionSiteData;
    }
    // Method to get current selections
    public (string source, string target) GetCurrentSelections()
    {
        string source = sourceDropdown != null ? sourceDropdown.options[sourceDropdown.value].text : "";
        string target = targetDropdown != null ? targetDropdown.options[targetDropdown.value].text : "";
        return (source, target);
    }
}
