using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using Unity.AI.Navigation;

public class ZonePopulator : MonoBehaviour
{
    public SelectedSiteData selectedSiteData;
    public SimulationManager simulationManager;
    public RoadGenerator roadGenerator;
    private ConstructionSiteDataInfo constructionSiteData;

    [SerializeField] public float heightOffset = 10.0f;
    // List of zones
    [SerializeField] public List<Zone> zones = new List<Zone>();

    [SerializeField] private GameObject constructionFencePrefab;

    [Header("Terrain Excavation Settings")]
    [SerializeField] public float depth = 0.6f;  // Depth of the hole
    [SerializeField] private float perimeterBuffer = 0.1f;
    [SerializeField] private Terrain terrain;

    [Header("Terrain Demolition Settings")]
    [SerializeField] private GameObject demolitionPrefab;

    [Header("Terrain Concrete Settings")]
    [SerializeField] private GameObject concretePrefab;

    [Header("Terrain Building Settings")]
    [SerializeField] private GameObject buildingPrefab;

    private TerrainData terrainData;
    private int heightmapResolution;
    public NavMeshSurface navMeshSurface;



    private float[,] originalHeights;


    private void Start()
    {
        // Save the original terrain heightmap data
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            Debug.Log("Terrain Data " + terrainData.heightmapResolution);
            heightmapResolution = terrainData.heightmapResolution;
            originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        }

        LoadJsonData();
        BuildZones();
        BakeNavMesh();
        roadGenerator.StartRoadGeneration();

        simulationManager.StartSimulation();

    }
    private void OnApplicationQuit()
    {
        Debug.Log("Application is quitting. Resetting terrain to original state.");
        ResetTerrain(); // Called on application quit
    }


    public void ResetTerrain()
    {
        if (terrain != null && originalHeights != null)
        {
            Debug.Log("Resetting terrain to original state. " + originalHeights.Length);
            terrain.terrainData.SetHeights(0, 0, originalHeights);
        }
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

    public ConstructionSite selectedSite = null;
    private void BuildZones()
    {

        if (selectedSiteData.siteIndex >= 0 && selectedSiteData.siteIndex < constructionSiteData.ConstructionSites.Count)
        {
            selectedSite = constructionSiteData.ConstructionSites[selectedSiteData.siteIndex];
        }
        else if (!string.IsNullOrEmpty(selectedSiteData.siteName))
        {
            selectedSite = constructionSiteData.ConstructionSites.Find(site => site.Name == selectedSiteData.siteName);
        }

        if (selectedSite != null)
        {
            foreach (var zone in selectedSite.Layout.Zones)
            {
                CreateZone(zone);
            }
        }
        else
        {
            Debug.LogError("Selected site not found.");
        }
    }

    private void CreateZone(Zone zoneData)
    {
        // Create a new GameObject for the zone
        GameObject zoneObject = new GameObject(zoneData.Name);
        List<Vector3> vertices = new List<Vector3>();
        foreach (var vertex in zoneData.Vertices)
        {
            vertices.Add(vertex.ToVector3());
        }
        Vector3 centroid = CalculateCentroid(vertices);
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(centroid.x, centroid.y + 100f, centroid.z), Vector3.down, out hit, Mathf.Infinity))
        {
            centroid.y = hit.point.y;
        }
        zoneObject.transform.position = centroid;


        // Generate mesh from zone vertices
        Mesh mesh = CreateExtrudedMeshFromVertices(zoneData.Vertices);
        // Save the mesh to view and debug in the editor
#if UNITY_EDITOR
        AssetDatabase.CreateAsset(mesh, $"Assets/Resources/{zoneData.Name}.asset");
