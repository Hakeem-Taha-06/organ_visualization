using UnityEngine;
using UnityEngine.Splines; // Make sure the Splines package is installed
using System.Collections.Generic;
using Unity.Mathematics; // Required for float3

// This class will be visible in the Inspector
[System.Serializable]
public class WaypointGroup
{
    [Tooltip("Just for your reference, e.g., 'Right Atrium'")]
    public string groupName;
    
    [Tooltip("All the parts that make up this single waypoint.")]
    public List<Transform> parts;
}

[RequireComponent(typeof(SplineContainer))]
public class PathGenerator : MonoBehaviour
{
    [Tooltip("Group multiple parts (like all wall sections) into a single waypoint.")]
    public List<WaypointGroup> waypointGroups;
    
    private SplineContainer targetSpline;

    [ContextMenu("Generate Spline From Waypoint Groups")]
    public void GenerateSpline()
    {
        targetSpline = GetComponent<SplineContainer>();
        if (targetSpline == null)
        {
            Debug.LogError("No SplineContainer found on this object!");
            return;
        }

        targetSpline.Spline.Clear(); // Clear any existing spline points
        var knots = new List<BezierKnot>();

        Debug.Log($"Starting spline generation... found {waypointGroups.Count} waypoint groups.");

        foreach (WaypointGroup group in waypointGroups)
        {
            if (group.parts == null || group.parts.Count == 0)
            {
                Debug.LogWarning($"Waypoint group '{group.groupName}' is empty, skipping.");
                continue;
            }

            // Calculate the combined center of all parts in this group
            Vector3 waypoint = GetCombinedWorldSpaceCenter(group.parts);
            
            knots.Add(new BezierKnot(new float3(waypoint.x, waypoint.y, waypoint.z)));
            Debug.Log($"Added knot for group: {group.groupName} at {waypoint}");
        }
        
        // Assign all the new knots to the spline at once
        targetSpline.Spline.Knots = knots;
        
        // This makes the spline smooth by default
        // targetSpline.Spline.SetAutoSmooth(true);

        Debug.Log("Spline generation complete!");
    }

    /// <summary>
    /// Calculates the center of the combined bounding box of all parts in the list.
    /// This gives the true center of the group, not just an average of pivots.
    /// </summary>
    private Vector3 GetCombinedWorldSpaceCenter(List<Transform> parts)
    {
        if (parts.Count == 0) return Vector3.zero;

        // If only one part, just get its center (faster)
        if (parts.Count == 1 && parts[0] != null)
        {
             return GetPartCenter(parts[0]);
        }
        
        // Create a new Bounds object that will grow to encompass all parts
        Bounds combinedBounds = new Bounds();
        bool boundsInitialized = false;

        foreach (Transform part in parts)
        {
            if (part == null) continue;
            
            // We use the Renderer's bounds, which is in world space
            Renderer partRenderer = part.GetComponent<Renderer>();
            if (partRenderer != null)
            {
                if (!boundsInitialized)
                {
                    combinedBounds = partRenderer.bounds; // Start with the first part's bounds
                    boundsInitialized = true;
                }
                else
                {
                    // Grow the combined bounds to include this part's bounds
                    combinedBounds.Encapsulate(partRenderer.bounds);
                }
            }
            else
            {
                // Fallback for objects without a renderer (e.g., empty pivots)
                if (!boundsInitialized)
                {
                     combinedBounds = new Bounds(part.position, Vector3.zero);
                     boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(part.position);
                }
            }
        }
        
        // The center of this new, large bounding box is our waypoint
        return boundsInitialized ? combinedBounds.center : Vector3.zero;
    }

    /// <summary>
    /// Finds the center of a single mesh part in world space.
    /// </summary>
    private Vector3 GetPartCenter(Transform partTransform)
    {
        // Best option: Renderer bounds
        Renderer renderer = partTransform.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.center;
        }

        // Good option: MeshFilter bounds
        MeshFilter meshFilter = partTransform.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Vector3 localCenter = meshFilter.sharedMesh.bounds.center;
            return partTransform.TransformPoint(localCenter);
        }

        // Fallback: Just the object's pivot point
        return partTransform.position;
    }
}