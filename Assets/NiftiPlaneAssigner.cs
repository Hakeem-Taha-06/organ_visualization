using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class NiftiPlaneAssigner : MonoBehaviour
{
    [Header("References")]
    public NiftiLoader niftiLoader;

    [Header("Plane Settings")]
    public NiftiLoader.Orientation orientation = NiftiLoader.Orientation.Axial;

    [Header("Position Mapping")]
    public Vector3 volumeCenter = Vector3.zero;
    public Vector3 volumeSize = new Vector3(256, 256, 256); // Physical size in Unity units

    [Header("Display Settings")]
    [Range(0f, 1f)]
    public float normalizedPosition = 0.5f; // 0 to 1 along the slice axis
    public bool autoUpdate = true;
    public bool useTransformPosition = true; // If true, uses plane's position instead of normalizedPosition
    public bool usePhysicalVoxelSize = false; // If true, uses actual voxel dimensions from NIfTI header

    [Header("Scaling")]
    public float scaleMultiplier = 1f; // Overall scale factor
    public bool maintainAspectRatio = true; // Keep texture aspect ratio
    public bool usePhysicalScale = false; // Scale based on actual voxel dimensions

    private MeshRenderer meshRenderer;
    private Material planeMaterial;
    private Texture2D currentTexture;
    private int currentSliceIndex = -1;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // Create a unique material instance for this plane
        planeMaterial = new Material(Shader.Find("Unlit/Texture"));
        meshRenderer.material = planeMaterial;

        if (niftiLoader == null)
        {
            niftiLoader = FindAnyObjectByType<NiftiLoader>();
            if (niftiLoader == null)
            {
                Debug.LogError("NiftiLoader not found! Please assign it in the inspector.");
                return;
            }
        }

        // If using physical voxel size, update volume size from NIfTI header
        // NOTE: GetVoxelSize() returns Unity space coordinates (Y/Z swapped)
        if (usePhysicalVoxelSize && niftiLoader.IsLoaded)
        {
            Vector3 voxelSize = niftiLoader.GetVoxelSize(); // Already in Unity space
            volumeSize = new Vector3(
                niftiLoader.Width * voxelSize.x,      // X dimension
                niftiLoader.Depth * voxelSize.y,      // Unity Y = NIfTI Z (depth)
                niftiLoader.Height * voxelSize.z      // Unity Z = NIfTI Y (height)
            );
            Debug.Log($"Using physical volume size (Unity space): {volumeSize}");
        }

        UpdateSlice();
    }

    void Update()
    {
        if (autoUpdate && niftiLoader != null && niftiLoader.IsLoaded)
        {
            UpdateSlice();
        }
    }

    public void UpdateSlice()
    {
        if (niftiLoader == null || !niftiLoader.IsLoaded) return;

        int sliceIndex = GetSliceIndexFromPosition();

        // Only update texture if slice index changed
        if (sliceIndex != currentSliceIndex)
        {
            currentSliceIndex = sliceIndex;
            SetSliceTexture(sliceIndex);
        }
    }

    private int GetSliceIndexFromPosition()
    {
        float normalizedPos = normalizedPosition;

        if (useTransformPosition)
        {
            // Convert world position to normalized position (0-1) along the slice axis
            Vector3 localPos = transform.position - volumeCenter;

            switch (orientation)
            {
                case NiftiLoader.Orientation.Axial:
                    // Axial slices through Z (depth in NIfTI) = Y in Unity
                    normalizedPos = Mathf.InverseLerp(-volumeSize.y / 2f, volumeSize.y / 2f, localPos.y);
                    break;
                case NiftiLoader.Orientation.Sagittal:
                    // Sagittal slices through X (same in both)
                    normalizedPos = Mathf.InverseLerp(-volumeSize.x / 2f, volumeSize.x / 2f, localPos.x);
                    break;
                case NiftiLoader.Orientation.Coronal:
                    // Coronal slices through Y (height in NIfTI) = Z in Unity
                    normalizedPos = Mathf.InverseLerp(-volumeSize.z / 2f, volumeSize.z / 2f, localPos.z);
                    break;
            }
        }

        // Clamp and convert to slice index
        normalizedPos = Mathf.Clamp01(normalizedPos);
        int sliceCount = niftiLoader.GetSliceCount(orientation);
        int sliceIndex = Mathf.RoundToInt(normalizedPos * (sliceCount - 1));

        return sliceIndex;
    }

    private void SetSliceTexture(int sliceIndex)
    {
        // Clean up old texture
        if (currentTexture != null)
        {
            Destroy(currentTexture);
        }

        // Get new slice texture
        currentTexture = niftiLoader.GetSliceTexture(orientation, sliceIndex);

        if (currentTexture != null && planeMaterial != null)
        {
            planeMaterial.mainTexture = currentTexture;

            // Adjust plane scale to match texture aspect ratio
            AdjustPlaneScale();
        }
    }

    private void AdjustPlaneScale()
    {
        if (currentTexture == null) return;

        Vector3 scale = transform.localScale;
        string scalingMode = "";

        if (usePhysicalScale && niftiLoader != null && niftiLoader.IsLoaded)
        {
            scalingMode = "Physical Scale";
            // Use actual voxel dimensions from NIfTI header (in Unity space)
            Vector3 voxelSize = niftiLoader.GetVoxelSize(); // Already Y/Z swapped for Unity
            Vector3 voxelSizeNifti = niftiLoader.GetVoxelSizeNifti(); // Original NIfTI space

            switch (orientation)
            {
                case NiftiLoader.Orientation.Axial:
                    // XY plane in NIfTI space
                    // Texture width = NIfTI X, Texture height = NIfTI Y
                    scale.x = currentTexture.width * voxelSizeNifti.x * scaleMultiplier;  // NIfTI X
                    scale.z = currentTexture.height * voxelSizeNifti.y * scaleMultiplier; // NIfTI Y -> Unity Z
                    break;
                case NiftiLoader.Orientation.Sagittal:
                    // YZ plane in NIfTI space
                    // Texture width = NIfTI Z, Texture height = NIfTI Y
                    scale.x = currentTexture.width * voxelSizeNifti.z * scaleMultiplier;  // NIfTI Z -> Unity Y (but horizontal)
                    scale.z = currentTexture.height * voxelSizeNifti.y * scaleMultiplier; // NIfTI Y -> Unity Z
                    break;
                case NiftiLoader.Orientation.Coronal:
                    // XZ plane in NIfTI space
                    // Texture width = NIfTI X, Texture height = NIfTI Z
                    scale.x = currentTexture.width * voxelSizeNifti.x * scaleMultiplier;  // NIfTI X
                    scale.z = currentTexture.height * voxelSizeNifti.z * scaleMultiplier; // NIfTI Z -> Unity Y (but vertical in texture)
                    break;
            }
        }
        else if (maintainAspectRatio)
        {
            scalingMode = "Aspect Ratio";
            // Maintain aspect ratio based on texture dimensions
            float textureAspect = (float)currentTexture.width / currentTexture.height;

            switch (orientation)
            {
                case NiftiLoader.Orientation.Axial:
                    // XY plane (but Y in NIfTI = Z in Unity)
                    scale.x = textureAspect * scaleMultiplier;
                    scale.z = 1f * scaleMultiplier;
                    break;
                case NiftiLoader.Orientation.Sagittal:
                    // YZ plane in NIfTI (Z/Y in Unity)
                    scale.x = textureAspect * scaleMultiplier;
                    scale.z = 1f * scaleMultiplier;
                    break;
                case NiftiLoader.Orientation.Coronal:
                    // XZ plane in NIfTI (X and Z->Y in Unity)
                    scale.x = textureAspect * scaleMultiplier;
                    scale.z = 1f * scaleMultiplier;
                    break;
            }
        }
        else
        {
            scalingMode = "Uniform";
            // Uniform scaling - ignore aspect ratio
            switch (orientation)
            {
                case NiftiLoader.Orientation.Axial:
                    scale.x = scaleMultiplier;
                    scale.z = scaleMultiplier;
                    break;
                case NiftiLoader.Orientation.Sagittal:
                    scale.x = scaleMultiplier;
                    scale.z = scaleMultiplier;
                    break;
                case NiftiLoader.Orientation.Coronal:
                    scale.x = scaleMultiplier;
                    scale.z = scaleMultiplier;
                    break;
            }
        }

        transform.localScale = scale;

        // Debug output for scaling and dimensions
        Debug.Log($"[{gameObject.name}] {orientation} Plane Scaling Debug:\n" +
                  $"  Mode: {scalingMode}\n" +
                  $"  Texture Dimensions: {currentTexture.width}x{currentTexture.height}px\n" +
                  $"  Slice Index: {currentSliceIndex}\n" +
                  $"  Scale Multiplier: {scaleMultiplier}\n" +
                  $"  Final Unity Scale: {scale}\n" +
                  $"  Transform Position: {transform.position}");

        if (usePhysicalScale && niftiLoader != null && niftiLoader.IsLoaded)
        {
            Vector3 voxelSizeNifti = niftiLoader.GetVoxelSizeNifti();
            Debug.Log($"  Voxel Size (NIfTI): {voxelSizeNifti}\n" +
                      $"  Voxel Size (Unity): {niftiLoader.GetVoxelSize()}\n" +
                      $"  Physical Size (mm): ({scale.x:F2}, {scale.y:F2}, {scale.z:F2})");
        }
    }

    public void SetNormalizedPosition(float normalizedPos)
    {
        normalizedPosition = Mathf.Clamp01(normalizedPos);
        useTransformPosition = false;
        UpdateSlice();
    }

    public void SetSliceIndex(int index)
    {
        if (niftiLoader == null || !niftiLoader.IsLoaded) return;

        int sliceCount = niftiLoader.GetSliceCount(orientation);
        normalizedPosition = Mathf.Clamp01((float)index / (sliceCount - 1));
        useTransformPosition = false;
        UpdateSlice();
    }

    public int GetCurrentSliceIndex()
    {
        return currentSliceIndex;
    }

    void OnDestroy()
    {
        // Clean up texture and material
        if (currentTexture != null)
        {
            Destroy(currentTexture);
        }
        if (planeMaterial != null)
        {
            Destroy(planeMaterial);
        }
    }

    // Helper to visualize the slice position in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw volume bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(volumeCenter, volumeSize);
    }
}