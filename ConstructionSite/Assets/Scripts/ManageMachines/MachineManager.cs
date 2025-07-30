using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;
using System;

namespace constructionSite.Scripts
{

    [Serializable]
    public class MachineParser
    {
        public int id;
        public string typeId;
        public string name;
        public string manufacturer;
        public string engineType;
        public double fuelConsumption;
        public double maintenanceCost;
        public double tankCapacity;
        public string wheelOrTrackType;
        public double maxSpeed;
        public double averageSpeed;
        public string machineType;
        public int assignedWorkerID;
        public CapacityData capacity;
    }

    [Serializable]
    public class CapacityData
    {
        public double value;
        public string unit;
        public string description;
    }

    /// <summary>
    /// Manages machine inventory and selection across two different canvases:
    /// - Machine List Canvas: Shows owned machines with filtering and sorting
    /// - Choose Machine Canvas: Allows selection of new machines to add
    /// </summary>
    public class MachineManager : MonoBehaviour
    {
        #region Serialized Fields

        // Canvas
        [Header("Canvas References")]
        [SerializeField] private GameObject machineListCanvas;
        [SerializeField] private GameObject chooseMachineCanvas;

        // Input field 
        [Header("UI Input Elements")]
        [SerializeField] private TMP_InputField InputFilterByName;
        [SerializeField] private TMP_InputField InputFilterSearch;

        // Dropdown Menu
        [Header("Dropdown Menus")]
        [SerializeField] private TMP_Dropdown orderByNameDropdown;
        [SerializeField] private TMP_Dropdown filterByTypeDropdown;
        [SerializeField] private TMP_Dropdown filterByStatusDropdown;

        // Slider
        [Header("Sliders")]
        [SerializeField] private Slider vehicleTypeMainPageSlider;

        // Prefab
        [Header("Prefabs")]
        [SerializeField] private GameObject machineListItemPrefab;              // Show owned machines
        [SerializeField] private GameObject chooseMachineItemPrefab;            // Show machines in the platform

        // Container UI
        [Header("Containers")]
        [SerializeField] private Transform listContainerMachineList;            // Contains machineListItemPrefab
        [SerializeField] private Transform listContainerChooseMachine;          // Contains chooseMachineItemPrefab

        #endregion

        #region Private Fields
        // Dropdown Menu options for dynamic changes
        private List<string> movingVehicle = new List<string> { "All Type", "Excavator", "Truck", "Wheel loader" };
        private List<string> steadyVehicle = new List<string> { "All Type", "Crane" };

        // Streaming assets -> JSONs
        private const string MACHINE_LIST_PATH = "machines.json";                     // Path to JSON with all the machines provided

        // Machine Collections
        private List<MachineParser> allMachines;                                            // List of all available machines - read from JSON file
        private List<MachineParser> orderedMachines;                                        // List of all available machines with selected ones on top
        private List<MachineParser> filteredMachines;                                       // List of all available machines filtered by input field
        private List<MachineParser> tmpSelectedMachines = new List<MachineParser>();        // List of machines selected by the user

        private Dictionary<string, List<Machine>> machineOwned = new Dictionary<string, List<Machine>>();               // Dictionary of user machines
        private Dictionary<string, List<Machine>> filteredMachinesOwned = new Dictionary<string, List<Machine>>();      // Dictionary of user machines filtered

        #endregion

        public ProjectManager projectManager;

        #region Unity Lifecycle
        // Start function called as the Scene is loaded
        public void Start()
        {

            // TODO: delete instantiation of ProjectManager and put it at the start of the game
            if (ProjectManager.Instance == null)
            {
                Debug.Log("Creating instance of ProjectManager");
                projectManager.Initialize(1, "Construction Project");
            }
            else
            {
                projectManager = ProjectManager.Instance;

            }

            // Set the fist Canvas to be displayed
            machineListCanvas.SetActive(true);
            chooseMachineCanvas.SetActive(false);

            // Initialize slider and dropdown menu
            vehicleTypeMainPageSlider.value = 0;
            UpdateDropdownVehicleType();

            // Load machines from JSON file
            LoadMachines();
        }

