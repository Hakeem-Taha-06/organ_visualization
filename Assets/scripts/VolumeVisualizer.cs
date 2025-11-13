using UnityEngine;

/// <summary>
/// Manages three orthogonal clipping planes that display 2D slices from a 3D volume
/// </summary>
[RequireComponent(typeof(NiftiVolumeLoader))]
public class VolumeVisualizer : MonoBehaviour
{
    [Header("Plane References")]
    public GameObject axialPlane;    // XY plane (Z slice)
    public GameObject sagittalPlane; // YZ plane (X slice)
    public GameObject coronalPlane;  // XZ plane (Y slice)

    [Header("Material Settings")]
    public Material sliceMaterial;
    public bool autoCreatePlanes = true;

    [Header("Visualization Settings")]
    [Range(0f, 1f)]
    public float windowLevel = 0.5f;
    [Range(0.1f, 2f)]
    public float windowWidth = 1f;

    // Component references
    private NiftiVolumeLoader volumeLoader;

    // Plane data
    private PlaneData axialData;
    private PlaneData sagittalData;
    private PlaneData coronalData;

    // Internal class to hold plane-specific data
    public class PlaneData
    {
        public GameObject planeObject;
        public MeshRenderer renderer;
        public Texture2D texture;
        public int currentSliceIndex;
        public Vector3 normalDirection;
        public SliceAxis axis;
    }

    public enum SliceAxis { X, Y, Z }

    void Start()
    {
        volumeLoader = GetComponent<NiftiVolumeLoader>();

        // Wait for volume to load
        if (volumeLoader.IsLoaded)
        {
            InitializeVisualization();
        }
        else
        {
            StartCoroutine(WaitForVolumeLoad());
        }
    }

    System.Collections.IEnumerator WaitForVolumeLoad()
    {
        while (!volumeLoader.IsLoaded)
        {
            yield return new WaitForSeconds(0.1f);
        }
        InitializeVisualization();
    }

    /// <summary>
    /// Initialize the three clipping planes
    /// </summary>
    void InitializeVisualization()
    {
        Debug.Log("Initializing volume visualization");

        // Create planes if they don't exist
        if (autoCreatePlanes)
        {
            CreatePlanesIfNeeded();
        }

        // Setup each plane
        axialData = SetupPlane(axialPlane, SliceAxis.Z, Vector3.forward, "Axial");
        sagittalData = SetupPlane(sagittalPlane, SliceAxis.X, Vector3.right, "Sagittal");
        coronalData = SetupPlane(coronalPlane, SliceAxis.Y, Vector3.up, "Coronal");

        // Scale the volume parent to match real-world dimensions
        Vector3 volumeScale = new Vector3(
            volumeLoader.Dimensions.x * volumeLoader.VoxelSpacing.x,
            volumeLoader.Dimensions.y * volumeLoader.VoxelSpacing.y,
            volumeLoader.Dimensions.z * volumeLoader.VoxelSpacing.z
        );
        transform.localScale = volumeScale;

        // Position planes at center of volume
        UpdatePlanePosition(axialData, 0.5f);
        UpdatePlanePosition(sagittalData, 0.5f);
        UpdatePlanePosition(coronalData, 0.5f);

        Debug.Log($"Volume visualization initialized. Scale: {volumeScale}");
    }

    /// <summary>
    /// Create plane GameObjects if they don't exist
    /// </summary>
    void CreatePlanesIfNeeded()
    {
        if (axialPlane == null)
        {
            axialPlane = CreatePlaneObject("Axial_Plane", Vector3.zero, Quaternion.identity);
        }

        if (sagittalPlane == null)
        {
            sagittalPlane = CreatePlaneObject("Sagittal_Plane", Vector3.zero,
                Quaternion.Euler(0, 90, 0));
        }

        if (coronalPlane == null)
        {
            coronalPlane = CreatePlaneObject("Coronal_Plane", Vector3.zero,
                Quaternion.Euler(90, 0, 0));
        }
    }

    /// <summary>
    /// Create a plane GameObject with a quad mesh
    /// </summary>
    GameObject CreatePlaneObject(string name, Vector3 position, Quaternion rotation)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = name;
        plane.transform.SetParent(transform);
        plane.transform.localPosition = position;
        plane.transform.localRotation = rotation;
        plane.transform.localScale = Vector3.one;

        // Remove collider
        Destroy(plane.GetComponent<Collider>());

        // Setup material
        if (sliceMaterial != null)
        {
            plane.GetComponent<MeshRenderer>().material = sliceMaterial;
        }

        // Add controller
        var controller = plane.AddComponent<ClippingPlaneController>();
        controller.visualizer = this;

