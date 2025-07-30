using System;
using System.Collections.Generic;
using UnityEngine;

public static class PolygonUtils
{
    public static bool DoLinesIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        // Convert 3D vectors to 2D by ignoring the y-coordinate, focusing on the XZ plane
        return DoLinesIntersect2D(new Vector2(p1.x, p1.z), new Vector2(q1.x, q1.z),
                                  new Vector2(p2.x, p2.z), new Vector2(q2.x, q2.z));
    }

    // Check if two 2D line segments intersect
    private static bool DoLinesIntersect2D(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Utility functions to determine the orientation of the order of points
        int orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            if (val == 0) return 0;  // Collinear
            return (val > 0) ? 1 : 2; // Clock or counterclockwise
        }

        bool onSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
                return true;
            return false;
        }

        // Find the four orientations needed for the general and special cases
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and p2 are collinear and q2 lies on segment p1q1
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases
    }

    public static bool IsPointOnLineSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        // Calculate the closest point on the line segment to 'point'
        Vector3 closestPoint = GetClosestPointOnLine(start, end, point);

        // Check if the closest point is effectively the same as the given point (within a tolerance)
        return Vector3.Distance(point, closestPoint) < 0.01f; // Adjust tolerance as needed
    }

    public static Vector3 GetClosestPointOnLine(Vector3 start, Vector3 end, Vector3 point)
    {
        // Calculate the vector from start to end
        Vector3 lineDirection = end - start;
        float lineLengthSquared = lineDirection.sqrMagnitude; // Use squared length to avoid unnecessary square root

        // If the segment is actually a point (start == end), return start as the closest point
        if (lineLengthSquared == 0)
            return start;

        // Project point onto the line defined by start and end, and calculate the position along the line
        float t = Vector3.Dot(point - start, lineDirection) / lineLengthSquared;

        // Clamp t to the range [0, 1] to keep the point within the segment boundaries
        t = Mathf.Clamp01(t);

        // Interpolate to get the closest point on the line segment
        return start + t * lineDirection;
    }
    public static bool IsAngleValid(Vector3 previous, Vector3 current, Vector3 next, float minAngle, float maxAngle)
    {
        Vector3 v1 = (current - previous).normalized;
        Vector3 v2 = (next - current).normalized;
        float angle = 180 - Vector3.Angle(v1, v2);
        return angle >= minAngle && angle <= maxAngle;
    }

    public static bool PolygonsIntersect(List<Vector3> zoneA, List<Vector3> zoneB)
    {
        for (int i = 0; i < zoneA.Count; i++)
        {
            Vector3 p1 = zoneA[i];
            Vector3 p2 = zoneA[(i + 1) % zoneA.Count];
            for (int j = 0; j < zoneB.Count; j++)
            {
                Vector3 p3 = zoneB[j];
                Vector3 p4 = zoneB[(j + 1) % zoneB.Count];

                // Check if the segments are the same (shared edge)
                if ((p1 == p3 && p2 == p4) || (p1 == p4 && p2 == p3))
                {
                    continue; // Ignore shared edges
                }

                if (DoLinesIntersect(p1, p2, p3, p4))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool SelfIntersecting(List<Vector3> points)
    {
        int n = points.Count;
        if (n < 4) return false; // A polygon with fewer than 4 edges can't self-intersect

        for (int i = 0; i < n; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[(i + 1) % n];
            for (int j = i + 2; j < n; j++)
            {
                // Ensure we don't check adjacent segments or the segment that closes the polygon
                if (i == 0 && j == n - 1) continue;

                Vector3 p3 = points[j];
                Vector3 p4 = points[(j + 1) % n];

                if (PolygonUtils.DoLinesIntersect(p1, p2, p3, p4))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Helper to check if a point is on the perimeter
    public static bool IsPointOnPerimeter(Vector3 point, PolygonData mainPerimeter)
    {
        if (mainPerimeter == null || mainPerimeter.points == null)
        {
            Debug.LogWarning("ZoneManager: perimeterVertices is not set.");
            return false; // Or handle as appropriate for your logic
        }
        Vector3 pointOnXZ = new Vector3(point.x, 0, point.z); // Ensure point is projected onto x-z plane
        Debug.Log(mainPerimeter.Count);
        for (int i = 0; i < mainPerimeter.Count; i++)
        {

            // Get the current and next vertices in the perimeter
            Vector3 start = new Vector3(mainPerimeter.points[i].x, 0, mainPerimeter.points[i].z);
            Vector3 end = new Vector3(mainPerimeter.points[(i + 1) % mainPerimeter.Count].x, 0, mainPerimeter.points[(i + 1) % mainPerimeter.Count].z);


            // Create debug spheres for visualization
            // CreateDebugSphere(pointOnXZ, Color.red);
            // CreateDebugSphere(start, Color.blue);
            // CreateDebugSphere(end, Color.green);

            // Check if the point lies on the line segment between start and end
            if (PolygonUtils.IsPointOnLineSegment(pointOnXZ, start, end))
            {
                return true; // Point is on the perimeter edge
            }
        }

        return false;
    }

    public static float CalculateAreaOfPolygon(PolygonData perimeter)
    {
        if (perimeter == null || perimeter.points == null || perimeter.points.Count < 3)
        {
            Debug.LogWarning("PolygonUtils: Invalid perimeter data.");
            return 0;
        }

        float area = 0;
        for (int i = 0; i < perimeter.points.Count; i++)
        {
            Vector3 p1 = perimeter.points[i];
            Vector3 p2 = perimeter.points[(i + 1) % perimeter.points.Count];
            area += p1.x * p2.z - p2.x * p1.z;
        }
        return Mathf.Abs(area) / 2;
    }

    public static bool IsPointInPerimeter(Vector3 point, PolygonData mainPerimeter)
    {
        if (mainPerimeter == null || mainPerimeter.points == null)
        {
            Debug.LogWarning("ZoneManager: perimeterVertices is not set.");
            return false; // Or handle as appropriate for your logic
        }
        Vector3 pointOnXZ = new Vector3(point.x, 0, point.z); // Ensure point is projected onto x-z plane

        int intersections = 0;
        for (int i = 0; i < mainPerimeter.Count; i++)
        {
            // Get the current and next vertices in the perimeter
            Vector3 start = new Vector3(mainPerimeter.points[i].x, 0, mainPerimeter.points[i].z);
            Vector3 end = new Vector3(mainPerimeter.points[(i + 1) % mainPerimeter.Count].x, 0, mainPerimeter.points[(i + 1) % mainPerimeter.Count].z);

            // Check if the point lies on the line segment between start and end
            if (PolygonUtils.IsPointOnLineSegment(pointOnXZ, start, end))
            {
                return true; // Point is on the perimeter edge
            }

            // Check if the line segment intersects the ray from the point along the positive x-axis
            if (DoLinesIntersect(start, end, pointOnXZ, new Vector3(float.MaxValue, 0, point.z)))
            {
                intersections++;
            }
        }

        // If the number of intersections is odd, the point is inside the perimeter
        return intersections % 2 == 1;
    }
    public static int FindClosestSegmentIndex(List<Vector3> mainPerimeterPoints, Vector3 zonePoint)
    {
        int closestSegmentIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < mainPerimeterPoints.Count; i++)
        {
            Vector3 start = mainPerimeterPoints[i];
            Vector3 end = mainPerimeterPoints[(i + 1) % mainPerimeterPoints.Count];

            // Find the closest point on the segment (start, end) to zonePoint
            Vector3 closestPointOnSegment = GetClosestPointOnLine(start, end, zonePoint);
            float distance = Vector3.Distance(closestPointOnSegment, zonePoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSegmentIndex = i;
            }
        }

        return closestSegmentIndex;
    }


    //Calculate the area of a polygon
    public static float CalculateArea(List<Vector3> perimeter)
    {

        float scaleFactor = 3.6f;
        float area = 0;
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector3 p1 = perimeter[i];
            Vector3 p2 = perimeter[(i + 1) % perimeter.Count];
            area += p1.x * p2.z - p2.x * p1.z;
        }
        return (Mathf.Abs(area) / 2) / scaleFactor;

    }

    public static float CalculatePerimeter(List<Vector3> perimeter)
    {
        float perimeterLength = 0;
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector3 p1 = perimeter[i];
            Vector3 p2 = perimeter[(i + 1) % perimeter.Count];
            perimeterLength += Vector3.Distance(p1, p2);
        }
        return perimeterLength;
    }
}