        #endregion

        #region Owned Machine List Canvas - Public Methods
        // On Slider Change Value -> Update the options displayed in dropdowns menu
        public void UpdateDropdownVehicleType()
        {
            // Point to All Type option
            filterByTypeDropdown.value = 0;

            // Clear options displayed
            filterByTypeDropdown.ClearOptions();

            // Get slider value and update the filter options displayed
            if (vehicleTypeMainPageSlider.value == 0)
            {
                filterByTypeDropdown.AddOptions(movingVehicle);         // Show all Moving Vehicle 
            }
            else
            {
                filterByTypeDropdown.AddOptions(steadyVehicle);         // Show all Steady Vehicle
            }

            // Point to All Type option
            filterByTypeDropdown.value = 0;

            // If the menu changed then update the filter applied
            ApplyFilterToOwnedMachines();
        }

        // On Filter Change Value -> Update machine list based on filters defined
        public void ApplyFilterToOwnedMachines()
        {
            // Get the dictionary of owned machines
            filteredMachinesOwned = new Dictionary<string, List<Machine>>(machineOwned);

            ApplyStatusFilter();    // Filter based on Status value     (Available / Unavailable)
            ApplyTypeFilter();      // Filter by machine type           (Based on dropdown menu value)
            ApplyNameOrder();       // Order by alphabet                (A-Z / Z-A)
            ApplyNameFilter();      // Search based on inserted pattern (Name / Manifacturer / Type) 

            // Refresh UI
            RefreshMachineListView(listContainerMachineList, filteredMachinesOwned);
        }

        // On button click -> Remove a machine from the list based on its MachineID 
        public void DeleteSingleMachine(string key, int id)
        {
            List<Machine> machineList = machineOwned[key];

            // Find the first machine that has the id and delete it
            foreach (Machine machine in machineList)
            {
                if (machine.MachineID == id)
                {
                    machineList.Remove(machine);
                    ProjectManager.Instance.RemoveMachine(id);
                    break;
                }
            }

            // If the list is empty, remove the key from the dictionary
            if (machineList.Count == 0 || machineList == null)
            {
                machineOwned.Remove(key);
            }
            // If a deletion happened then apply the filters
            ApplyFilterToOwnedMachines();
        }

        #endregion

        #region Owned Machine List Canvas - Private Methods
        // Refresh the owned machines
        private void RefreshMachineListView(Transform container, Dictionary<string, List<Machine>> machineTable)
        {
            ClearMachineListView(container);               // Remove all the current machines displayed
            DisplayOwnedMachineList(machineTable);         // Display the machines dictionary
        }

        // Display all the machines in the UI
        private void DisplayOwnedMachineList(Dictionary<string, List<Machine>> machines)
        {
            // Check if the dictionary is empty or null
            if (machines != null || machines.Count != 0)
            {
                // Iterate on each key of the dictionary
                foreach (KeyValuePair<string, List<Machine>> entry in machines)
                {
                    List<Machine> machineList = entry.Value;
                    // Create a UI element for each element in the dictionary
                    CreateOwnedMachineItemUI(machineList[0], machineList.Count(m => m.IsAvailable), machineList.Count);
                }
            }
        }

        // Add a new machine to the UI using a Prefab
        private void CreateOwnedMachineItemUI(Machine machine, int quantityAvailable, int listLength)
        {
            // Instantiate a new prefab
            GameObject machineItem = Instantiate(machineListItemPrefab, listContainerMachineList);

            // Find all the field in the prefab
            TMP_Text nameText = machineItem.transform.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text typeText = machineItem.transform.Find("TypeText").GetComponent<TMP_Text>();
            TMP_Text statusText = machineItem.transform.Find("StatusText").GetComponent<TMP_Text>();
            Button deleteButton = machineItem.transform.Find("DeleteButton").GetComponent<Button>();

            // Update all the field in the prefab
            nameText.text = machine.Name;
            typeText.text = machine.MachineType;
            statusText.text = quantityAvailable + " / " + listLength;

            // Add a listener to the button to delete a machine
            deleteButton.onClick.AddListener(() =>
            {
                DeleteSingleMachine(machine.TypeId, machine.MachineID);
                SaveMachinesToFile();
            });
        }

