using UnityEngine;

public class LineSegmentSelector : MonoBehaviour
{
    public Vector3 StartPoint { get; private set; }
    public Vector3 EndPoint { get; private set; }
    public int ZoneIndex { get; private set; }
    public int SegmentIndex { get; private set; }

    public void Initialize(Vector3 start, Vector3 end, int zoneIndex, int segmentIndex)
    {
        StartPoint = start;
        EndPoint = end;
        ZoneIndex = zoneIndex;
        SegmentIndex = segmentIndex;
    }

    void OnMouseOver()
    {
        // This function is called every frame while the mouse is over the collider
        if (Input.GetMouseButtonDown(0)) // Checks if the left mouse button was clicked
        {
            SelectSegment();
        }
    }

    private void SelectSegment()
    {
        Debug.Log($"Segment {SegmentIndex} of Zone {ZoneIndex} selected.");
        // Implement additional functionality for what happens when a segment is selected
    }
}
