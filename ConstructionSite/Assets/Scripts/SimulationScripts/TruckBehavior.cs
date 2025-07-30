using UnityEngine;
using System.Collections;
using TaskManagement;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Linq;

public class TruckBehavior : MachineBehavior
{
    public float moveSpeed = 5f;
    private int loadCount = 0;
    private NavMeshAgent agent;
    private Coroutine currentTaskCoroutine;
    private bool isPaused = false;
    Vector3 TargetPosition;
    private Vector3 lastPosition;
    public float FuelRate = 15.8f; // Fuel consumption at liters per hour
    private String ZoneEndTemp = "A0";

    void Start()
    {
        MachineType = "Truck"; // Set the machine type for this behavior
        tankCapacity = 500.0f; // Set the fuel tank capacity
        StartCoroutine(FuelConsumptionCounter(FuelRate));
    }

    public override IEnumerator ExecuteTask(TaskData task)
    {
        Debug.Log($"Truck received task: {task.TaskType}");
        Working = true;

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = true; // Ensure updateRotation is true
        }

        if (!OnOff)
        {
            Debug.LogWarning("Truck is turned off. Cannot execute task.");
            yield return null;
        }

        switch (task.TaskType)
        {
            case "Move":

                Moving = true;
                // Call the MoveToPosition coroutine
                // Send raycast at target position to find a valid y position
                RaycastHit hit;
                // Draw a debug ray to visualize the raycast
                Debug.Log("Truck moving to position... " + task.Position);
                TargetPosition = new Vector3(task.Position.x, 100f, task.Position.z);
                Debug.DrawRay(TargetPosition, Vector3.down * 10, Color.red, 500f);
                if (Physics.Raycast(new Vector3(task.Position.x, 100f, task.Position.z), Vector3.down, out hit, Mathf.Infinity))
                {
                    TargetPosition.y = hit.point.y;
                }
                else
                {
                    Debug.LogWarning("Failed to find valid target position.");
                    yield break;
                }

                Debug.LogWarning("New Position" + TargetPosition);

                if (agent == null)
                {
                    Debug.LogError("Truck does not have a NavMeshAgent component!");
                    yield break;
                }

                // set the destination of the agent to the target position
                agent.SetDestination(TargetPosition);

                // Initialize tracking variables
                lastPosition = transform.position;

                currentTaskCoroutine = StartCoroutine(CheckPosition());
                yield return currentTaskCoroutine;
                Moving = false;
                break;
            case "MoveZone":
                Moving = true;
                // Search the zone object
                GameObject ZoneEnd = GameObject.Find(task.TargetZone);

                ZoneEndTemp = task.TargetZone;

                // find the node inside the zone

                Transform[] objlst = ZoneEnd.GetComponentsInChildren<Transform>();


                var endNodes = ZoneEnd.GetComponentsInChildren<Transform>().Where(t => t.CompareTag("GateTrigger")).ToList();
                if (endNodes.Count == 0)
                {
                    Debug.LogWarning($"No gate nodes found in zone {ZoneEnd}");
                    yield break;
                }

                // Find the closest node to the machine
                Transform closestEndNode = endNodes[0];
                float minEndDistance = Vector3.Distance(transform.position, closestEndNode.position);
                foreach (var node in endNodes)
                {
                    float distance = Vector3.Distance(transform.position, node.position);
                    if (distance < minEndDistance)
                    {
                        minEndDistance = distance;
                        closestEndNode = node;
                    }
                }

                if (agent == null)
                {
                    Debug.LogError("Truck does not have a NavMeshAgent component!");
                    yield break;
                }
                TargetPosition = closestEndNode.position;
                // offset the targe position a bit in direction to the center of the zone (gameobject)
                Debug.Log("Moving to zone end: " + ZoneEnd.name + " at " + TargetPosition);
                Vector3 offsetDirection = (ZoneEnd.transform.position - TargetPosition).normalized; // Away from gate
                Debug.Log("Offset Direction: " + offsetDirection);
                TargetPosition += offsetDirection * 50f;
                Debug.Log("Offset Position: " + TargetPosition);

                // set the destination of the agent to the target position
                agent.SetDestination(TargetPosition);

                // Initialize tracking variables
                lastPosition = transform.position;

                currentTaskCoroutine = StartCoroutine(CheckPosition());
                yield return currentTaskCoroutine;
                Moving = false;
                break;
            case "Unload":
                Moving = false;
                yield return StartCoroutine(Unload());
                break;

            case "WaitForLoad":
                Moving = false;

                yield return StartCoroutine(WaitForLoad(task.MachineId)); // Pass any specific required load count if necessary
                break;

            default:
                Moving = false;

                Debug.LogWarning($"Truck received unsupported task: {task.TaskType}");
                yield return null;
                break;
        }

