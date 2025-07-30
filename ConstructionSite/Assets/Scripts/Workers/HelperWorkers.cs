using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace constructionSite.Scripts
{
    /// <summary>
    /// Static helper class that manages worker data serialization and ID generation.
    /// Provides functionality for saving and loading worker data to/from JSON.
    /// </summary>
    static public class HelperWorkers
    {

        private static string path = "./Assets/workers.json";

        private static string testPath = "./Assets/test_workers.json";

        // Flag to indicate if the current operation is a test and to avoid overwriting the JSON file
        public static bool isTest = false;

        [System.Serializable]
        /// <summary>
        /// Serializable class representing the data structure of a worker
        /// for JSON storage and retrieval.
        /// </summary>
        public class WorkerData
        {
            public string Name;
            public string Surname;
            public int Id;
            public string Role;
            public string Status;
        }

        [System.Serializable]
        /// <summary>
        /// Serializable wrapper class containing a list of worker data
        /// for JSON serialization.
        /// </summary>
        public class WorkersData
        {
            public List<WorkerData> workers;
        }


        /// <summary>
        /// Generates a unique ID for a new worker based on existing worker IDs.
        /// </summary>
        /// <param name="existingWorkers">List of current workers in the system</param>
        /// <returns>A unique worker ID that is one greater than the highest existing ID</returns>
        public static int GenerateWorkerUniqueId(List<Worker> existingWorkers)
        {
            if (existingWorkers.Count == 0) return 1;
            return existingWorkers.Max(w => w.Id) + 1;
        }

        /// <summary>
        /// Loads worker data from a JSON file and populates the ProjectManager's worker list.
        /// Clears existing workers before loading new data.
        /// </summary>
        /// <param name="projectManager">The ProjectManager instance to load worker data into</param>
        /// <exception cref="System.Exception">Thrown when there's an error loading or parsing the JSON file</exception>
        public static void LoadWorkersFromJson(ProjectManager projectManager)
        {
            try
            {
                if (System.IO.File.Exists(GetPath()))
                {
                    projectManager.Workers.Clear();
                    string json = System.IO.File.ReadAllText(GetPath());
                    WorkersData workersData = JsonUtility.FromJson<WorkersData>(json);

                    foreach (WorkerData workerData in workersData.workers)
                    {
                        Worker worker = new Worker
                        (workerData.Id,
                        workerData.Name,
                        workerData.Surname,
                        (Worker.Role)Enum.Parse(typeof(Worker.Role), workerData.Role),
                        (Worker.WorkerStatus)Enum.Parse(typeof(Worker.WorkerStatus), workerData.Status));
                        projectManager.addWorker(worker);
                    }
                    Debug.Log($"Loaded workers from JSON");
                }

                // Resets the test flag after loading
                isTest = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading workers from JSON: {e}");
            }
        }

        /// <summary>
        /// Saves the current worker data from ProjectManager to a JSON file.
        /// Converts worker objects to serializable format before saving.
        /// </summary>
        /// <param name="projectManager">The ProjectManager instance containing worker data to save</param>
        /// <exception cref="System.Exception">Thrown when there's an error saving the data to JSON file</exception>
        public static void SaveWorkersToJson(ProjectManager projectManager)
        {
            try
            {
                WorkersData workersData = new WorkersData();
                workersData.workers = projectManager.Workers.Select(worker => new WorkerData
                {
                    Name = worker.Name,
                    Surname = worker.Surname,
                    Id = worker.Id,
                    Role = worker.GetRole().ToString(),
                    Status = worker.Status.ToString()
                }).ToList();

                string json = JsonUtility.ToJson(workersData, true);
                System.IO.File.WriteAllText(GetPath(), json);

                // Resets the test flag after saving
                isTest = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving workers to JSON: {e}");
            }
        }

        /// <summary>
        /// Function that returns the path to the JSON file based on the current operation type.
        /// the test flag is set to true manually, so by default it will return the path to the main JSON file.
        /// if the test flag is set to true, it will return the path to the test JSON file. only for a single operation.
        /// After the saving or loading operation is completed, the test flag is reset to false.
        /// </summary>
        private static string GetPath()
        {
            return isTest ? testPath : path;
        }
    }
}