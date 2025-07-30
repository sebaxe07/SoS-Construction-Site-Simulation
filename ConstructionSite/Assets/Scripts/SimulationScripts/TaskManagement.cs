using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TaskManagement
{
    public class TaskData
    {
        public int MachineId { get; set; }    // Assigned machine ID
        public string MachineType { get; set; } // e.g., "Digger", "Loader", "Truck"
        public string TaskType { get; set; }   // e.g., "Dig", "Move"
        public Vector3 Position { get; set; } // Target position for the task
        public string ZoneName { get; set; }  // Zone associated with the task
        public string TargetZone { get; set; } // Target zone for the task
    }

    public static class TaskManager
    {
        public static Dictionary<string, List<TaskData>> ZoneTasks = new Dictionary<string, List<TaskData>>();

        public static void AddTask(string machineType, string taskType, Vector3 position, string zoneName)
        {
            if (!ZoneTasks.ContainsKey(zoneName))
            {
                ZoneTasks[zoneName] = new List<TaskData>();
            }

            ZoneTasks[zoneName].Add(new TaskData
            {
                MachineId = -1,
                MachineType = machineType,
                TaskType = taskType,
                Position = position,
                ZoneName = zoneName
            });
        }

        public static void AddTask(string machineType, string taskType, string targetZone, string zoneName)
        {
            if (!ZoneTasks.ContainsKey(zoneName))
            {
                ZoneTasks[zoneName] = new List<TaskData>();
            }

            ZoneTasks[zoneName].Add(new TaskData
            {
                MachineId = -1,
                MachineType = machineType,
                TaskType = taskType,
                TargetZone = targetZone,
                ZoneName = zoneName
            });
        }
        public static void ClearTasksForZone(string zoneName)
        {
            if (ZoneTasks.ContainsKey(zoneName))
            {
                ZoneTasks[zoneName].Clear();
            }
        }

        public static List<TaskData> GetTasksForZone(string zoneName)
        {
            if (ZoneTasks.ContainsKey(zoneName))
            {
                // print list of tasks for debugging
                foreach (var task in ZoneTasks[zoneName])
                {
                    Debug.Log($"Task: {task.TaskType} for {task.MachineType} at {task.Position} in Zone {task.TargetZone}");
                }
                return ZoneTasks[zoneName];
            }
            return new List<TaskData>();
        }

        public static void ClearAllTasks()
        {
            ZoneTasks.Clear();
        }

        public static void AssignTaskToMachine(TaskData task, int machineId)
        {
            task.MachineId = machineId;
        }
    }

    public static class TaskAllocator
    {
        // Maps which machine types can perform which block types
        private static readonly Dictionary<BlockType, MachineType[]> taskCapabilities = new Dictionary<BlockType, MachineType[]>
        {
            { BlockType.Move, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.MoveZone, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.Dig, new[] { MachineType.Excavator } },
            { BlockType.Pickup, new[] { MachineType.Loader, MachineType.Excavator } },
            { BlockType.Unload, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.TurnOn, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.TurnOff, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.Idle, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.Park, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } },
            { BlockType.DropOff, new[] { MachineType.Loader, MachineType.Excavator, MachineType.Truck } }
        };

        public static void AllocateTasksToMachinesInZone(string zoneName, List<GameObject> zoneMachines)
        {
            List<TaskData> tasks = TaskManager.GetTasksForZone(zoneName);

            if (tasks == null || tasks.Count == 0)
            {
                Debug.LogWarning($"No tasks found for Zone {zoneName}");
                return;
            }

            var machineTasks = new Dictionary<GameObject, List<TaskData>>();


            foreach (var task in tasks)
            {
                // Get all machines of the required type in the zone
                var availableMachines = zoneMachines
                    .Where(machine => machine.GetComponent<MachineBehavior>().MachineType.Equals(task.MachineType, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (availableMachines.Count == 0)
                {
                    Debug.LogWarning($"No available machines of type {task.MachineType} in Zone {zoneName} for task {task.TaskType}");
                    continue;
                }

                // Assign task to the first available machine
                var machine = availableMachines.First();
                if (!machineTasks.ContainsKey(machine))
                {
                    machineTasks[machine] = new List<TaskData>();
                }

                machineTasks[machine].Add(task);

                TaskManager.AssignTaskToMachine(task, machine.GetComponent<MachineBehavior>().MachineID);

            }

            // Process the tasks for each machine
            foreach (var machineTask in machineTasks)
            {
                MachineBehavior machineBehavior = machineTask.Key.GetComponent<MachineBehavior>();
                if (machineBehavior != null)
                {
                    machineBehavior.MachineName = machineTask.Key.name;
                    machineBehavior.AssignedZone = zoneName;
                    machineBehavior.Tasks = new Queue<TaskData>(machineTask.Value);
                }
                else
                {
                    Debug.LogError($"Machine {machineTask.Key.name} does not have a MachineBehavior component!");
                }
                SimulationManager.Instance.StartMachineTaskProcessing(machineTask.Key, new Queue<TaskData>(machineTask.Value));
            }

        }
    }
}
