using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolygonFiller : MonoBehaviour
{

    public void Start()
    {
        // Setting the polygons Y position and Y scale manually so it is visible
        transform.position = new Vector3(transform.position.x, 200f, transform.position.z);  // Set Y to 200
        transform.localScale = new Vector3(transform.localScale.x, -1f, transform.localScale.z);  // Set Y scale to -1
    }

    // Create a polygon with the given vertices and color
    public void CreatePolygon(List<Vector3> vertices, Color color, string zoneName)
    {
        if (vertices == null || vertices.Count < 3)
        {
            Debug.LogError("A polygon requires at least 3 vertices!");
            return;
        }

        // Convert List<Vector3> to Vector2 for triangulation
        Vector2[] vertices2D = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
            vertices2D[i] = new Vector2(vertices[i].x, vertices[i].y);

        // Triangulate the vertices (2D only!)
        Triangulator triangulator = new Triangulator(vertices2D);
        int[] triangles = triangulator.Triangulate();

        // Create Mesh
        Mesh mesh = new Mesh();

        // Convert back to 3D vertices
        Vector3[] vertices3D = vertices.ToArray();

        mesh.vertices = vertices3D;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Assign the mesh to this GameObject
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Assign material and color
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = color;
        meshRenderer.material = material;

        CreateTextAtCenter(vertices3D, zoneName);
    }

    // Create a TextMeshPro object at the center of the polygon
    private void CreateTextAtCenter(Vector3[] vertices, string zoneName)
    {
        // Calculate the center of the polygon (simple average of vertices here)
        Vector3 center = Vector3.zero;
        foreach (Vector3 vertex in vertices)
        {
            center += vertex;
        }
        center /= vertices.Length;

        float width = CalculateHorizontalWidth(vertices, center.z);

        // Create a new GameObject for the text
        GameObject textObject = new GameObject("PolygonText");

        // Add TextMeshPro component to the GameObject
        TextMeshPro textMeshPro = textObject.AddComponent<TextMeshPro>();

        // Set text properties
        textMeshPro.text = $"<b>{zoneName}</b>"; // Add bold tags around the text
        textMeshPro.fontSize = 550;  // Set font size
        textMeshPro.alignment = TextAlignmentOptions.MidlineLeft; // Align the text to left
        textMeshPro.color = Color.black;  // Set the text color

        // Adjust RectTransform width
        RectTransform rectTransform = textMeshPro.rectTransform;
        rectTransform.sizeDelta = new Vector2(width, 100f); // Set width proportional to polygon's width
        rectTransform.pivot = new Vector2(0.15f, 0.5f); // Ensure the pivot is centered

        // Position the text at the center of the polygon
        textObject.transform.position = new Vector3(center.x, 300f, center.z);  // Set Y position to 300

        // Apply rotation to the text (90 degrees around the X-axis)
        textObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Scale the text object
        textObject.transform.localScale = new Vector3(1f, 1f, 1f);

    }

    // Calculate the horizontal width of the polygon at a given z-coordinate
    private float CalculateHorizontalWidth(Vector3[] vertices, float centerZ)
    {
        // Find intersection points with the horizontal line at centerZ
        List<float> intersectionXPoints = new List<float>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 edgeStart = vertices[i];
            Vector3 edgeEnd = vertices[(i + 1) % vertices.Length]; // Wrap around to the first vertex

            // Check if the edge intersects the horizontal line at centerZ
            if ((edgeStart.z <= centerZ && edgeEnd.z >= centerZ) || (edgeStart.z >= centerZ && edgeEnd.z <= centerZ))
            {
                // Calculate intersection point
                float t = (centerZ - edgeStart.z) / (edgeEnd.z - edgeStart.z);
                float intersectionX = edgeStart.x + t * (edgeEnd.x - edgeStart.x);

                intersectionXPoints.Add(intersectionX);
            }
        }

        // Calculate horizontal width
        if (intersectionXPoints.Count >= 2)
        {
            float minX = Mathf.Min(intersectionXPoints.ToArray());
            float maxX = Mathf.Max(intersectionXPoints.ToArray());
            return maxX - minX;
        }

        return 0f; // No valid width found
    }

}
