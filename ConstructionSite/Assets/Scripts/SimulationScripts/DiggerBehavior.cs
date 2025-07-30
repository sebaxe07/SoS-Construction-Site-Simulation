using UnityEngine;
using System.Collections;
using TaskManagement;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Linq;

public class DiggerBehavior : MachineBehavior
{
    public float moveSpeed = 5f;
    public float digDuration = 10f;
    public GameObject groundMesh;
    public GameObject dirtPilePrefab;
    public float digRadius = 5f;
    public float digDepth = 0.5f;
    private NavMeshAgent agent;

    private Animator animator;
    private bool isDigging = false;

    private Coroutine currentTaskCoroutine;

    private bool isPaused = false;
    Vector3 TargetPosition;
    private Vector3 lastPosition;

    public float FuelRate = 10.5f; // Fuel consumption at liters per hour
    private String ZoneEndTemp = "A0";


    void Start()
    {
        MachineType = "Excavator"; // Set the machine type for this behavior
        tankCapacity = 300.0f; // Set the fuel tank capacity
        InitializeAnimator();
        StartCoroutine(FuelConsumptionCounter(FuelRate));
    }

    public override IEnumerator ExecuteTask(TaskData task)
    {
        Debug.Log($"Digger received task: {task.TaskType}");
        Working = true;

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = true; // Ensure updateRotation is true
        }


        if (!OnOff)
        {
            Debug.LogWarning("Machine is turned off.");
            yield break;
        }

        switch (task.TaskType)
        {
            case "Dig":
                yield return StartCoroutine(Dig(task.Position));
                break;

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
            case "FlattenTerrain":
                Moving = false;

                FlattenTerrain();
                yield return null; // Task is synchronous
                break;

            case "ModifyTerrain":
                Moving = false;

                ModifyTerrain(task.Position);
                yield return null; // Task is synchronous
                break;

            case "PlaceDirtPile":
                Moving = false;

                PlaceDirtPileBehind();
                yield return null; // Task is synchronous
                break;

            default:
                Moving = false;

                Debug.LogWarning($"Digger received unsupported task: {task.TaskType}");
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
        Debug.Log("Digger moving to position... " + position);
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
        Debug.Log("Digger moving to position...");

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

    private IEnumerator CostMovement(Vector3 Position)
    {
        Moving = true;
        // Call the MoveToPosition coroutine
        // Send raycast at target position to find a valid y position
        RaycastHit hit;
        // Draw a debug ray to visualize the raycast
        Debug.Log("Truck moving to position... " + Position);
        TargetPosition = new Vector3(Position.x, 100f, Position.z);
        Debug.DrawRay(TargetPosition, Vector3.down * 10, Color.red, 500f);
        if (Physics.Raycast(new Vector3(Position.x, 100f, Position.z), Vector3.down, out hit, Mathf.Infinity))
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
    }

    private IEnumerator Dig(Vector3 digPos)
    {
        Debug.Log("Digger started digging.");
        isDigging = true;
        Debug.Log("Digger moving to dig position...");
        yield return CostMovement(digPos);

        Debug.Log("Digger reached dig position.");
        PlayDiggingAnimation();

        yield return new WaitForSeconds(digDuration);

        StopDiggingAnimation();
        ModifyTerrain(transform.position);
        PlaceDirtPileBehind();

        Debug.Log("Digger finished digging.");
        isDigging = false;
    }

    public void FlattenTerrain()
    {
        Terrain terrain = groundMesh.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found on the groundMesh GameObject.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        float[,] flatHeights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int x = 0; x < flatHeights.GetLength(0); x++)
        {
            for (int y = 0; y < flatHeights.GetLength(1); y++)
            {
                flatHeights[x, y] = 0.028f;
            }
        }
        terrainData.SetHeights(0, 0, flatHeights);
        Debug.Log("Terrain flattened.");
    }

    public void ModifyTerrain(Vector3 stopPosition)
    {
        Vector3 offsetDirection = transform.right;
        Vector3 digPosition = stopPosition + offsetDirection * 5f;

        Terrain terrain = groundMesh.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found on the groundMesh GameObject.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        Vector3 terrainPosition = digPosition - terrain.transform.position;

        float normalizedX = terrainPosition.x / terrainData.size.x;
        float normalizedZ = terrainPosition.z / terrainData.size.z;

        int xCenter = Mathf.FloorToInt(normalizedX * terrainData.heightmapResolution);
        int zCenter = Mathf.FloorToInt(normalizedZ * terrainData.heightmapResolution);

        int digRadiusInHeightmap = Mathf.FloorToInt((digRadius / terrainData.size.x) * terrainData.heightmapResolution);
        int diameter = digRadiusInHeightmap * 2;

        float[,] heights = terrainData.GetHeights(
            Mathf.Max(0, xCenter - digRadiusInHeightmap),
            Mathf.Max(0, zCenter - digRadiusInHeightmap),
            Mathf.Min(diameter, terrainData.heightmapResolution - xCenter),
            Mathf.Min(diameter, terrainData.heightmapResolution - zCenter)
        );

        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int z = 0; z < heights.GetLength(1); z++)
            {
                float distance = Vector2.Distance(new Vector2(x, z), new Vector2(digRadiusInHeightmap, digRadiusInHeightmap));
                if (distance < digRadiusInHeightmap)
                {
                    heights[x, z] -= digDepth * (1 - (distance / digRadiusInHeightmap));
                }
            }
        }

        terrainData.SetHeights(
            Mathf.Max(0, xCenter - digRadiusInHeightmap),
            Mathf.Max(0, zCenter - digRadiusInHeightmap),
            heights
        );

        Debug.Log($"Terrain modified at offset position: {digPosition}");
    }

    public void PlaceDirtPileBehind()
    {
        Vector3 behindPosition = transform.position - transform.right * 5f;
        if (Physics.Raycast(behindPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit))
        {
            behindPosition.y = hit.point.y;
        }
        Instantiate(dirtPilePrefab, behindPosition, Quaternion.identity);
        Debug.Log("Dirt pile placed at: " + behindPosition);
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

    private void InitializeAnimator()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found.");
        }
    }

    private void PlayDiggingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDigging", true);
        }
    }

    private void StopDiggingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDigging", false);
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
        Debug.Log("Digger is entering the zone. Changing to zone system...");
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
        Debug.Log("Digger is exiting the zone. Changing to road system...");

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
