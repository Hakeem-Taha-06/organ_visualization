using UnityEngine;

public class AddMeshColliders : MonoBehaviour
{
    [Header("Parent object that holds all heart parts")]
    public Transform heartParent;

    [Header("Options")]
    public bool makeConvex = true;

    [ContextMenu("Add Mesh Colliders To All Children")]
    public void AddColliders()
    {
        if (heartParent == null)
        {
            Debug.LogWarning("Assign the heartParent first!");
            return;
        }

        int addedCount = 0;

        foreach (Transform child in heartParent.GetComponentsInChildren<Transform>())
        {
            // Skip the parent itself if desired
            if (child == heartParent) continue;

            // Add MeshCollider if missing
            MeshCollider col = child.GetComponent<MeshCollider>();
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();

            if (meshFilter != null && col == null)
            {
                col = child.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = meshFilter.sharedMesh;
                col.convex = makeConvex;
                addedCount++;
            }
        }

        Debug.Log($"✅ Added MeshColliders to {addedCount} child objects under {heartParent.name}");
    }
}
