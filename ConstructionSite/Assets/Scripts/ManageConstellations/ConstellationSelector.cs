using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TaskManagement;
using UnityEditor;
using constructionSite.Scripts;

namespace constructionSite.Scripts
{
    public static class ConstellationSelector
    {
        /// <summary>
        /// Finds the most suitable constellation for a given list of tasks based on machine requirements
        /// </summary>
        /// <param name="tasks">List of tasks containing required machine types</param>
        /// <returns>The ID of the most suitable constellation, or null if none found</returns>
        public static string FindSuitableConstellation(List<TaskData> tasks)
        {
            // Validate inputs
            if (tasks == null || !tasks.Any())
            {
                Debug.LogError("Task list is null or empty");
                return null;
            }

            // Get required machine types from tasks, converting to lowercase and removing duplicates
            HashSet<string> requiredMachineTypes = new HashSet<string>(
                tasks
                    .Where(task => !string.IsNullOrEmpty(task.MachineType))
                    .Select(task => task.MachineType.ToLower())
            );

            // If no machines required, return null
            if (requiredMachineTypes.Count == 0)
            {
                Debug.LogWarning("No machine requirements specified in tasks");
                return null;
            }

            // Track the best constellation found
            string bestConstellationId = null;
            int minExtraMachines = int.MaxValue;

            // Load saved constellations and check if not null or empty
            HelperConstellations.InitializeConstellations();
            var constellations = HelperConstellations.SavedConstellations;
            if (constellations == null)
            {
                Debug.LogWarning("No constellations found");
                return null;
            }

            // Iterate through each saved constellation
            foreach (var constellation in HelperConstellations.SavedConstellations)
            {
                // Skip if constellation is empty
                if (constellation.machineWorker == null || constellation.machineWorker.Count == 0)
                {
                    continue;
                }

                // Get machine count by type in this constellation
                var constellationMachines = new Dictionary<string, int>();
                foreach (var pair in constellation.machineWorker)
                {

                    MachineManager.LoadMachinesStatic();
                    // Get machine type from machine ID
                    Machine machine = ProjectManager.Instance.MachinesOwned
                        .FirstOrDefault(m => m.MachineID == pair.machineId);

                    if (machine != null)
                    {
                        string machineType = machine.MachineType.ToLower();
                        if (!constellationMachines.ContainsKey(machineType))
                        {
                            constellationMachines[machineType] = 0;
                        }
                        constellationMachines[machineType]++;
                    }
                }

                // Check if constellation has all required machine types in sufficient quantities
                bool hasAllRequired = true;
                foreach (var requiredType in requiredMachineTypes)
                {
                    // Count how many of this type are required
                    int requiredCount = tasks
                        .Count(task => task.MachineType.ToLower() == requiredType);

                    // Check if constellation has enough of this type
                    if (!constellationMachines.ContainsKey(requiredType) ||
                        constellationMachines[requiredType] < requiredCount)
                    {
                        hasAllRequired = false;
                        break;
                    }
                }

                if (hasAllRequired)
                {
                    // Calculate total required machines
                    int totalRequired = tasks.Count;

                    // Calculate how many extra machines this constellation has
                    int extraMachines = constellation.machineWorker.Count - totalRequired;

                    // Update best constellation if this one has fewer extra machines
                    if (extraMachines < minExtraMachines && extraMachines >= 0)
                    {
                        minExtraMachines = extraMachines;
                        bestConstellationId = constellation.id;
                    }
                }
            }

            if (bestConstellationId == null)
            {
                Debug.LogWarning($"No suitable constellation found for tasks");
            }
            else
            {
                Debug.Log($"Found suitable constellation {bestConstellationId} for tasks");
            }

            return bestConstellationId;
        }
    }
}