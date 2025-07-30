using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace constructionSite.Scripts
{
    public class UIManagerConstellations : MonoBehaviour
    {

        [Header("Constellation Save UI")]
        private TMP_InputField constellationNameInput;

        public GameObject constellationPrefab;

        [Header("UI Elements")]
        private GameObject constellationPage;
        public GameObject constellationsListPanel;
        public Button createConstellationButton;
        public GameObject mainCanvas;
        public GameObject popupPrefab;
        public GameObject assignWorkerPopupPrefab;
        private GameObject availabilitiesListPanel;
        private GameObject selectionsListPanel;
        private Button cancelButton;
        private Button saveButton;
        private Slider machineWorkerToggle;



        [Header("Prefab")]
        public GameObject workerPrefab;
        public GameObject selectionPrefab;

        [Header("Worker Data")]
        private List<Worker> _availableWorkers;
        private List<Worker> _selectedWorkers;

        [Header("Machine Data")]
        private Dictionary<string, List<Machine>> _machinesByTypeAvailable;
        private Dictionary<string, List<Machine>> _machinesByTypeSelected;

        [Header("Class Data")]
        public ProjectManager projectManager;

        /// <summary>
        /// Initializes the UIManager by setting up the ProjectManager instance, initializing data structures,
        /// adding button listeners, and displaying saved constellations.
        /// </summary>
        private void Start()
        {
            if (ProjectManager.Instance == null)
            {
                Debug.Log("Creating instance of ProjectManager");
                projectManager.Initialize(1, "Construction Project");
            }
            else
            {
                projectManager = ProjectManager.Instance;

            }
            // initialize List and Dictionary
            _machinesByTypeAvailable = new Dictionary<string, List<Machine>>();
            _machinesByTypeSelected = new Dictionary<string, List<Machine>>();
            _availableWorkers = new List<Worker>();
            _selectedWorkers = new List<Worker>();

            // Add listeners to buttons
            createConstellationButton.onClick.AddListener(CreateConstellationPage);

            // Load Constellations data
            HelperConstellations.InitializeConstellations();

            // display saved constellations
            DisplaySavedConstellations();
        }

        /// <summary>
        /// Loads worker data from JSON file into the available workers list.
        /// Only includes workers that aren't already in the selected workers list.
        /// Clears the available workers list before loading new data.
        /// </summary>
        private void LoadWorkers()
        {
            try
            {
                HelperWorkers.LoadWorkersFromJson(ProjectManager.Instance);
                _availableWorkers.Clear();
                // Only add workers that aren't already selected
                foreach (var worker in ProjectManager.Instance.Workers)
                {
                    if (!_selectedWorkers.Any(w => w.Id == worker.Id))
                    {
                        _availableWorkers.Add(worker);
                    }
                }
                Debug.Log($"Loaded {_availableWorkers.Count} available workers");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading workers: {e.Message}");
            }
        }
        /// <summary>
        /// Creates and displays a new constellation creation page.
        /// Clears previous selections, instantiates the popup UI, sets up UI elements,
        /// and configures button listeners for saving and canceling.
        /// Also initializes the machine/worker toggle functionality.
        /// </summary>
        private void CreateConstellationPage()
        {
            // clear the lists to avoid that previous choices reflect on the new popup
            _availableWorkers.Clear();
            _selectedWorkers.Clear();
            _machinesByTypeAvailable.Clear();
            _machinesByTypeSelected.Clear();

            constellationPage = Instantiate(popupPrefab, mainCanvas.transform); // Instantiate at the root level to center it in the scene
            constellationPage.name = "CreateConstellationPage";
            // Set the position of the popup to the center of the screen
            RectTransform rectTransform = constellationPage.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;

            availabilitiesListPanel = constellationPage.transform.Find("AvailabilitiesView/Viewport/ListView").gameObject;
            selectionsListPanel = constellationPage.transform.Find("SelectedConstellation/Viewport/ListView").gameObject;


            // Cancel button to close the popup
            cancelButton = constellationPage.transform.Find("CancelButton").GetComponentInChildren<Button>();
            cancelButton.onClick.AddListener(() =>
            {
                // Add the selected workers back to the available workers
                foreach (Worker worker in _selectedWorkers)
                {
                    _availableWorkers.Add(worker);
                }
                _selectedWorkers.Clear();

                // Add the selected machines back to the available machines
                foreach (var machineList in _machinesByTypeSelected.Values)
                {
                    foreach (Machine machine in machineList)
                    {
                        if (_machinesByTypeAvailable.ContainsKey(machine.MachineType))
                        {
                            _machinesByTypeAvailable[machine.MachineType].Add(machine);
                        }
                        else
                        {
                            _machinesByTypeAvailable[machine.MachineType] = new List<Machine> { machine };
                        }
                    }
                }
                _machinesByTypeSelected.Clear();
                Destroy(constellationPage);
            });

            // Save button to save the constellation
            saveButton = constellationPage.transform.Find("SaveButton").GetComponentInChildren<Button>();
            saveButton.onClick.AddListener(() =>
            {
                try
                {
                    // Save the constellation
                    HelperConstellations.SaveConstellation(constellationNameInput, _selectedWorkers, _machinesByTypeSelected);

                    // after saving the constellation, clear the lists and destroy the popup
                    Destroy(constellationPage);
                    DisplaySavedConstellations();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving constellation: {e.Message}");
                }
            });

            constellationNameInput = constellationPage.transform.Find("ConstellationName").GetComponent<TMP_InputField>();

            machineWorkerToggle = constellationPage.transform.Find("MachineWorkerSlider").GetComponentInChildren<Slider>();

            machineWorkerToggle.onValueChanged.AddListener((value) =>
            {
                UpdateEveryList();
            });
            // Used to show workers in assign popup before they are shown in create constellation popup with toggle (slider) = 1
            UpdateAvailableWorkerList();
            // Used to show the machines as soon as the popup is created because the value is not changed at the start
            UpdateMachineList();
        }


        /// <summary>
        /// Retrieves the list of available workers by loading workers from storage
        /// and filtering out any workers that are already selected.
        /// </summary>
        private void GetAvailableWorkers()
        {
            try
            {
                // adds to available workers only not selected workers
                LoadWorkers();
                _availableWorkers = _availableWorkers.Where(w => !_selectedWorkers.Contains(w)).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading workers: {e.Message}");
            }
        }

        /// <summary>
        /// Refreshes the UI display of available workers.
        /// Clears existing worker UI elements and creates new ones for each available worker.
        /// Includes worker details like ID, surname, role, and status.
        /// </summary>
        private void UpdateAvailableWorkerList()
        {
            // Clear existing UI elements in the worker list
            foreach (Transform child in availabilitiesListPanel.transform)
            {
                Destroy(child.gameObject);
            }

            GetAvailableWorkers();

            // Iterate over all workers in the ProjectManager and display them in the UI
            foreach (Worker worker in _availableWorkers)
            {
                // Instantiate a new worker UI element under the workerListPanel
                GameObject workerUI = Instantiate(workerPrefab, availabilitiesListPanel.transform);

                // Set the text of the UI element to the worker's ID
                TMP_Text workerText = workerUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                workerText.text = $"ID: {worker.Id}"; // Assuming Worker has an 'Id' property

                TMP_Text workerSurname = workerUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                workerSurname.text = $"{worker.Surname}"; // Assuming Worker has an 'Id' property

                TMP_Text roleText = workerUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                roleText.text = $"{worker.GetRole()}";

                TMP_Text statusText = workerUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>();
                statusText.text = $"{worker.Status}";

                // Add listener to delete the specific worker when the button is clicked
                Button selectButton = workerUI.transform.Find("SelectWorkerButton").GetComponent<Button>();
                selectButton.onClick.AddListener(() => SelectWorker(worker));
            }
        }

        /// <summary>
        /// Updates the UI display of available machines.
        /// Clears existing machine UI elements, loads machines from the ProjectManager,
        /// and displays them organized by machine type with relevant details and selection buttons.
        /// </summary>
        private void UpdateMachineList()
        {
            // Clear existing items
            foreach (Transform child in availabilitiesListPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Load machines from ProjectManager
            MachineManager.LoadMachinesStatic();

            // Organize machines into available dictionary
            OrganizeMachinesForConstellation();

            // Display all machines in a flat list
            foreach (var machineType in _machinesByTypeAvailable)
            {
                foreach (var machine in machineType.Value)
                {
                    GameObject machineUI = Instantiate(workerPrefab, availabilitiesListPanel.transform);

                    TMP_Text idText = machineUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                    idText.text = $"ID: {machine.MachineID}";

                    TMP_Text nameText = machineUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                    nameText.text = machine.Name;

                    TMP_Text typeText = machineUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                    typeText.text = machine.MachineType;

                    TMP_Text statusText = machineUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>();
                    statusText.text = machine.IsAvailable ? "Available" : "In Use";

                    Button selectButton = machineUI.transform.Find("SelectWorkerButton").GetComponent<Button>();
                    selectButton.onClick.AddListener(() => SelectMachine(machine));
                }
            }
        }

        /// <summary>
        /// Updates the UI display of selected workers and machines in the constellation.
        /// Filters out workers already assigned to machines and displays both unassigned workers
        /// and machines with their assigned workers if applicable.
        /// </summary>
        private void UpdateSelectedConstellationList()
        {
            foreach (Transform child in selectionsListPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Filter out workers that are already assigned to machines
            List<Worker> workers = new List<Worker>(_selectedWorkers).Where(w =>
            {
                foreach (var machineType in _machinesByTypeSelected)
                {
                    foreach (var machine in machineType.Value)
                    {
                        if (machine.AssignedWorkerID == w.Id)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }).ToList();
            // Iterate over all workers in the ProjectManager and display them in the UI
            foreach (Worker worker in workers)
            {
                // Instantiate a new worker UI element under the workerListPanel
                GameObject workerUI = Instantiate(workerPrefab, selectionsListPanel.transform);

                // Set the text of the UI element to the worker's ID
                TMP_Text workerText = workerUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                workerText.text = $"ID: {worker.Id}"; // Assuming Worker has an 'Id' property

                TMP_Text workerSurname = workerUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                workerSurname.text = $"{worker.Surname}"; // Assuming Worker has an 'Id' property

                TMP_Text roleText = workerUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                roleText.text = $"{worker.GetRole()}";

                TMP_Text statusText = workerUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>();
                statusText.text = $"{worker.Status}";

                // Add listener to delete the specific worker when the button is clicked
                Button selectButton = workerUI.transform.Find("SelectWorkerButton").GetComponent<Button>();
                selectButton.GetComponentInChildren<TMP_Text>().text = "-";
                selectButton.onClick.AddListener(() => RemoveWorker(worker));
            }

            foreach (var machineList in _machinesByTypeSelected.Values)
            {
                foreach (Machine machine in machineList)
                {
                    GameObject machineUI = Instantiate(selectionPrefab, selectionsListPanel.transform);

                    TMP_Text idText = machineUI.transform.Find("MachineId").GetComponent<TMP_Text>();
                    idText.text = $"ID: {machine.MachineID}";

                    TMP_Text nameText = machineUI.transform.Find("MachineType").GetComponent<TMP_Text>();
                    nameText.text = machine.Name;

                    TMP_Text typeText = machineUI.transform.Find("MachineManufacturer").GetComponent<TMP_Text>();
                    typeText.text = machine.MachineType;

                    TMP_Text statusText = machineUI.transform.Find("MachineStatus").GetComponent<TMP_Text>();
                    statusText.text = machine.IsAvailable ? "Available" : "In Use";

                    Button selectButton = machineUI.transform.Find("SelectMachineButton").GetComponent<Button>();
                    selectButton.GetComponentInChildren<TMP_Text>().text = "-";
                    selectButton.onClick.AddListener(() =>
                    {
                        RemoveMachine(machine);
                        _selectedWorkers.RemoveAll(w => w.Id == machine.AssignedWorkerID);
                    });

                    Button addWorkerButton = machineUI.transform.Find("SelectWorkerButton").GetComponent<Button>();
                    addWorkerButton.onClick.AddListener(() => AssignWorker(machine));

                    // Add worker info if assigned
                    if (machine.AssignedWorkerID != 0)
                    {
                        Worker worker = _selectedWorkers.Find(w => w.Id == machine.AssignedWorkerID);
                        if (worker != null)
                        {
                            // Set the text of the UI element to the worker's ID
                            TMP_Text workerText = machineUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                            workerText.text = $"ID: {worker.Id}"; // Assuming Worker has an 'Id' property

                            TMP_Text workerSurname = machineUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                            workerSurname.text = $"{worker.Surname}"; // Assuming Worker has an 'Id' property

                            TMP_Text roleText = machineUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                            roleText.text = $"{worker.GetRole()}";

                            TMP_Text workerStatusText = machineUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>();
                            workerStatusText.text = $"{worker.Status}";
                        }
                    }
                    else
                    {
                        machineUI.transform.Find("WorkerId").GetComponent<TMP_Text>().text = "ID: -";
                        machineUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>().text = "Name: -";
                        machineUI.transform.Find("WorkerRole").GetComponent<TMP_Text>().text = "Role: -";
                        machineUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>().text = "Status: -";
                    }
                }
            }
        }

        /// <summary>
        /// Handles the selection of a worker by moving them from available to selected list
        /// and updates both the available and selected worker displays.
        /// </summary>
        /// <param name="worker">The worker to be selected</param>
        private void SelectWorker(Worker worker)
        {
            _selectedWorkers.Add(worker);
            _availableWorkers = _availableWorkers.Where(w => w.Id != worker.Id).ToList();
            Debug.Log($"Selected worker: {worker.Id} Removed from available workers");
            UpdateAvailableWorkerList();
            UpdateSelectedConstellationList();
        }

        /// <summary>
        /// Handles the selection of a machine by moving it from available to selected list,
        /// organizing it by machine type, and updating the UI displays.
        /// </summary>
        /// <param name="machine">The machine to be selected</param>
        private void SelectMachine(Machine machine)
        {
            _machinesByTypeAvailable[machine.MachineType] = _machinesByTypeAvailable[machine.MachineType].Where(m => m.MachineID != machine.MachineID).ToList();
            if (!_machinesByTypeSelected.ContainsKey(machine.MachineType))
            {
                _machinesByTypeSelected[machine.MachineType] = new List<Machine>();
            }
            _machinesByTypeSelected[machine.MachineType].Add(machine);
            Debug.Log($"Selected machine: {machine.MachineID}");
            UpdateMachineList();
            UpdateSelectedConstellationList();
        }

        /// <summary>
        /// Removes a worker from the selected list and adds them back to available workers.
        /// Updates both the available and selected worker displays.
        /// </summary>
        /// <param name="worker">The worker to be removed</param>
        private void RemoveWorker(Worker worker)
        {
            _availableWorkers.Add(worker);
            _selectedWorkers = _selectedWorkers.Where(w => w.Id != worker.Id).ToList();
            Debug.Log($"Removed worker: {worker.Id} Added to available workers");
            UpdateAvailableWorkerList();
            UpdateSelectedConstellationList();

        }


        /// <summary>
        /// Removes a machine from the selected list and adds it back to available machines.
        /// Updates both the available and selected machine displays.
        /// </summary>
        /// <param name="machine">The machine to be removed</param>
        private void RemoveMachine(Machine machine)
        {
            _machinesByTypeSelected[machine.MachineType] = _machinesByTypeSelected[machine.MachineType].Where(m => m.MachineID != machine.MachineID).ToList();
            if (!_machinesByTypeAvailable.ContainsKey(machine.MachineType))
            {
                _machinesByTypeAvailable[machine.MachineType] = new List<Machine>();
            }
            _machinesByTypeAvailable[machine.MachineType].Add(machine);
            Debug.Log($"Removed machine: {machine.MachineID} Added to available machine");
            UpdateMachineList();
            UpdateSelectedConstellationList();
        }


        /// <summary>
        /// Loads and displays all saved constellations in the UI.
        /// Creates UI elements for each constellation with options to delete or edit.
        /// </summary>
        private void DisplaySavedConstellations()
        {
            HelperConstellations.LoadSavedConstellations();
            // Clear existing items
            foreach (Transform child in constellationsListPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Display each constellation
            foreach (var constellation in HelperConstellations.SavedConstellations)
            {
                GameObject constellationUI = Instantiate(constellationPrefab, constellationsListPanel.transform);

                // Reusing the worker prefab layout, but displaying constellation info
                TMP_Text idText = constellationUI.transform.Find("Id").GetComponent<TMP_Text>();
                idText.text = $"{constellation.id}";

                TMP_Text nameText = constellationUI.transform.Find("Name").GetComponent<TMP_Text>();
                nameText.text = constellation.name;

                // Change the select button to something more appropriate
                Button deleteButton = constellationUI.transform.Find("DeleteButton").GetComponent<Button>();
                // Add view functionality if needed
                deleteButton.onClick.AddListener(() => DeleteConstellation(constellation));

                Button editButton = constellationUI.transform.Find("EditButton").GetComponent<Button>();
                // Add view functionality if needed
                editButton.onClick.AddListener(() => EditConstellation(constellation));
            }
        }

        /// <summary>
        /// Deletes a saved constellation from both the list and storage file.
        /// Updates the UI display after deletion.
        /// </summary>
        /// <param name="constellation">The constellation to be deleted</param>
        private void DeleteConstellation(HelperConstellations.SavedConstellation constellation)
        {
            try
            {
                // Remove from the list
                HelperConstellations.SavedConstellations.RemoveAll(c => c.id == constellation.id);

                // Remove worker assignments from machines
                List<Machine> machinesToRemove = new List<Machine>();

                // loop through all machines in the constellation
                foreach (var machineWorkerPair in constellation.machineWorker)
                {
                    Machine machine = ProjectManager.Instance.MachinesOwned.Find(m => m.MachineID == machineWorkerPair.machineId);
                    if (machine.IsAvailable == false)
                    {
                        throw new Exception("Machine in use. The constellation cannot be deleted.");
                    }
                }

                // loop through all machines in the constellation and remove them
                foreach (var machineWorkerPair in constellation.machineWorker)
                {
                    Machine machine = ProjectManager.Instance.MachinesOwned.Find(m => m.MachineID == machineWorkerPair.machineId);
                    if (machine != null)
                    {
                        machine.AssignedWorkerID = -1;
                        machinesToRemove.Add(machine);
                    }
                }
                HelperConstellations.MirrorConstellationChangesInOwnedMachines(machinesToRemove);

                // Save changes to files
                HelperConstellations.SaveConstellationToFile();
                MachineManager.SaveMachinesToFile();

                // Refresh the display
                DisplaySavedConstellations();

                Debug.Log($"Constellation '{constellation.name}' deleted successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting constellation: {e.Message}");
            }
        }

        /// <summary>
        /// Opens the constellation editor with pre-populated data from an existing constellation.
        /// Loads associated workers and machines, sets up their assignments,
        /// and configures the save button to update instead of create new.
        /// </summary>
        /// <param name="constellation">The constellation to be edited</param>
        private void EditConstellation(HelperConstellations.SavedConstellation constellation)
        {
            // Create the page like normal
            CreateConstellationPage();

            // Set the constellation name
            constellationNameInput.text = constellation.name;

            // Load all available workers and machines first
            LoadWorkers();
            MachineManager.LoadMachinesStatic();

            // Organize machines into available dictionary
            OrganizeMachinesForConstellation();

            // Initialize selected workers
            foreach (string workerId in constellation.workerIds)
            {
                Worker worker = _availableWorkers.Find(w => w.Id.ToString() == workerId);
                if (worker != null)
                {
                    _selectedWorkers.Add(worker);
                    _availableWorkers.Remove(worker);
                }
            }

            // Initialize selected machines and their worker assignments
            foreach (HelperConstellations.MachineWorkerPair machineWorkerPair in constellation.machineWorker)
            {
                // Find the machine in the Project Manager's list
                Machine machineToSelect = ProjectManager.Instance.MachinesOwned
                    .Find(m => m.MachineID == machineWorkerPair.machineId);

                if (machineToSelect != null)
                {
                    string machineType = machineToSelect.MachineType.ToLower();

                    // Remove from available machines
                    if (_machinesByTypeAvailable.ContainsKey(machineType))
                    {
                        _machinesByTypeAvailable[machineType].RemoveAll(m =>
                            m.MachineID == machineToSelect.MachineID);
                    }

                    // Add to selected machines
                    if (!_machinesByTypeSelected.ContainsKey(machineType))
                    {
                        _machinesByTypeSelected[machineType] = new List<Machine>();
                    }

                    // Set the worker assignment
                    machineToSelect.AssignedWorkerID = machineWorkerPair.workerId;
                    _machinesByTypeSelected[machineType].Add(machineToSelect);

                    // Ensure the assigned worker is in selected workers if not already there
                    if (machineWorkerPair.workerId != 0)
                    {
                        Worker assignedWorker = _availableWorkers.Find(w =>
                            w.Id == machineWorkerPair.workerId);
                        if (assignedWorker != null)
                        {
                            _selectedWorkers.Add(assignedWorker);
                            _availableWorkers.Remove(assignedWorker);
                        }
                    }
                }
            }

            // Update all UI elements
            UpdateEveryList();

            // Modify save button to update instead of create new
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => UpdateConstellation(constellation.id));
        }

        /// <summary>
        /// Updates an existing constellation with new data.
        /// Validates the constellation name, checks for duplicates,
        /// updates worker and machine assignments, and saves changes to storage.
        /// </summary>
        /// <param name="constellationId">The ID of the constellation to update</param>
        private void UpdateConstellation(string constellationId)
        {
            // Validate constellation name
            string constellationName = constellationNameInput.text.Trim();
            if (string.IsNullOrEmpty(constellationName))
            {
                Debug.LogWarning("Please enter a constellation name");
                return;
            }

            // Check for duplicate names, excluding the current constellation
            if (HelperConstellations.SavedConstellations.Any(c => c.id != constellationId &&
                c.name.Equals(constellationName, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning("A constellation with this name already exists. Please choose a different name.");
                return;
            }

            // Find and update the existing constellation
            HelperConstellations.SavedConstellation existingConstellation = HelperConstellations.SavedConstellations.Find(c => c.id == constellationId);
            if (existingConstellation != null)
            {
                existingConstellation.name = constellationName;
                existingConstellation.workerIds = _selectedWorkers.Select(w => w.Id.ToString()).ToList();
                existingConstellation.machineWorker = new List<HelperConstellations.MachineWorkerPair>();

                // Validate constellation has at least one worker or machine
                if (existingConstellation.workerIds.Count == 0 && existingConstellation.machineWorker.Count == 0)
                {
                    Debug.LogWarning("Cannot save empty constellation. Please select at least one worker or machine.");
                    return;
                }

                // Add machine IDs from all selected machines
                foreach (var machineType in _machinesByTypeSelected)
                {
                    foreach (var machine in machineType.Value)
                    {
                        HelperConstellations.MachineWorkerPair pair = new HelperConstellations.MachineWorkerPair
                        {
                            machineId = machine.MachineID,
                            workerId = machine.AssignedWorkerID
                        };
                        existingConstellation.machineWorker.Add(pair);
                    }
                    HelperConstellations.MirrorConstellationChangesInOwnedMachines(_machinesByTypeSelected[machineType.Key]);
                }

                // Save to file
                HelperConstellations.SaveConstellationToFile();
                DisplaySavedConstellations();
                Destroy(constellationPage);

                Debug.Log($"Constellation '{constellationName}' updated successfully with {existingConstellation.workerIds.Count} workers and {existingConstellation.machineWorker.Count} machines");
            }
        }

        /// <summary>
        /// Creates a popup interface for assigning a worker to a specific machine.
        /// Displays available workers and handles the assignment process,
        /// including updating worker lists and machine assignments.
        /// </summary>
        /// <param name="machine">The machine to assign a worker to</param>
        private void AssignWorker(Machine machine)
        {
            // Create a new popup to assign a worker to the machine
            GameObject assignWorkerPopup = Instantiate(assignWorkerPopupPrefab, mainCanvas.transform);
            assignWorkerPopup.name = "AssignWorker2MachinePopup";
            RectTransform rectTransform = assignWorkerPopup.GetComponent<RectTransform>();
            rectTransform.SetAsLastSibling(); // Move to the top of the hierarchy
            rectTransform.anchoredPosition = Vector2.zero;

            // Get the worker list panel
            GameObject workerListPanel = assignWorkerPopup.transform.Find("AssignableWorkersView/Viewport/WorkersListView").gameObject;
            List<Worker> availableWorkers = _availableWorkers.Where(w => !_selectedWorkers.Contains(w)).ToList();
            Debug.Log($"Available workers: {availableWorkers.Count}");
            // Display all available workers
            foreach (Worker availableWorker in _availableWorkers)
            {
                GameObject workerUI = Instantiate(workerPrefab, workerListPanel.transform);

                TMP_Text idText = workerUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                idText.text = $"ID: {availableWorker.Id}";

                TMP_Text nameText = workerUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                nameText.text = availableWorker.Surname;

                TMP_Text roleText = workerUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                roleText.text = availableWorker.GetRole();

                TMP_Text statusText = workerUI.transform.Find("WorkerStatus").GetComponent<TMP_Text>();
                statusText.text = availableWorker.Status.ToString();

                Button selectButton = workerUI.transform.Find("SelectWorkerButton").GetComponent<Button>();
                selectButton.onClick.AddListener(() =>
                {
                    // Assign the worker to the machine
                    if (machine.AssignedWorkerID != -1)
                    {
                        // Add the previously assigned worker back to the available workers
                        Worker previouslyAssignedWorker = _selectedWorkers.Find(w => w.Id == machine.AssignedWorkerID);
                        if (previouslyAssignedWorker != null)
                        {
                            _availableWorkers.Add(previouslyAssignedWorker);
                            _selectedWorkers.Remove(previouslyAssignedWorker);
                        }
                    }
                    machine.AssignedWorkerID = availableWorker.Id;
                    _selectedWorkers.Add(availableWorker);
                    _availableWorkers.Remove(availableWorker);
                    UpdateEveryList();
                    Destroy(assignWorkerPopup);
                });
            }

            // Add a cancel button
            Button cancelButton = assignWorkerPopup.transform.Find("CancelButton").GetComponent<Button>();
            cancelButton.onClick.AddListener(() => Destroy(assignWorkerPopup));
        }

        /// <summary>
        /// Updates all UI lists based on the machine/worker toggle state.
        /// Refreshes either the machine list or worker list along with the selected constellation list.
        /// </summary>
        private void UpdateEveryList()
        {
            if (machineWorkerToggle.value == 0)
            {
                UpdateMachineList();
            }
            else
            {
                UpdateAvailableWorkerList();
            }
            UpdateSelectedConstellationList();
        }

        /// <summary>
        /// Organizes available machines by type for constellation creation.
        /// Clears existing available machines and categorizes machines from ProjectManager,
        /// excluding any that are already selected in existing constellations.
        /// </summary>
        private void OrganizeMachinesForConstellation()
        {
            // Clear existing available machines dictionary
            _machinesByTypeAvailable.Clear();

            // Get machines from ProjectManager
            List<Machine> allMachines = ProjectManager.Instance.MachinesOwned;

            // Organize machines by type, excluding any that are already in selected machines
            foreach (Machine machine in allMachines)
            {
                string machineType = machine.MachineType.ToLower();

                // Skip if machine is already in selected machines
                bool isAlreadySelected = false;
                if (_machinesByTypeSelected.ContainsKey(machineType))
                {
                    isAlreadySelected = _machinesByTypeSelected[machineType]
                        .Any(m => m.MachineID == machine.MachineID);
                }

                if (!isAlreadySelected)
                {
                    if (!_machinesByTypeAvailable.ContainsKey(machineType))
                    {
                        _machinesByTypeAvailable[machineType] = new List<Machine>();
                    }
                    _machinesByTypeAvailable[machineType].Add(machine);
                }
            }
        }
    }
}