        // Filter owned machines based on the availability
        private void ApplyStatusFilter()
        {
            var tmpFilteredMachinesOwned = new Dictionary<string, List<Machine>>();

            foreach (var m in filteredMachinesOwned)
            {
                List<Machine> filteredMachines = m.Value;           // Default is the full list

                switch (filterByStatusDropdown.value)
                {
                    case 1: // Available -> At least a machine is available (x/N | x <= N)
                        filteredMachines = m.Value.Where(machine => machine.IsAvailable).ToList();
                        break;
                    case 2: // Unavailable -> No machines are available (0/N)
                        filteredMachines = m.Value.Where(machine => !machine.IsAvailable).ToList();
                        break;
                }

                // Update the temporary dictionary with the modification done
                UpdateFilteredMachines(tmpFilteredMachinesOwned, m.Key, filteredMachines);
            }

            // Update the dictionary that as to be displayed
            filteredMachinesOwned = tmpFilteredMachinesOwned;
        }

        // Filter owned machines based on the selected type
        private void ApplyTypeFilter()
        {
            // Get the selected type from the dropdown
            string selectedType = GetDropdownMachineType(filterByTypeDropdown.value);

            if (selectedType != "all type")
            {
                var tmpFilteredMachinesOwned = new Dictionary<string, List<Machine>>();

                foreach (var m in filteredMachinesOwned)
                {
                    // Get all the machines that has the same type defined by the dropdown
                    var filteredMachines = m.Value.Where(machine => machine.MachineType.Equals(selectedType, StringComparison.OrdinalIgnoreCase)).ToList();

                    // Update the temporary dictionary with the modification done
                    UpdateFilteredMachines(tmpFilteredMachinesOwned, m.Key, filteredMachines);
                }

                // Update the dictionary that as to be displayed
                filteredMachinesOwned = tmpFilteredMachinesOwned;
            }
        }

        // Update dictionary with a new list
        private void UpdateFilteredMachines(Dictionary<string, List<Machine>> dictionary, string key, List<Machine> machines)
        {
            if (machines.Count > 0)
                dictionary[key] = machines;
            else
                dictionary.Remove(key);
        }

        // Get the dropdown value and then change it into the corresponding string
        private string GetDropdownMachineType(int dropdownValue)
        {
            return filterByTypeDropdown.options[dropdownValue].text.ToLower();
        }

        // Order machines Alphabetically (From A to Z / From Z to A)
        private void ApplyNameOrder()
        {
            Dictionary<string, List<Machine>> tmpFilteredMachinesOwned = new Dictionary<string, List<Machine>>(filteredMachinesOwned);

            // Iterate through each key and filter its associated list of machines by type
            switch (orderByNameDropdown.value)
            {
                case 1: // From A to Z
                    tmpFilteredMachinesOwned = tmpFilteredMachinesOwned.OrderBy(kvp => kvp.Value.First().Name).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;

                case 2: // From Z to A
                    tmpFilteredMachinesOwned = tmpFilteredMachinesOwned.OrderByDescending(kvp => kvp.Value.First().Name).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    break;

                default: // As they were inserted
                    break;
            }

            // Update the dictionary that as to be displayed
            filteredMachinesOwned = tmpFilteredMachinesOwned;
        }

