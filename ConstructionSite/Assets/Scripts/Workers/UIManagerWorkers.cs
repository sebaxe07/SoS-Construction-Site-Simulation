using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace constructionSite.Scripts
{
    /// <summary>
    /// Manages the UI elements and interactions for worker management, including adding,
    /// editing, and displaying workers in the project.
    /// </summary>
    public class UIWorkersManager : MonoBehaviour
    {
        public ProjectManager projectManager;     // Reference to your ProjectManager (holds worker list)
        public Button addWorkerButton;            // The button to add workers

        public Button addWorkersButton;
        public GameObject workerListPanel;        // The panel where the workers will be listed
        public GameObject workerPrefab;           // Prefab for a worker UI element (Text or Button)

        public GameObject popupPrefab;            // Prefab for a popup to edit worker details

        public GameObject workersNumberField;

        public GameObject mainCanvas; // Used to have a position for the popup

        /// <summary>
        /// Initializes the UI manager by setting up the ProjectManager instance,
        /// attaching button listeners, and loading existing worker data.
        /// </summary>
        void Start()
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
            // Attach the AddWorker method to the button's onClick event
            addWorkerButton.onClick.AddListener(AddWorker);

            // Attach the AddWorkers method to the button's onClick event
            addWorkersButton.onClick.AddListener(AddWorkers);
            workersNumberField.GetComponentInChildren<TMP_InputField>().onSubmit.AddListener(delegate { AddWorkers(); });

            LoadWorkersFromJson(projectManager);
        }

        /// <summary>
        /// Handles the addition of a single worker to the project.
        /// Adds the worker through the project manager and updates the UI.
        /// </summary>
        void AddWorker()
        {
            try
            {

                projectManager.addWorker();

                // Update the UI to reflect the new worker
                UpdateWorkerList();
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }

        /// <summary>
        /// Adds multiple workers to the project based on the input field value.
        /// Validates the input number and creates the specified number of workers.
        /// </summary>
        private void AddWorkers()
        {

            TMP_InputField inputField = workersNumberField.GetComponentInChildren<TMP_InputField>();
            int number;

            if (int.TryParse(inputField.text, out number))
            {
                for (int i = 0; i < number; i++)
                {
                    try
                    {
                        projectManager.addWorker();
                        // Update the UI to reflect the new worker
                        UpdateWorkerList();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error adding worker: {e}");
                    }
                }
                inputField.text = "";
            }
            else
            {
                Debug.LogError("Invalid input for number of workers.");
            }
        }

        /// <summary>
        /// Removes a worker from the project and updates the UI.
        /// </summary>
        /// <param name="worker">The worker to be deleted</param>
        private void DeleteWorker(Worker worker)
        {
            try
            {
                // Remove worker from the project manager
                projectManager.deleteWorker(worker.Id);

                // Update the UI to reflect the change
                UpdateWorkerList();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deleting worker: {e}");
            }
        }

        /// <summary>
        /// Creates and displays a popup for editing worker details.
        /// Allows modification of name, surname, and role while displaying ID and status.
        /// </summary>
        /// <param name="worker">The worker to be edited</param>
        private void EditWorker(Worker worker)
        {
            GameObject editPopup = Instantiate(popupPrefab, mainCanvas.transform); // Instantiate at the root level to center it in the scene
            editPopup.name = "EditWorkerPopup";

            // Set the text fields in the popup to the current worker's details
            TMP_Text idField = editPopup.transform.Find("Canvas/WorkerId").GetComponent<TMP_Text>();
            idField.text = $"Worker ID: {worker.Id}";

            TMP_InputField nameField = editPopup.transform.Find("Canvas/WorkerName").GetComponent<TMP_InputField>();
            TMP_InputField surnameField = editPopup.transform.Find("Canvas/WorkerSurname").GetComponent<TMP_InputField>();

            TMP_Dropdown roleField = editPopup.transform.Find("Canvas/WorkerRole").GetComponent<TMP_Dropdown>();
            roleField.ClearOptions(); // Clear existing options
            List<string> roles = Enum.GetValues(typeof(Worker.Role)).Cast<Worker.Role>().Select(r => r.ToString()).ToList(); // Ensure ProjectManager has a GetAvailableRoles method
            roleField.AddOptions(roles);

            TMP_Text statusField = editPopup.transform.Find("Canvas/Status").GetComponent<TMP_Text>();
            statusField.text = worker.Status.ToString();

            Button saveButton = editPopup.transform.Find("Canvas/SaveButton").GetComponent<Button>();
            saveButton.onClick.AddListener(() =>
            {
                worker.Name = nameField.text;
                worker.Surname = surnameField.text;
                worker.SetRole((Worker.Role)Enum.Parse(typeof(Worker.Role), roleField.options[roleField.value].text));
                Destroy(editPopup);
                UpdateWorkerList();
            });

            // Add listener to cancel the edit
            Button cancelButton = editPopup.transform.Find("Canvas/CancelButton").GetComponent<Button>();
            cancelButton.onClick.AddListener(() => Destroy(editPopup));

            // Ensure the popup is in the center and on top
            RectTransform rectTransform = editPopup.GetComponent<RectTransform>();
            rectTransform.SetAsLastSibling(); // Move to the top of the hierarchy
            rectTransform.anchoredPosition = Vector2.zero; // Center the popup
        }

        /// <summary>
        /// Refreshes the UI display of all workers.
        /// Clears existing worker UI elements and creates new ones for each worker
        /// in the project, including their details and action buttons.
        /// </summary>
        void UpdateWorkerList()
        {
            // Clear existing UI elements in the worker list
            foreach (Transform child in workerListPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Iterate over all workers in the ProjectManager and display them in the UI
            foreach (Worker worker in projectManager.Workers)
            {
                // Instantiate a new worker UI element under the workerListPanel
                GameObject workerUI = Instantiate(workerPrefab, workerListPanel.transform);

                // Set the text of the UI element to the worker's ID
                TMP_Text workerText = workerUI.transform.Find("WorkerId").GetComponent<TMP_Text>();
                workerText.text = $"Worker ID: {worker.Id}"; // Assuming Worker has an 'Id' property

                TMP_Text workerName = workerUI.transform.Find("WorkerName").GetComponent<TMP_Text>();
                workerName.text = $"{worker.Name}"; // Assuming Worker has an 'Id' property

                TMP_Text workerSurname = workerUI.transform.Find("WorkerSurname").GetComponent<TMP_Text>();
                workerSurname.text = $"{worker.Surname}"; // Assuming Worker has an 'Id' property

                TMP_Text roleText = workerUI.transform.Find("WorkerRole").GetComponent<TMP_Text>();
                roleText.text = $"Role: {worker.GetRole()}";

                TMP_Text statusText = workerUI.transform.Find("Status").GetComponent<TMP_Text>();
                statusText.text = $"Status: {worker.Status}";

                Button deleteButton = workerUI.transform.Find("DeleteWorkerButton").GetComponent<Button>();
                Button editButton = workerUI.transform.Find("Edit").GetComponent<Button>();

                // Add listener to delete the specific worker when the button is clicked
                deleteButton.onClick.AddListener(() => DeleteWorker(worker));

                editButton.onClick.AddListener(() => EditWorker(worker));
            }
            SaveWorkersToJson(projectManager);
        }

        /// <summary>
        /// Saves the current worker data to JSON storage through the HelperWorkers class.
        /// </summary>
        /// <param name="projectManager">The ProjectManager instance containing worker data</param>
        private void SaveWorkersToJson(ProjectManager projectManager)
        {
            HelperWorkers.SaveWorkersToJson(projectManager);
        }

        /// <summary>
        /// Loads worker data from JSON storage and updates the UI display.
        /// </summary>
        /// <param name="projectManager">The ProjectManager instance to load worker data into</param>
        private void LoadWorkersFromJson(ProjectManager projectManager)
        {
            HelperWorkers.LoadWorkersFromJson(projectManager);
            UpdateWorkerList();
        }


    }
}