        Debug.Log($"Task {task.TaskType} completed.");
        Working = false;
    }

    private IEnumerator MoveToPosition(Vector3 position)
    {
        Moving = true;
        // Send raycast at target position to find a valid y position
        RaycastHit hit;
        // Draw a debug ray to visualize the raycast
        Debug.Log("Truck moving to position... " + position);
        TargetPosition = new Vector3(position.x, 100f, position.z);
        Debug.DrawRay(TargetPosition, Vector3.down * 10, Color.red, 500f);
        if (Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out hit, Mathf.Infinity))
        {
            TargetPosition.y = hit.point.y;
        }
        else
        {
            Debug.LogWarning("Failed to find valid target position.");
            yield break;
        }

        Debug.LogWarning("New Position" + TargetPosition);

        if (agent == null)
        {
            Debug.LogError("Truck does not have a NavMeshAgent component!");
            yield break;
        }

        // set the destination of the agent to the target position
        agent.SetDestination(TargetPosition);

        // Initialize tracking variables
        lastPosition = transform.position;

        currentTaskCoroutine = StartCoroutine(CheckPosition(true));
        yield return currentTaskCoroutine;
        Moving = false;
    }

    private IEnumerator CheckPosition(bool ignorePause = false)
    {
        Debug.Log("Truck moving to position...");

        // little delay to allow the agent to start moving
        yield return new WaitForSeconds(1f);

        // Wait until the agent reaches the destination position by checking the remaining distance
        while (agent.remainingDistance > agent.stoppingDistance)
        {
            while (isPaused && !ignorePause)
            {
                yield return null; // Wait until unpaused
            }

            // Calculate distance traveled since the last frame
            Vector3 currentPosition = transform.position;
            float distanceThisFrame = Vector3.Distance(currentPosition, lastPosition);
            DistanceTraveled += distanceThisFrame;
            lastPosition = currentPosition;

            yield return null;
        }
        // Clear the destination position
        TargetPosition = Vector3.zero;
        Debug.LogWarning(isPaused + " - " + ignorePause + " Reached: " + agent.remainingDistance);
    }

    private IEnumerator Unload()
    {
        if (loadCount > 0)
        {
            Debug.Log("Truck started unloading.");
            yield return new WaitForSeconds(5f); // Simulate unloading delay
            loadCount = 0; // Reset load count after unloading
            Debug.Log("Truck finished unloading.");
        }
        else
        {
            Debug.LogWarning("Truck attempted to unload with no load.");
            yield return null;
        }
    }

    private IEnumerator WaitForLoad(int requiredLoadCount)
    {
        Debug.Log($"Truck waiting to receive {requiredLoadCount} loads...");
        while (loadCount < requiredLoadCount)
        {
            yield return null; // Wait until the required load count is met
        }
        Debug.Log($"Truck has received {requiredLoadCount} loads.");
    }

    public void RegisterLoad()
    {
        loadCount++;
        Debug.Log($"Truck received a load. Current load count: {loadCount}");
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, moveSpeed * Time.deltaTime);
        }
    }

    private RoadGenerator RoadGenerator;
    private Vector3 SaveTargetPosition;

    public override void OnEnterZone()
    {
        if (RoadGenerator == null)
        {
            RoadGenerator = FindObjectOfType<RoadGenerator>();
        }

        ChangeToZoneSystem();
    }

    private void ChangeToZoneSystem()
    {
        Debug.Log("Truck is entering the zone. Changing to zone system...");
        // Stop the coroutine 
        if (currentTaskCoroutine != null)
        {
            StopCoroutine(currentTaskCoroutine);
        }
        // set the destination of the agent to the target position
        agent.SetDestination(SaveTargetPosition);

        // unpause the agent
        StartCoroutine(delaySwitch());
    }

    private IEnumerator delaySwitch()
    {
        yield return new WaitForSeconds(1f);
        isPaused = false;
    }

    public override void OnExitZone(String zoneName)
    {
        if (RoadGenerator == null)
        {
            RoadGenerator = FindObjectOfType<RoadGenerator>();
        }

        ChangeToRoadSystem(zoneName);
    }

    private void ChangeToRoadSystem(String zoneName)
    {
        isPaused = true;
        Debug.Log("Truck is exiting the zone. Changing to road system...");

        // Check the targetPostion is not zero
        if (TargetPosition == Vector3.zero)
        {
            Debug.LogWarning("Target position is not set.");
            return;
        }

        // Save the target position
        SaveTargetPosition = TargetPosition;

        // Ask the RoadManager to give the path to the destination
        List<String> path = RoadGenerator.GetPathToDestination(zoneName, ZoneEndTemp, gameObject);

        Debug.Log("Path to destination: " + string.Join(" -> ", path.ToArray()));

        // remove the first node from the path as it is the current node
        path.RemoveAt(0);


        // Start a new coroutine to move along the path
        StartCoroutine(FollowPath(path, ZoneEndTemp));
    }

    private IEnumerator FollowPath(List<String> path, String ZoneEnd)
    {
        GameObject roadSyst = GameObject.Find("RoadSystem");
        int pathCount = path.Count;
        int currentIndex = 0;

        foreach (String node in path)
        {
            currentIndex++;
            // if last iteration then search the node inside the End zone
            if (currentIndex == pathCount)
            {
                // Search the zone object
                GameObject zone = GameObject.Find(ZoneEnd);
                // find the node inside the zone
                Transform nodeTransform = zone.transform.Find(node);
                if (nodeTransform != null)
                {
                    Debug.Log("Moving to last node: " + node);
                    // Find the target position for the node from the roadSyst by name and appending "Node_" to the name
                    Vector3 targetPositionLast = nodeTransform.position;
                    // Move to the target position
                    yield return StartCoroutine(MoveToPosition(targetPositionLast));

                    break;
                }
                else
                {
                    Debug.LogWarning("Node not found in the zone.");
                    yield break;
                }
            }


            Debug.Log("Moving to node: " + node);

            // Find the target position for the node from the roadSyst by name and appending "Node_" to the name
            GameObject targetNode = roadSyst.transform.Find("Node_" + node).gameObject;
            Vector3 targetPosition = targetNode.transform.position;
            Debug.Log("NODE Target Position: " + targetPosition);
            // Move to the target position
            yield return StartCoroutine(MoveToPosition(targetPosition));
        }
    }
}