        // Filter machines based on the pattern inserted by user in the search bar
        private void ApplyNameFilter()
        {
            Dictionary<string, List<Machine>> tmpFilteredMachinesOwned = new Dictionary<string, List<Machine>>(filteredMachinesOwned);

            if (!string.IsNullOrEmpty(InputFilterByName.text))
            {
                // Filter machine list based on the corresponding of the pattern 
                tmpFilteredMachinesOwned = tmpFilteredMachinesOwned.Where(kvp => kvp.Value.Count > 0 &&
                                                                            (kvp.Value.First().Name.Contains(InputFilterByName.text, StringComparison.OrdinalIgnoreCase) ||
                                                                            kvp.Value.First().Manufacturer.Contains(InputFilterByName.text, StringComparison.OrdinalIgnoreCase) ||
                                                                            kvp.Value.First().MachineType.Contains(InputFilterByName.text, StringComparison.OrdinalIgnoreCase)))
                                                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            // Update the dictionary that as to be displayed
            filteredMachinesOwned = tmpFilteredMachinesOwned;
        }

        #endregion

        #region Choose Machine List Canvas - Public Methods
        // On button click -> change active canvas to display the Add New Machine Page
        public void OpenChooseMachineCanvas()
        {
            // Change active canvas
            chooseMachineCanvas.SetActive(true);
            machineListCanvas.SetActive(false);

            // Read JSON file from path
            string json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, MACHINE_LIST_PATH));

            // Deserialize JSON file into a list of parsed machines using the Wrapper class
            allMachines = JsonUtility.FromJson<Wrapper>(WrapArray(json)).machines;
            orderedMachines = new List<MachineParser>(allMachines);

            // Display all the machines in the UI
            DisplayAllMachineList(orderedMachines);
        }

        // Apply the search filter to the machine list
        public void ApplySearchFilter()
        {
            string searchQuery = InputFilterSearch.text.ToLower();

            // Filter machines based on the pattern inserted by the user
            filteredMachines = string.IsNullOrEmpty(searchQuery)
                                ? new List<MachineParser>(orderedMachines)
                                : orderedMachines.Where(m =>
                                    m.name.ToLower().Contains(searchQuery) ||
                                    m.manufacturer.ToLower().Contains(searchQuery) ||
                                    m.machineType.ToLower().Contains(searchQuery))
                                .ToList();

            // Refresh the UI
            RefreshAllMachineListView(listContainerChooseMachine, filteredMachines);
        }

        // On button click -> Save new machine in the machines dictionary 
        public void SaveMachines()
        {
            // Parse selected machines into a dictionary
            CreateMachineMapFromParser(tmpSelectedMachines);

            // Refresh owned machine view to display the selected machines
            RefreshMachineListView(listContainerMachineList, machineOwned);

            // Apply filter if any defined before
            ApplyFilterToOwnedMachines();

            // Clear all fields in the Choose Machine Page
            allMachines.Clear();
            orderedMachines.Clear();
            if (filteredMachines != null && filteredMachines.Count > 0)
                filteredMachines.Clear();
            tmpSelectedMachines.Clear();
            InputFilterSearch.text = "";

            ClearMachineListView(listContainerChooseMachine);

            // Reset the active canvas to machine list view
            chooseMachineCanvas.SetActive(false);
            machineListCanvas.SetActive(true);
            SaveMachinesToFile();
        }

        // On button click -> Go back to the machine list view
        public void BackToAllMachines()
        {
            // Change active canvas
            machineListCanvas.SetActive(true);
            chooseMachineCanvas.SetActive(false);

            // Clear all fields in the Choose Machine Page
            allMachines.Clear();
            orderedMachines.Clear();
            if (filteredMachines != null && filteredMachines.Count > 0)
                filteredMachines.Clear();
            tmpSelectedMachines.Clear();
            InputFilterSearch.text = "";

            // Clear list container
            ClearMachineListView(listContainerChooseMachine);
        }

        #endregion

        #region Choose Machine List Canvas - Private Methods
        // Refresh the displayed machine list
        private void RefreshAllMachineListView(Transform container, List<MachineParser> machineList)
        {
            ClearMachineListView(container);            // Remove all the machines displayed
            DisplayAllMachineList(machineList);         // Update the machines to be displayed
        }

        // Display all the machines available in the platform
        private void DisplayAllMachineList(List<MachineParser> machines)
        {
            foreach (MachineParser m in machines)
            {
                CreateMachineItemUI(m);
            }
        }

