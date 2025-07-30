using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// @brief Manages the view and interaction with construction site cards, including pagination, search, and sorting.
public class ViewConstructionSites : MonoBehaviour
{
    // Current page and items per page for the construction site cards pagination
    private int currentPage = 1;
    private int itemsPerPage = 5;

    // List of all the construction sites
    private List<ConstructionSite> filteredSites;
    private ConstructionSiteDataInfo constructionSiteData;
    public SelectedSiteData selectedSiteData;

    //Pagination buttons
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageDisplay;

    // Site search input field
    public TMP_InputField searchInput;

    // Site sort by dropdown
    public TMP_Dropdown sortByDropdown;

    // Card prefab to display basic construction site information
    public GameObject cardPrefab;

    // Card grid that displays all of the construction site cards
    public Transform cardGrid;

    /// @brief Start is called before the first frame update.
    void Start()
    {
        if (sortByDropdown == null)
        {
            Debug.LogError("TMP_Dropdown is not assigned in the Inspector.");
            return;
        }

        // Load construction sites from JSON file
        // constructionSites = ConstructionSiteLoader.LoadSitesFromJson("Assets/SiteList.json");
        LoadJsonData();
        Debug.Log(constructionSiteData);
        foreach (var site in constructionSiteData.ConstructionSites)
                {
                    Debug.Log($"Name: {site.Name}");
                    Debug.Log($"Address: {site.Address}");
                    Debug.Log($"City: {site.City}");
                    Debug.Log($"State: {site.State}");
                    Debug.Log($"Zip: {site.Zip}");
                    Debug.Log($"Phone: {site.Phone}");
                    Debug.Log($"Dimensions: {site.Dimensions}");
                    Debug.Log("----------------------------");
                }

        // Apply the filters to the construction sites
        ApplyFilters();

        // Fill the card grid with the construction sites
        UpdatePage();

        // Add options to the dropdown
        sortByDropdown.ClearOptions();
        sortByDropdown.AddOptions(new List<string> { "Name", "Status", "Size of Area" });
    }