        return plane;
    }

    /// <summary>
    /// Setup a clipping plane
    /// </summary>
    PlaneData SetupPlane(GameObject planeObj, SliceAxis axis, Vector3 normal, string name)
    {
        if (planeObj == null)
        {
            Debug.LogError($"Plane object for {name} is null!");
            return null;
        }

        PlaneData data = new PlaneData
        {
            planeObject = planeObj,
            renderer = planeObj.GetComponent<MeshRenderer>(),
            normalDirection = normal,
            axis = axis,
            currentSliceIndex = 0
        };

        // Create texture based on slice dimensions
        Vector2Int texSize = GetSliceTextureSize(axis);
        data.texture = new Texture2D(texSize.x, texSize.y, TextureFormat.RFloat, false);
        data.texture.filterMode = FilterMode.Bilinear;
        data.texture.wrapMode = TextureWrapMode.Clamp;

        // Apply texture to material
        if (data.renderer != null && data.renderer.material != null)
        {
            data.renderer.material.mainTexture = data.texture;
        }

        return data;
    }

    /// <summary>
    /// Get the texture size for a given slice axis
    /// </summary>
    Vector2Int GetSliceTextureSize(SliceAxis axis)
    {
        Vector3Int dims = volumeLoader.Dimensions;

        switch (axis)
        {
            case SliceAxis.X: // YZ plane
                return new Vector2Int(dims.z, dims.y);
            case SliceAxis.Y: // XZ plane
                return new Vector2Int(dims.x, dims.z);
            case SliceAxis.Z: // XY plane
                return new Vector2Int(dims.x, dims.y);
            default:
                return new Vector2Int(128, 128);
        }
    }

    /// <summary>
    /// Update a plane's position and regenerate its slice texture
    /// </summary>
    public void UpdatePlanePosition(PlaneData planeData, float normalizedPosition)
    {
        if (planeData == null || !volumeLoader.IsLoaded) return;

        // Clamp position to [0, 1]
        normalizedPosition = Mathf.Clamp01(normalizedPosition);

        // Calculate slice index
        int maxIndex = GetMaxSliceIndex(planeData.axis);
        int sliceIndex = Mathf.RoundToInt(normalizedPosition * maxIndex);
        sliceIndex = Mathf.Clamp(sliceIndex, 0, maxIndex);

        planeData.currentSliceIndex = sliceIndex;

        // Update plane position in local space
        Vector3 localPos = Vector3.zero;
        switch (planeData.axis)
        {
            case SliceAxis.X:
                localPos = new Vector3(normalizedPosition - 0.5f, 0, 0);
                break;
            case SliceAxis.Y:
                localPos = new Vector3(0, normalizedPosition - 0.5f, 0);
                break;
            case SliceAxis.Z:
                localPos = new Vector3(0, 0, normalizedPosition - 0.5f);
                break;
        }

        planeData.planeObject.transform.localPosition = localPos;

        // Regenerate slice texture
        GenerateSliceTexture(planeData);
    }

    /// <summary>
    /// Get the maximum slice index for a given axis
    /// </summary>
    int GetMaxSliceIndex(SliceAxis axis)
    {
        Vector3Int dims = volumeLoader.Dimensions;
        switch (axis)
        {
            case SliceAxis.X: return dims.x - 1;
            case SliceAxis.Y: return dims.y - 1;
            case SliceAxis.Z: return dims.z - 1;
            default: return 0;
        }
    }

    /// <summary>
    /// Generate a 2D slice texture from the 3D volume
    /// </summary>
    void GenerateSliceTexture(PlaneData planeData)
    {
        if (planeData == null || planeData.texture == null) return;

        float[,,] volume = volumeLoader.VolumeData;
        Vector3Int dims = volumeLoader.Dimensions;
        int slice = planeData.currentSliceIndex;

        int width = planeData.texture.width;
        int height = planeData.texture.height;

        // Extract slice data based on axis
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = 0f;

                // Sample from volume based on slice orientation
                switch (planeData.axis)
                {
                    case SliceAxis.X: // Sagittal (YZ plane)
                        if (slice < dims.x && y < dims.y && x < dims.z)
                            value = volume[slice, y, x];
                        break;

                    case SliceAxis.Y: // Coronal (XZ plane)
                        if (x < dims.x && slice < dims.y && y < dims.z)
                            value = volume[x, slice, y];
                        break;

                    case SliceAxis.Z: // Axial (XY plane)
                        if (x < dims.x && y < dims.y && slice < dims.z)
                            value = volume[x, y, slice];
                        break;
                }

                // Apply window level/width adjustment
                value = ApplyWindowing(value);

                planeData.texture.SetPixel(x, y, new Color(value, value, value, 1));
            }
        }

        planeData.texture.Apply();
    }

    /// <summary>
    /// Apply window level/width adjustment for contrast control
    /// </summary>
    float ApplyWindowing(float value)
    {
        float lower = windowLevel - (windowWidth / 2f);
        float upper = windowLevel + (windowWidth / 2f);

        if (value <= lower) return 0f;
        if (value >= upper) return 1f;

        return (value - lower) / windowWidth;
    }

    /// <summary>
    /// Convert world position to normalized volume position
    /// </summary>
    public Vector3 WorldToNormalizedVolume(Vector3 worldPos)
    {
        // Convert to local space
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        // Normalize to [0, 1] (local space is [-0.5, 0.5])
        return new Vector3(
            localPos.x + 0.5f,
            localPos.y + 0.5f,
            localPos.z + 0.5f
        );
    }

    /// <summary>
    /// Public method to update planes by normalized position
    /// </summary>
    public void UpdateAxialPlane(float normalizedZ) => UpdatePlanePosition(axialData, normalizedZ);
    public void UpdateSagittalPlane(float normalizedX) => UpdatePlanePosition(sagittalData, normalizedX);
    public void UpdateCoronalPlane(float normalizedY) => UpdatePlanePosition(coronalData, normalizedY);

    /// <summary>
    /// Update window settings and refresh all slices
    /// </summary>
    public void UpdateWindowSettings(float level, float width)
    {
        windowLevel = Mathf.Clamp01(level);
        windowWidth = Mathf.Clamp(width, 0.1f, 2f);

        if (volumeLoader.IsLoaded)
        {
            GenerateSliceTexture(axialData);
            GenerateSliceTexture(sagittalData);
            GenerateSliceTexture(coronalData);
        }
    }
}