        // Create a new item that show the machine data
        private void CreateMachineItemUI(MachineParser m)
        {
            // Instantiate a new prefab 
            GameObject machineItem = Instantiate(chooseMachineItemPrefab, listContainerChooseMachine);

            // Define all the element of the prefab
            Toggle machineSelected = machineItem.transform.Find("Select").GetComponent<Toggle>();
            TMP_Text machineName = machineItem.transform.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text machineType = machineItem.transform.Find("TypeText").GetComponent<TMP_Text>();
            TMP_Text manufacturer = machineItem.transform.Find("ManifacturerText").GetComponent<TMP_Text>();
            Button removeButton = machineItem.transform.Find("RemoveButton").GetComponent<Button>();
            TMP_Text count = machineItem.transform.Find("MachineNumberText").GetComponent<TMP_Text>();
            Button addButton = machineItem.transform.Find("AddButton").GetComponent<Button>();

            // Update Text fields in the prefab
            machineName.text = m.name;
            machineType.text = m.machineType;
            manufacturer.text = m.manufacturer;
            count.text = "1";

            // Remove from visualization buttons and count
            addButton.gameObject.SetActive(false);
            removeButton.gameObject.SetActive(false);
            count.gameObject.SetActive(false);

            // Set Toggle ON if a machine is in the selected list
            if (tmpSelectedMachines != null && tmpSelectedMachines.Count > 0 && tmpSelectedMachines.Contains(m))
            {
                // Update toggle
                machineSelected.isOn = true;
                // Update counter
                int counter = tmpSelectedMachines.Count(mSel => mSel == m);
                count.text = counter.ToString();
                // Update background color
                machineItem.GetComponent<Image>().color = Color.green;

                // Make buttons and count visible
                addButton.gameObject.SetActive(true);
                removeButton.gameObject.SetActive(true);
                count.gameObject.SetActive(true);

                // Add increment and decrement handler
                addButton.onClick.AddListener(() => UpdateMachineCount(count, m, true));
                removeButton.onClick.AddListener(() => UpdateMachineCount(count, m, false));
            }

            // Add listener to toggle on value change action
            machineSelected.onValueChanged.AddListener(isOn => HandleMachineSelectionChange(m, isOn));
        }

        // Increment - Decrement counter of the selected machine
        private void UpdateMachineCount(TMP_Text countText, MachineParser m, bool increment)
        {
            // Increment or decrement counter
            int count = int.Parse(countText.text);
            count = increment ? count + 1 : Math.Max(1, count - 1);
            countText.text = count.ToString();

            if (increment)
            {
                // Add a new machine in the selected machine list
                AddMachineToSelectedList(m);
            }
            else
            {
                if (count > 1)
                {
                    // Remove a machine in the selected machine list
                    RemoveMachineFromSelectedList(m);
                }
            }
        }

        // Handle the change of machine selection based on toggle value
        private void HandleMachineSelectionChange(MachineParser machine, bool isSelected)
        {
            if (isSelected)
            {
                AddMachineToSelectedList(machine);
                ReorderMachineList(machine, true);
            }
            else
            {
                RemoveAllMachineFromSelectedList(machine);
                ReorderMachineList(machine, false);
            }
        }

        // Add machine to temporary list of selected machines
        private void AddMachineToSelectedList(MachineParser machine)
        {
            tmpSelectedMachines.Add(machine);
        }

        // Remove ONE instance of a specific machine from the selected machine list, based on remove button
        private void RemoveMachineFromSelectedList(MachineParser machine)
        {
            for (int i = 0; i < tmpSelectedMachines.Count; i++)
            {
                if (tmpSelectedMachines[i].id == machine.id)
                {
                    tmpSelectedMachines.RemoveAt(i);
                    break;
                }
            }
        }

        // Remove ALL the machine from temporary list of selected machines, based on toggle value
        private void RemoveAllMachineFromSelectedList(MachineParser machine)
        {
            tmpSelectedMachines.RemoveAll(m => m.id == machine.id);
        }