    private void LoadJsonData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "siteData.json");

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
    }

    /// @brief Update is called once per frame.
    void Update()
    {
        // Handle arrow key pagination
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPage();  // Show previous page
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPage();  // Show the next page
        }
    }

    /// @brief Apply filters to the construction sites based on search input and sort selection.
    public void ApplyFilters()
    {
        // Get the list of all construction sites in the system
        filteredSites = new List<ConstructionSite>(constructionSiteData.ConstructionSites);

        ApplySearchFilter(); // Show search results
        ApplySort(); // Show sorted results

        // Reset the pagination when applying sort or search
        currentPage = 1;

        // Update UI to display the filtered list
        UpdatePage();
    }

    /// @brief Filter construction sites based on search input.
    private void ApplySearchFilter()
    {
        if (!string.IsNullOrEmpty(searchInput.text))
        {
            filteredSites = filteredSites.Where(c => c.Name.StartsWith(searchInput.text, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// @brief Sort construction sites based on selected dropdown value.
    private void ApplySort()
    {
        switch (sortByDropdown.value)
        {
            case 0: // Sort by start date
                filteredSites = filteredSites.OrderBy(c => c.Name).ToList();
                break;
            case 1: // Sort by status
                filteredSites = filteredSites.OrderBy(c => GetStatusOrder(c.State)).ToList();
                break;
            case 2: // Sort by the area of the site
                filteredSites = filteredSites
                .OrderByDescending(c => ParseDimension(c.Dimensions))
                .ToList();
                break;
            default: // Default is by start date
                filteredSites = filteredSites.OrderBy(c => c.Name).ToList();
                break;
        }
    }

    private double ParseDimension(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
            return 0;

        // Split the string by space and take the first part
        string[] parts = dimension.Split(' ');
        if (parts.Length > 0 && double.TryParse(parts[0], out double result))
        {
            return result;
        }

        // If parsing fails, return 0
        return 0;
    }

    /// @brief Assign a numerical value to the status of the construction site for sorting purposes.
    /// @param status The status of the construction site.
    /// @return An integer representing the order of the status.
    private int GetStatusOrder(string status)
    {
        switch (status)
        {
            case "Not Started": return 0;
            case "In Progress": return 1;
            case "Completed": return 2;
            default: return 3; // Fallback if status is unknown
        }
    }

    /// @brief Add a new construction site to the UI using a card Prefab.
    /// @param constructionSite The construction site to be added.
    private void AddConstructionSiteLoader(ConstructionSite constructionSite, int siteIndex)
    {
        // Instantiate a new prefab
        GameObject constructionSiteCard = Instantiate(cardPrefab, cardGrid);

        // Find all the fields in the prefab
        TMP_Text nameLabel = constructionSiteCard.transform.Find("SiteName").GetComponent<TMP_Text>();
        TMP_Text addressLabel = constructionSiteCard.transform.Find("SiteAddress").GetComponent<TMP_Text>();
        TMP_Text statusLabel = constructionSiteCard.transform.Find("SiteStatus").GetComponent<TMP_Text>();
        Image siteImage = constructionSiteCard.transform.Find("Image").GetComponent<Image>();

        // Find all the more info panel fields in the prefab
        TMP_Text siteIDLabel = constructionSiteCard.transform.Find("MoreInfoPanel/SiteID").GetComponent<TMP_Text>();
        TMP_Text moreNameLabel = constructionSiteCard.transform.Find("MoreInfoPanel/SiteName").GetComponent<TMP_Text>();
        TMP_Text moreAddressLabel = constructionSiteCard.transform.Find("MoreInfoPanel/SiteAddress").GetComponent<TMP_Text>();
        TMP_Text moreStatusLabel = constructionSiteCard.transform.Find("MoreInfoPanel/SiteStatus").GetComponent<TMP_Text>();
        TMP_Text areaLabel = constructionSiteCard.transform.Find("MoreInfoPanel/SiteArea").GetComponent<TMP_Text>();
        TMP_Text startDateLabel = constructionSiteCard.transform.Find("MoreInfoPanel/StartDate").GetComponent<TMP_Text>();
        TMP_Text endDateLabel = constructionSiteCard.transform.Find("MoreInfoPanel/EndDate").GetComponent<TMP_Text>();

        // Add on hover pop up for the more info panel
        GameObject moreInfoPanel = constructionSiteCard.transform.Find("MoreInfoPanel").gameObject;
        moreInfoPanel.SetActive(false);
        AddHoverEvents(constructionSiteCard, moreInfoPanel);

        // Button for redirecting to the see more page
        Button seeMoreButton = constructionSiteCard.transform.Find("OpenSiteButton").GetComponent<Button>();
        Button seeMoreButton2 = constructionSiteCard.transform.Find("MoreInfoPanel/OpenSiteButton").GetComponent<Button>();

        if (seeMoreButton == null)
        {
            Debug.LogError("OpenSiteButton not found in prefab.");
        }

        if (seeMoreButton2 == null)
        {
            Debug.LogError("MoreInfoPanel/OpenSiteButton not found in prefab.");
        }

        // Update all the fields in the prefab
        nameLabel.text = $"Name: {constructionSite.Name}";
        // Shorten the address string if it's too long to fit 
        addressLabel.text = $"Address: {(constructionSite.Address.Length <= 40 ? constructionSite.Address : constructionSite.Address.Substring(0, 40) + "...")}";
        statusLabel.text = $"State: {constructionSite.State}";

        // Update all of the more info panel fields
        // siteIDLabel.text = $"Site ID: {constructionSite.SiteID}";
        siteIDLabel.text = $"City: {constructionSite.City}";
        moreNameLabel.text = $"Name: {constructionSite.Name}";
        moreAddressLabel.text = $"Address: {constructionSite.Address}";
        moreStatusLabel.text = $"State: {constructionSite.State}";
        areaLabel.text = $"Dimensions: {constructionSite.Dimensions}";
        startDateLabel.text = $"Zip: {constructionSite.Zip}";
        endDateLabel.text = $"Phone: {constructionSite.Phone}";

        // startDateLabel.text = $"Start Date: {(constructionSite.StartDate != DateTime.MinValue ? constructionSite.StartDate.ToString("dd/MM/yyyy") : "Not Available")}";
        // endDateLabel.text = $"End Date: {(constructionSite.EndDate != DateTime.MinValue ? constructionSite.EndDate.ToString("dd/MM/yyyy") : "Not Available")}";

        // Load and set the site image (For now this is just loading the picture of the construction site provided by the client for every card)
        // In the future, we can have a default image for all construction sites and a way to upload or generate a custom image for each site
        string imageName = "SinCon_Site_BG";
        Sprite imageSprite = Resources.Load<Sprite>(imageName);
        if (imageSprite != null)
        {
            siteImage.sprite = imageSprite;
        }
        else
        {
            Debug.LogWarning($"Image for site {constructionSite.Name} not found!");
        }

        seeMoreButton.onClick.AddListener(() =>
        {
            // selectedSiteData = constructionSiteData.ConstructionSites[selectedSiteData.siteIndex];
            HandleSiteSelection(siteIndex);
        });

        seeMoreButton2.onClick.AddListener(() =>
        {
            HandleSiteSelection(siteIndex);
        });

        // // Link to the button the open construction site page function with the construction site ID
        // seeMoreButton.onClick.AddListener(() =>
        // {
        //     // Assign the site to the singleton
        //     // ActiveSiteManager.Instance.CurrentSite = constructionSite;

        //     // Find the SceneManager object in the scene and call the LoadScene method
        //     GameObject sceneManager = GameObject.Find("SceneManager");
        //     if (sceneManager != null)
        //     {
        //         SceneLoader loader = sceneManager.GetComponent<SceneLoader>();
        //         loader.LoadScene("OpenConstructionSite");
        //     }
        //     else
        //     {
        //         Debug.LogError("SceneManager object not found in the scene.");
        //     }
            
        // });

        // seeMoreButton2.onClick.AddListener(() =>
        // {


        //     // Assign the site to the singleton
        //     ActiveSiteManager.Instance.CurrentSite = constructionSite;

        //     // Find the SceneManager object in the scene and call the LoadScene method
        //     GameObject sceneManager = GameObject.Find("SceneManager");
        //     if (sceneManager != null)
        //     {
        //         SceneLoader loader = sceneManager.GetComponent<SceneLoader>();
        //         loader.LoadScene("OpenConstructionSite");
        //     }
        //     else
        //     {
        //         Debug.LogError("SceneManager object not found in the scene.");
        //     }
        // });
    }


    private void HandleSiteSelection(int siteIndex)
    {
        // Assign the selected index to the ScriptableObject
        selectedSiteData.siteIndex = siteIndex;

        // Load the next scene
        SceneManager.LoadScene("OpenConstructionSite");
    }
    

    public void LoadZonePopulatorScene(int siteIndex)
    {
        selectedSiteData.siteIndex = siteIndex;
        selectedSiteData.siteName = ""; // Clear the name if using index

    }

    public void LoadZonePopulatorScene(string siteName)
    {
        selectedSiteData.siteIndex = -1; // Clear the index if using name
        selectedSiteData.siteName = siteName;

    }


    /// @brief Add hover events to the card to show more information when hovering over it.
    /// @param card The card GameObject.
    /// @param moreInfoPanel The more info panel GameObject.
    private void AddHoverEvents(GameObject card, GameObject moreInfoPanel)
    {
        EventTrigger trigger = card.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = card.AddComponent<EventTrigger>();
        }

        // Add pointer enter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener((data) => { moreInfoPanel.SetActive(true); });
        trigger.triggers.Add(pointerEnter);

        // Add pointer exit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        pointerExit.callback.AddListener((data) => { moreInfoPanel.SetActive(false); });
        trigger.triggers.Add(pointerExit);
    }
    

    /// @brief Show the next page of construction site cards.
    public void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)filteredSites.Count / itemsPerPage);
        if (currentPage < totalPages)
        {
            currentPage++;
            UpdatePage();
        }
    }

    /// @brief Show the previous page of construction site cards.
    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePage();
        }
    }

    /// @brief Update the page display and enable/disable pagination buttons.
    private void UpdatePage()
    {
        // Clear existing items in container
        foreach (Transform child in cardGrid)
        {
            Destroy(child.gameObject);
        }

        // Load items for current page
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredSites.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int originalIndex = constructionSiteData.ConstructionSites.IndexOf(filteredSites[i]);

            // Pass the original index to AddConstructionSiteLoader
            AddConstructionSiteLoader(filteredSites[i], originalIndex);
        }

        // Update page display
        int totalPages = Mathf.CeilToInt((float)filteredSites.Count / itemsPerPage);
        pageDisplay.text = $"{currentPage}/{totalPages}";

        // Enable or disable buttons
        prevButton.interactable = currentPage > 1;
        nextButton.interactable = currentPage < totalPages;
    }
}
