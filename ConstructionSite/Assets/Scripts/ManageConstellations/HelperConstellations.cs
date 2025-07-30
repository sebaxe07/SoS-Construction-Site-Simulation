using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace constructionSite.Scripts
{
    /// <summary>
    /// Static helper class that manages constellation data, including saving, loading, 
    /// and organizing machine and worker configurations.
    /// </summary>
    static public class HelperConstellations
    {

        private static List<SavedConstellation> _savedConstellations;

        public static List<SavedConstellation> SavedConstellations
        {
            get { return _savedConstellations; }
            set { _savedConstellations = value; }
        }

        #region JSON Data
        [Serializable]
        public class MachineWorkerPair
        {
            public int machineId;
            public int workerId;
        }

        [Serializable]
        public class SavedConstellation
        {
            public string id;
            public string name;
            public List<string> workerIds;
            public List<MachineWorkerPair> machineWorker;
        }

        // Helper class for JSON deserialization
        [Serializable]
        private class ConstellationsWrapper
        {
            public List<SavedConstellation> constellations;
        }

        [Serializable]
        public class MovingVehicleProperties
        {
            public string wheelOrTrackType;
            public double fuelCapacity;
            public double maxSpeed;
            public double avgSpeed;
        }

        [Serializable]
        public class SteadyVehicleProperties
        {
            public bool immovable;
        }

        [Serializable]
        public class ExcavatorProperties
        {
            public double bucketCapacity;
        }

        [Serializable]
        public class TruckProperties
        {
            public double loadCapacity;
        }

        [Serializable]
        public class CraneProperties
        {
            public double loadMaxWeight;
        }

        [Serializable]
        private class Wrapper
        {
            public List<MachineParser> machines;
        }
        #endregion

        #region Constellation methods

        /// <summary>
        /// Saves the current list of constellations to a JSON file.
        /// Creates a wrapper object and writes the formatted JSON to the Assets folder.
        /// </summary>
        /// <exception cref="Exception">Thrown when there's an error saving the constellations</exception>
        public static void SaveConstellationToFile()
        {
            try
            {
                // Create wrapper object
                var wrapper = new ConstellationsWrapper { constellations = _savedConstellations };

                // Convert to JSON with pretty printing
                string json = JsonUtility.ToJson(wrapper, true);

                // Write to file in Assets folder
                File.WriteAllText("./Assets/constellations.json", json);

                Debug.Log($"Constellations saved successfully to file");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving constellations: {e.Message}");
            }
        }


        /// <summary>
        /// Creates and saves a new constellation with the given name and selected workers.
        /// Validates the constellation name, checks for duplicates, and ensures all machines have assigned workers.
        /// </summary>
        /// <param name="constellationNameInput">Input field containing the constellation name</param>
        /// <param name="_selectedWorkers">List of workers selected for the constellation</param>
        /// <exception cref="ArgumentException">Thrown when validation fails for constellation name or worker assignments</exception>
        public static void SaveConstellation(TMP_InputField constellationNameInput, List<Worker> _selectedWorkers, Dictionary<string, List<Machine>> _machinesByTypeSelected)
        {
            // Validate constellation name
            string constellationName = constellationNameInput.text.Trim();
            if (string.IsNullOrEmpty(constellationName))
            {
                Debug.LogWarning("Please enter a constellation name");
                // Show a warning message
                Sprite warningImage = Resources.Load<Sprite>("Images/Template Images/warning.png");
                ModalManager.Instance.ShowModal(ModalViewMode.Horizontal, "Notification", warningImage, "Constellation has no name\n\nPlease input a name and try again"
                , "OK", null, null, null, null, null);
                throw new System.ArgumentException("Constellation name cannot be empty");
            }

            // Check for duplicate names
            if (_savedConstellations.Any(c => c.name.Equals(constellationName, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning("A constellation with this name already exists. Please choose a different name.");
                Sprite warningImage = Resources.Load<Sprite>("Images/Template Images/warning.png");
                ModalManager.Instance.ShowModal(ModalViewMode.Horizontal, "Notification", warningImage, "Constellation has same name of another constellation\n\nPlease input a different name and try again"
                , "OK", null, null, null, null, null);
                throw new System.ArgumentException("Constellation name already exists");
            }

            // Create constellation data
            SavedConstellation constellation = new SavedConstellation
            {
                id = System.Guid.NewGuid().ToString(),
                name = constellationName,
                workerIds = _selectedWorkers.Select(w => w.Id.ToString()).ToList(),
                machineWorker = new List<MachineWorkerPair>()
            };

            // Add machine IDs from all selected machines
            foreach (var machineType in _machinesByTypeSelected)
            {
                foreach (var machine in machineType.Value)
                {
                    int workerId = machine.AssignedWorkerID;
                    if (workerId == -1)
                    {
                        Debug.LogWarning("Please assign a worker to all selected machines");
                        Sprite warningImage = Resources.Load<Sprite>("Images/Template Images/warning.png");
                        ModalManager.Instance.ShowModal(ModalViewMode.Horizontal, "Notification", warningImage, "Please assign a worker to all selected machines"
                        , "OK", null, null, null, null, null);
                        throw new System.ArgumentException("All machines must have a worker assigned");
                    }
                    MachineWorkerPair pair = new MachineWorkerPair
                    {
                        machineId = machine.MachineID,
                        workerId = workerId
                    };
                    constellation.machineWorker.Add(pair);
                }
            }

            // Validate constellation has at least one worker or machine
            if (constellation.workerIds.Count == 0 && constellation.machineWorker.Count == 0)
            {
                Debug.LogWarning("Cannot save empty constellation. Please select at least one worker or machine.");
                Sprite warningImage = Resources.Load<Sprite>("Images/Template Images/warning.png");
                ModalManager.Instance.ShowModal(ModalViewMode.Horizontal, "Notification", warningImage, "Cannot save empty constellation. Please select at least one worker or machine"
                , "OK", null, null, null, null, null);
                throw new System.ArgumentException("Constellation must have at least one worker or machine");
            }

            foreach (var machineType in _machinesByTypeSelected.Keys)
            {
                MirrorConstellationChangesInOwnedMachines(_machinesByTypeSelected[machineType]);
            }


            // Add to saved constellations
            _savedConstellations.Add(constellation);

            // Save to file
            SaveConstellationToFile();




            Debug.Log($"Constellation '{constellationName}' saved successfully with {constellation.workerIds.Count} workers and {constellation.machineWorker.Count} machines");
        }


        public static void MirrorConstellationChangesInOwnedMachines(List<Machine> machines)
        {
            //here the list of owned machines in the project manager is updated
            foreach (var machine in machines)
            {
                // Remove machine from owned list and add it back to update the reference
                Machine machineFound = ProjectManager.Instance.MachinesOwned.Find(m => m.MachineID.Equals(machine.MachineID));
                machineFound.AssignedWorkerID = machine.AssignedWorkerID;
            }
            MachineManager.SaveMachinesToFile();
        }

        /// <summary>
        /// Loads saved constellations from the JSON file in the Assets folder.
        /// Creates a new empty list if no file exists or if there's an error loading the file.
        /// </summary>
        /// <returns>List of saved constellations</returns>
        public static List<SavedConstellation> LoadSavedConstellations()
        {
            try
            {
                // Read from the JSON file in Assets folder
                string jsonPath = "./Assets/constellations.json";

                // Check if file exists
                if (!File.Exists(jsonPath))
                {
                    Debug.Log("No constellations file found. Starting with empty list.");
                    return new List<SavedConstellation>();
                }

                // Read the JSON content
                string jsonContent = File.ReadAllText(jsonPath);

                // Deserialize the JSON
                var wrapper = JsonUtility.FromJson<ConstellationsWrapper>(jsonContent);

                Debug.Log($"Loaded {wrapper?.constellations?.Count ?? 0} constellations from file");
                return wrapper?.constellations ?? new List<SavedConstellation>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading constellations: {e.Message}");
                return new List<SavedConstellation>();
            }
        }

        /// <summary>
        /// Initializes the saved constellations list by loading data from storage.
        /// </summary>
        public static void InitializeConstellations()
        {
            _savedConstellations = LoadSavedConstellations();
        }

        /// <summary>
        /// Returns the machine IDs from a constellation with the specified ID.
        /// </summary>
        /// <param name="constellationId"></param>
        /// <returns></returns>
        public static List<int> GetMachineIdsFromConstellation(string constellationId)
        {
            // Find the constellation with the specified ID
            SavedConstellation constellation = _savedConstellations?.FirstOrDefault(c => c.id == constellationId);

            // If constellation not found or has no machine-worker pairs, return empty list
            if (constellation == null || constellation.machineWorker == null)
            {
                Debug.LogWarning($"No constellation found with ID: {constellationId}");
                return new List<int>();
            }

            // Extract and return all machine IDs from the constellation
            List<int> machineIds = constellation.machineWorker
                .Select(pair => pair.machineId)
                .ToList();

            Debug.Log($"Found {machineIds.Count} machines in constellation: {constellation.name}");
            return machineIds;
        }


        #endregion
    }

}