        // Order machines in the UI when selected or not
        private void ReorderMachineList(MachineParser machine, bool isMachineSelected)
        {
            // Remove the machine from its current position
            orderedMachines.RemoveAll(m => m.typeId == machine.typeId);

            if (isMachineSelected)
            {
                // Move the machine to the top
                orderedMachines.Insert(0, machine);
            }
            else
            {
                // Put the machine back in the list right after the selected machines
                int lastSelectedIndex = -1;
                int index = -1;

                // FInd the position where to put the machine
                foreach (MachineParser selectedMachine in tmpSelectedMachines)
                {
                    index = orderedMachines.IndexOf(selectedMachine);
                    if (index > lastSelectedIndex)
                    {
                        lastSelectedIndex = index;
                    }
                }

                // Insert the machine in the position found
                orderedMachines.Insert(lastSelectedIndex + 1, machine);
            }

            // Refresh UI
            RefreshAllMachineListView(listContainerChooseMachine, orderedMachines);
        }

        // Convert the selected machines into a dictionary when the user decide to confirm the selection done
        private void CreateMachineMapFromParser(List<MachineParser> machineParsedList)
        {
            foreach (MachineParser m in machineParsedList)
            {
                // Create a new machine instance for each machine in the list
                Machine newMachine = CreateMachine(m, HelperMachines.GenerateMachineUniqueId(), -1);

                if (newMachine != null)
                {
                    // Add to ProjectManager's machines owned list
                    ProjectManager.Instance.MachinesOwned.Add(newMachine);

                    // Also maintain the dictionary structure
                    if (machineOwned.ContainsKey(m.typeId))
                    {
                        machineOwned[m.typeId].Add(newMachine);
                    }
                    else
                    {
                        machineOwned.Add(m.typeId, new List<Machine> { newMachine });
                    }
                }
            }
        }

        // Create a Machine based on parsed data from the JSON
        public static Machine CreateMachine(MachineParser m)
        {
            // Based on the machine type instantiate a different Machine
            switch (m.machineType)
            {
                case "excavator":
                    return new Excavator(m.id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                        m.wheelOrTrackType, m.tankCapacity, m.maxSpeed, m.averageSpeed, m.capacity.value, m.assignedWorkerID);
                case "truck":
                    return new Truck(m.id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                    m.wheelOrTrackType, m.tankCapacity, m.maxSpeed, m.averageSpeed, m.capacity.value, m.assignedWorkerID);
                case "crane":
                    return new Crane(m.id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                    true, m.capacity.value, m.assignedWorkerID);
                case "wheel loader":
                default:
                    return null;
            }
        }