#endif

        zones.Add(zoneData);

        AddFenceAroundZone(zoneData, zoneObject);

        // Create a central hole if this is a digging zone
        if (zoneData.Type == "Excavation")
        {
            //Debug.Log("Creating digging hole in terrain for zone " + zoneData.Name);
            ApplyCustomBrushToTerrain(vertices, depth);
        }
        else if (zoneData.Type == "Demolition")
        {
            //Debug.Log("Creating demolition zone in terrain for zone " + zoneData.Name);
            // Instantiate the prefab at the adjusted position
            GameObject buildingInstance = Instantiate(demolitionPrefab, centroid, Quaternion.identity, zoneObject.transform);
        }
        else if (zoneData.Type == "Concrete")
        {
            //Debug.Log("Creating concrete zone in terrain for zone " + zoneData.Name);

            // Instantiate the prefab at the adjusted position
            GameObject concreteInstance = Instantiate(concretePrefab, centroid, Quaternion.identity, zoneObject.transform);

            // Calculate the offset from the prefab's pivot point to its base
            Renderer[] renderers = concreteInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float minY = float.MaxValue;
                foreach (Renderer renderer in renderers)
                {
                    minY = Mathf.Min(minY, renderer.bounds.min.y);
                }
                float offsetY = centroid.y - minY;
                concreteInstance.transform.position = new Vector3(centroid.x, centroid.y + offsetY - 5, centroid.z);
            }
            else
            {
                Debug.LogError("No renderers found in the concrete prefab.");
            }
        }
        else if (zoneData.Type == "Building")
        {
            //Debug.Log("Creating building zone in terrain for zone " + zoneData.Name);

            // Instantiate the prefab at the adjusted position
            GameObject concreteInstance = Instantiate(buildingPrefab, centroid, Quaternion.identity, zoneObject.transform);

            // Calculate the offset from the prefab's pivot point to its base
            Renderer[] renderers = concreteInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float minY = float.MaxValue;
                foreach (Renderer renderer in renderers)
                {
                    minY = Mathf.Min(minY, renderer.bounds.min.y);
                }
                float offsetY = centroid.y - minY;
                concreteInstance.transform.position = new Vector3(centroid.x, centroid.y + offsetY - 5, centroid.z);
            }
            else
            {
                Debug.LogError("No renderers found in the concrete prefab.");
            }
        }

        //Debug.Log($"Zone '{zoneData.Name}' of type '{zoneData.Type}' created.");


    }

    // Method to bake the NavMesh
    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            Debug.Log("Baking NavMesh...");
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baking completed!");
        }
        else
        {
            Debug.LogError("NavMeshSurface reference is missing!");
        }
    }


    private void ApplyCustomBrushToTerrain(List<Vector3> vertices, float depth)
    {
        // Step 1: Create Brush Texture
        Texture2D brushTexture = CreateBrushTexture(vertices);
        // Save the brush texture as an image to view and debug in the editor
#if UNITY_EDITOR
        byte[] bytes = brushTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/BrushTexture.png", bytes);
#endif
        // Step 2: Apply Brush to Terrain
        PaintTerrainWithBrush(brushTexture, vertices, depth, perimeterBuffer);
    }

    private Texture2D CreateBrushTexture(List<Vector3> vertices)
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // Fill texture with transparent color
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0, 0, 0, 0); // Transparent
        }

        texture.SetPixels(pixels);


        // Convert world vertices to texture coordinates
        List<Vector2> textureVertices = ConvertWorldToTextureCoordinates(vertices, 512);

        // Draw and fill the polygon
        DrawSmoothPolygon(texture, textureVertices, Color.black);
        FillPolygon(texture, textureVertices, Color.red);

        texture.Apply();
        return texture;
    }

    private static List<Vector2> ConvertWorldToTextureCoordinates(List<Vector3> worldVertices, int textureSize)
    {
        // Step 1: Find the bounding box in world space
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var vertex in worldVertices)
        {
            if (vertex.x < minX) minX = vertex.x;
            if (vertex.z < minY) minY = vertex.z;
            if (vertex.x > maxX) maxX = vertex.x;
            if (vertex.z > maxY) maxY = vertex.z;
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // Step 2: Convert world coordinates to texture coordinates
        List<Vector2> textureVertices = new List<Vector2>();
        foreach (var vertex in worldVertices)
        {
            float u = (vertex.x - minX) / width * (textureSize - 1);
            float v = (vertex.z - minY) / height * (textureSize - 1);
            textureVertices.Add(new Vector2(u, v));
        }

        return textureVertices;
    }

    private static void DrawSmoothPolygon(Texture2D texture, List<Vector2> vertices, Color color)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector2 p0 = vertices[i];
            Vector2 p1 = vertices[(i + 1) % vertices.Count];
            DrawSmoothLine(texture, p0, p1, color);
        }
    }

    private static void DrawSmoothLine(Texture2D texture, Vector2 p0, Vector2 p1, Color color)
    {
        int segments = 50;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = Vector2.Lerp(p0, p1, t);
            texture.SetPixel((int)point.x, (int)point.y, color);
        }
    }

    private static void FillPolygon(Texture2D texture, List<Vector2> vertices, Color fillColor)
    {
        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), vertices))
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }
        }
    }

    public static bool IsPointInPolygon(Vector2 point, List<Vector2> vertices)
    {
        bool inside = false;
        int j = vertices.Count - 1;
        for (int i = 0; i < vertices.Count; i++)
        {
            if ((vertices[i].y > point.y) != (vertices[j].y > point.y) &&
                point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private void PaintTerrainWithBrush(Texture2D brushTexture, List<Vector3> zoneVertices, float depth, float perimeterBuffer)
    {
        float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
        int brushWidth = brushTexture.width;
        int brushHeight = brushTexture.height;

        // Step 1: Find the bounding box of the zone in terrain coordinates
        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.z;

        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;

        foreach (Vector3 vertex in zoneVertices)
        {
            float normalizedX = (vertex.x - terrain.transform.position.x) / terrainWidth;
            float normalizedZ = (vertex.z - terrain.transform.position.z) / terrainHeight;

            if (normalizedX < minX) minX = normalizedX;
            if (normalizedZ < minZ) minZ = normalizedZ;
            if (normalizedX > maxX) maxX = normalizedX;
            if (normalizedZ > maxZ) maxZ = normalizedZ;
        }

        // Step 2: Apply the perimeter buffer to shrink the bounding box
        float bufferX = (maxX - minX) * perimeterBuffer;
        float bufferZ = (maxZ - minZ) * perimeterBuffer;

        minX += bufferX;
        minZ += bufferZ;
        maxX -= bufferX;
        maxZ -= bufferZ;

        // Convert bounding box from normalized coordinates to heightmap coordinates
        int startX = Mathf.RoundToInt(minX * heightmapResolution);
        int startY = Mathf.RoundToInt(minZ * heightmapResolution);
        int endX = Mathf.RoundToInt(maxX * heightmapResolution);
        int endY = Mathf.RoundToInt(maxZ * heightmapResolution);

        // Step 3: Apply the brush texture only within the adjusted bounding box
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                // Map terrain coordinates to brush texture coordinates
                float u = (float)(x - startX) / (endX - startX);
                float v = (float)(y - startY) / (endY - startY);
                int brushX = Mathf.RoundToInt(u * brushWidth);
                int brushY = Mathf.RoundToInt(v * brushHeight);

                // Check if brush texture has an effect here
                if (brushX >= 0 && brushY >= 0 && brushX < brushWidth && brushY < brushHeight)
                {
                    Color brushColor = brushTexture.GetPixel(brushX, brushY);
                    if (brushColor.a > 0) // Brush intensity (non-transparent areas)
                    {
                        heights[y, x] -= brushColor.a * depth; // Apply digging effect with brush alpha as intensity
                    }
                }
            }
        }

        // Apply modified heights back to the terrain
        terrainData.SetHeights(0, 0, heights);
    }

    public static Vector3 CalculateCentroid(List<Vector3> vertices)
    {
        Vector3 centroid = Vector3.zero;

        // Sum all the vertices
        foreach (var vertex in vertices)
        {
            centroid += vertex;
        }

        // Divide by the total number of vertices to get the average
        centroid /= vertices.Count;

        return centroid;
    }

    private void AddFenceAroundZone(Zone zoneData, GameObject zoneObject)
    {
        List<Vertex> vertices = zoneData.Vertices;

        // Create the ZoneController GameObject
        ZoneController zoneController = zoneObject.AddComponent<ZoneController>();

        for (int i = 0; i < vertices.Count; i++)
        {
            // Parent object for the fences
            GameObject fenceParent = new GameObject("Fences " + i);
            fenceParent.transform.parent = zoneObject.transform;
            fenceParent.transform.localPosition = Vector3.zero;

            // Get the current and next vertex (looping back to the start at the end)
            Vector3 start = vertices[i].ToVector3();
            Vector3 end = vertices[(i + 1) % vertices.Count].ToVector3();

            // Calculate the position and rotation for the fence segment
            Vector3 direction = (end - start).normalized;
            float length = Vector3.Distance(start, end);

            Transform fenceChild = constructionFencePrefab.transform.Find("SM_construction_fence_01a");
            if (fenceChild == null)
            {
                Debug.LogError("Child object 'SM_construction_fence_01a' not found in the prefab.");
                return;
            }
            float fenceSegmentLength = fenceChild.GetComponent<MeshFilter>().sharedMesh.bounds.size.z + 4.9f;
            int segmentCount = Mathf.CeilToInt(length / fenceSegmentLength);
            // Check if there is a gate in the current edge
            bool hasGate = false;
            foreach (var gate in zoneData.Gates)
            {
                if ((gate.StartVertex == i && gate.EndVertex == (i + 1) % vertices.Count) ||
                    (gate.StartVertex == (i + 1) % vertices.Count && gate.EndVertex == i))
                {
                    hasGate = true;
                    break;
                }
            }

            if (hasGate)
            {
                // Leave a space in the middle of the edge
                for (int j = 0; j < segmentCount; j++)
                {
                    // Calculate the position for each segment
                    Vector3 position = start + direction * (j * fenceSegmentLength);
                    position.y += 10f;

                    // Adjust the position based on the terrain height using raycasting
                    RaycastHit hit;
                    if (Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity))
                    {
                        position.y = hit.point.y;
                    }

                    // skip the middle segment and the 2 segments around it
                    if (j == segmentCount / 2 || j == segmentCount / 2 - 1 || j == segmentCount / 2 + 1)
                    {
                        continue;
                    }



                    // Instantiate the fence segment
                    GameObject fenceSegment = Instantiate(constructionFencePrefab, position, Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(-90, 0, -90), fenceParent.transform);
                    // If this is the last segment, scale it down to match the end of the edge
                    if (j == segmentCount - 1)
                    {
                        float remainingLength = length - (fenceSegmentLength * (segmentCount - 1));
                        Vector3 scale = fenceSegment.transform.localScale;
                        scale.x = remainingLength / fenceSegmentLength * scale.x;
                        fenceSegment.transform.localScale = scale;
                    }
                }


                // Calculate the position and size of the hole left by the missing fence segments
                Vector3 holeCenter = start + direction * (segmentCount / 2 * fenceSegmentLength + fenceSegmentLength / 2);
                float holeLength = fenceSegmentLength * 3; // The length of the hole is 3 segments

                // Create a new GameObject with a BoxCollider component
                GameObject gateTrigger = new GameObject("GateTrigger Side " + i);
                gateTrigger.transform.parent = fenceParent.transform;
                gateTrigger.transform.position = holeCenter;
                gateTrigger.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0, 90, 0);

                // Set the tag for the gate trigger to "GateTrigger"
                gateTrigger.tag = "GateTrigger";

                BoxCollider boxCollider = gateTrigger.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(holeLength, 10.0f, 5.0f);
                boxCollider.center = new Vector3(0, 4.0f, 0);

                // Attach the GateTrigger script to the gate trigger and set the ZoneController reference
                GateTrigger gateTriggerScript = gateTrigger.AddComponent<GateTrigger>();
                gateTriggerScript.zoneController = zoneController;
            }
            else
            {
                // Build the normal edge with the fence
                for (int j = 0; j < segmentCount; j++)
                {
                    // Calculate the position for each segment
                    Vector3 position = start + direction * (j * fenceSegmentLength);
                    // Increase the height of the fence to match the terrain
                    position.y += 10f;

                    // Adjust the position based on the terrain height using raycasting
                    RaycastHit hit;
                    if (Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity))
                    {
                        position.y = hit.point.y;
                    }

                    // Instantiate the fence segment
                    GameObject fenceSegment = Instantiate(constructionFencePrefab, position, Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(-90, 0, -90), fenceParent.transform);
                    // If this is the last segment, scale it down to match the end of the edge
                    if (j == segmentCount - 1)
                    {
                        float remainingLength = length - (fenceSegmentLength * (segmentCount - 1));
                        Vector3 scale = fenceSegment.transform.localScale;
                        scale.x = remainingLength / fenceSegmentLength * scale.x;
                        fenceSegment.transform.localScale = scale;
                    }
                }
            }
        }
    }

    private Mesh CreateExtrudedMeshFromVertices(List<Vertex> vertices)
    {
        Mesh mesh = new Mesh();

        // Temporal: Modify the Y points to a raycast to the ground level
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i].ToVector3();
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(vertex.x, 1000f, vertex.z), Vector3.down, out hit, Mathf.Infinity))
            {
                vertex.y = hit.point.y;
                vertices[i] = Vertex.FromVector3(vertex);
            }

        }

        // Convert vertices to Vector3 and create top and bottom vertices
        int vertexCount = vertices.Count;
        Vector3[] meshVertices = new Vector3[vertexCount * 2];

        for (int i = 0; i < vertexCount; i++)
        {
            // Bottom vertices
            meshVertices[i] = vertices[i].ToVector3();
            // Top vertices, offset by height
            meshVertices[i + vertexCount] = vertices[i].ToVector3() + Vector3.up * heightOffset;
        }

        // Create triangles for top and bottom faces
        List<int> triangles = new List<int>();

        // Top face (using ear clipping)
        for (int i = 1; i < vertexCount - 1; i++)
        {
            triangles.Add(vertexCount);  // Starting vertex for the top face
            triangles.Add(vertexCount + i);
            triangles.Add(vertexCount + i + 1);
        }

        // Bottom face
        for (int i = 1; i < vertexCount - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        // Create side faces by connecting each pair of bottom-top edges
        for (int i = 0; i < vertexCount; i++)
        {
            int nextIndex = (i + 1) % vertexCount;

            // First triangle for the quad (bottom vertex to next bottom and top vertex)
            triangles.Add(i);
            triangles.Add(nextIndex);
            triangles.Add(i + vertexCount);

            // Second triangle for the quad (top vertex to next bottom and next top vertex)
            triangles.Add(i + vertexCount);
            triangles.Add(nextIndex);
            triangles.Add(nextIndex + vertexCount);
        }

        // Set the mesh data
        mesh.vertices = meshVertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

}
