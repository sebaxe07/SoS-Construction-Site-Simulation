using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// @brief Manages the details of a construction zone, including UI interactions.
public class ZoneDetails : MonoBehaviour
{
    // Reference to the UI elements
    public TMP_InputField zoneNameInputField;
    public TMP_Dropdown zoneTypeDropdown;
    public Button createGateButton;
    private ZoneManager zoneManager;

    private ConstructionSiteManager constructionSiteManager;

    private PolygonData zoneData;
    private int zoneIndex;

    /// @brief Initializes the ZoneDetails instance.
    private void Start()
    {
        Debug.Log("ZoneDetails script is attached to " + gameObject.name);
        // Add listener to the OK button
        if (createGateButton != null)
        {
            createGateButton.onClick.AddListener(StartGateDefiniton);
        }
        constructionSiteManager = GameObject.Find("SceneManager").GetComponent<ConstructionSiteManager>();
        zoneNameInputField.text = ""; // Clear the input field
        zoneNameInputField.text = zoneData.Name;
        zoneTypeDropdown.ClearOptions(); // Clear existing options
        zoneTypeDropdown.AddOptions(new List<string> { "Excavation", "Demolition", "Concrete", "Building" });
        // zoneManager.ChangeZoneType(zoneIndex, "Building");
        zoneNameInputField.onValueChanged.AddListener(OnZoneNameChanged);
        SetZoneType("Excavation");
        zoneTypeDropdown.onValueChanged.AddListener(delegate { OnZoneTypeChanged(zoneTypeDropdown); });
    }

    private void OnZoneNameChanged(string name)
    {
        Debug.Log("Zone name changed to " + name);
        zoneData.Name = name;
        // zoneManager.ChangeZoneName(zoneIndex, name);
    }

    private void OnZoneTypeChanged(TMP_Dropdown type)
    {
        Debug.Log("Zone type changed to " + type.options[type.value].text);
        zoneData.Type = type.options[type.value].text;
        // zoneManager.ChangeZoneType(zoneIndex, type.value.ToString());
    }


    public void SetZoneManager(ZoneManager zoneManager)
    {
        this.zoneManager = zoneManager;
    }
    public void SetZoneIndex(int zoneIndex)
    {
        this.zoneIndex = zoneIndex;
    }

    /// @brief Sets the zone data and initializes the UI elements.
    /// @param zoneData The data of the zone to be set.
    public void SetZoneData(PolygonData zoneData)
    {
        this.zoneData = zoneData;

    }

    public void SetZoneType(string type)
    {
        zoneTypeDropdown.value = zoneTypeDropdown.options.FindIndex(option => option.text == type);
        zoneData.Type = type;
    }

    public void SetZoneName(string name)
    {
        zoneNameInputField.text = name;
        zoneData.Name = name;
    }


    private void StartGateDefiniton()
    {
        Debug.Log("Gate definition started for zone " + zoneIndex);
        zoneManager.ChangeZoneColor(zoneIndex, Color.yellow);

        constructionSiteManager.HandleGateSelection(zoneIndex);
    }


}