        public static Machine CreateMachine(MachineParser m, int id, int assignedWorkerID)
        {
            // Based on the machine type instantiate a different Machine
            switch (m.machineType)
            {
                case "excavator":
                    return new Excavator(id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                        m.wheelOrTrackType, m.tankCapacity, m.maxSpeed, m.averageSpeed, m.capacity.value, assignedWorkerID);
                case "truck":
                    return new Truck(id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                    m.wheelOrTrackType, m.tankCapacity, m.maxSpeed, m.averageSpeed, m.capacity.value, assignedWorkerID);
                case "crane":
                    return new Crane(id, m.typeId, m.name, m.manufacturer, m.machineType, m.engineType, m.fuelConsumption, m.maintenanceCost,
                                    true, m.capacity.value, assignedWorkerID);
                case "wheel loader":
                default:
                    return null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Load the machines from the JSON file
        /// </summary>
        /// 
        public static void LoadMachinesStatic()
        {
            try
            {
                string jsonPath = Path.Combine(Application.dataPath, "machines.json");
                string jsonContent = File.ReadAllText(jsonPath);

                var wrapper = JsonUtility.FromJson<Wrapper>(jsonContent);
                List<Machine> machinesOwned = ProjectManager.Instance.MachinesOwned;
                machinesOwned.Clear();

                foreach (var machineParser in wrapper.machines)
                {
                    Machine machine = CreateMachine(machineParser);
                    Debug.Log("Loading machine: " + machineParser.id);
                    if (machine != null)
                    {
                        machinesOwned.Add(machine);
                    }
                }
                Debug.Log($"Loaded {machinesOwned.Count} machines from JSON");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading machines: {e.Message}");
            }
        }

        public void LoadMachines()
        {
            try
            {
                string jsonPath = Path.Combine(Application.dataPath, "machines.json");
                string jsonContent = File.ReadAllText(jsonPath);

                var wrapper = JsonUtility.FromJson<Wrapper>(jsonContent);
                List<Machine> machinesOwned = ProjectManager.Instance.MachinesOwned;
                machinesOwned.Clear();

                // Clear the local dictionary as well
                this.machineOwned.Clear();

                foreach (var machineParser in wrapper.machines)
                {
                    Debug.Log("Loading machine: " + machineParser.id);
                    Machine machine = CreateMachine(machineParser);
                    if (machine != null)
                    {
                        // Add to ProjectManager's machines owned list
                        machinesOwned.Add(machine);

                        // Add to local dictionary
                        if (this.machineOwned.ContainsKey(machine.TypeId))
                        {
                            this.machineOwned[machine.TypeId].Add(machine);
                        }
                        else
                        {
                            this.machineOwned.Add(machine.TypeId, new List<Machine> { machine });
                        }
                    }
                }

                // Refresh the UI after loading
                RefreshMachineListView(listContainerMachineList, this.machineOwned);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading machines: {e.Message}");
            }
        }


        public static void SaveMachinesToFile()
        {
            try
            {
                var machineJsonList = new List<MachineParser>();
                List<Machine> machinesOwned = ProjectManager.Instance.MachinesOwned;

                foreach (var machine in machinesOwned)
                {
                    var machineJson = new MachineParser
                    {
                        id = machine.MachineID,
                        typeId = machine.TypeId,
                        name = machine.Name,
                        manufacturer = machine.Manufacturer,
                        engineType = machine.EngineType,
                        fuelConsumption = machine.EnergyConsumption,
                        maintenanceCost = machine.MaintenanceCost,
                        machineType = machine.MachineType.ToLower(),
                        assignedWorkerID = machine.AssignedWorkerID
                    };

                    if (machine is MovingVehicle movingVehicle)
                    {
                        machineJson.wheelOrTrackType = movingVehicle.WheelType;
                        machineJson.tankCapacity = movingVehicle.FuelCapacity;
                        machineJson.maxSpeed = movingVehicle.MaxSpeed;
                        machineJson.averageSpeed = movingVehicle.AvgSpeed;
                    }

                    if (machine is Excavator excavator)
                    {
                        machineJson.capacity = new CapacityData
                        {
                            value = excavator.BucketCapacity,
                            unit = "m3",
                            description = "Bucket Capacity"
                        };
                    }
                    else if (machine is Truck truck)
                    {
                        machineJson.capacity = new CapacityData
                        {
                            value = truck.LoadCapacity,
                            unit = "tons",
                            description = "Load Capacity"
                        };
                    }
                    else if (machine is Crane crane)
                    {
                        machineJson.capacity = new CapacityData
                        {
                            value = crane.LoadMaxWeight,
                            unit = "tons",
                            description = "Load Max Weight"
                        };
                    }

                    machineJsonList.Add(machineJson);
                }

                var wrapper = new Wrapper { machines = machineJsonList };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(Path.Combine(Application.dataPath, "machines.json"), json);
                Debug.Log("Machines saved successfully to file");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving machines: {e.Message}");
            }
        }

        // Delete all machines from the UI given the container
        private void ClearMachineListView(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        // Wrapper helper to deserialize JSON files 
        private string WrapArray(string jsonArray)
        {
            return "{\"machines\":" + jsonArray + "}";
        }

        internal static IEnumerable<object> GetMachinesOfType(MachineType type)
        {
            throw new NotImplementedException();
        }

        [Serializable]
        private class Wrapper
        {
            public List<MachineParser> machines;
        }

        #endregion

    }
}