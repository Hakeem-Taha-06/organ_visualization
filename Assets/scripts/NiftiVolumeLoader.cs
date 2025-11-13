using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Nifti.NET;

/// <summary>
/// Loads NIfTI files (.nii or .nii.gz) and provides access to voxel data
/// </summary>
public class NiftiVolumeLoader : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Path to the .nii or .nii.gz file")]
    public string niftiFilePath;

    [Header("Data Normalization")]
    [Tooltip("Use percentile-based normalization for better contrast")]
    public bool usePercentileNormalization = true;
    [Range(0f, 10f)]
    public float lowerPercentile = 2f;
    [Range(90f, 100f)]
    public float upperPercentile = 98f;

    // Volume data
    private float[,,] volumeData;
    private Vector3Int dimensions;
    private Vector3 voxelSpacing;
    private float minIntensity;
    private float maxIntensity;

    // Public accessors
    public float[,,] VolumeData => volumeData;
    public Vector3Int Dimensions => dimensions;
    public Vector3 VoxelSpacing => voxelSpacing;
    public float MinIntensity => minIntensity;
    public float MaxIntensity => maxIntensity;
    public bool IsLoaded { get; private set; }

    void Start()
    {
        if (!string.IsNullOrEmpty(niftiFilePath))
        {
            LoadNiftiFile(niftiFilePath);
        }
    }

    /// <summary>
    /// Load a NIfTI file from the specified path
    /// </summary>
    public bool LoadNiftiFile(string filePath)
    {
        try
        {
            Debug.Log($"Loading NIfTI file: {filePath}");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return false;
            }

            // Load the NIfTI file
            var nifti = NiftiFile.Read(filePath);

            // Determine dimensions robustly. Different NIfTI readers expose dims differently:
            // - Some return dim[0] = number of dimensions, dim[1..n] = sizes
            // - Some return dims array starting at X (0)
            int width = 1, height = 1, depth = 1;
            try
            {
                var dims = nifti.Dimensions;
                if (dims != null && dims.Length > 0)
                {
                    // Common case: dim[0] contains number of dimensions, subsequent entries are sizes
                    if (dims.Length >= 4 && dims[0] == 3)
                    {
                        width = Math.Max(1, dims[1]);
                        height = Math.Max(1, dims[2]);
                        depth = Math.Max(1, dims[3]);
                    }
                    else
                    {
                        // Fallback: take first three non-zero entries (useful for readers that don't store dim[0])
                        var nonZero = new List<int>();
                        foreach (var d in dims)
                        {
                            if (d > 0) nonZero.Add(d);
                        }

                        if (nonZero.Count >= 3)
                        {
                            width = Math.Max(1, nonZero[0]);
                            height = Math.Max(1, nonZero[1]);
                            depth = Math.Max(1, nonZero[2]);
                        }
                        else
                        {
                            // As last resort, use up to first three elements (if present)
                            width = dims.Length > 0 ? Math.Max(1, dims[0]) : 1;
                            height = dims.Length > 1 ? Math.Max(1, dims[1]) : 1;
                            depth = dims.Length > 2 ? Math.Max(1, dims[2]) : 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to read dimensions from NIfTI header: {ex.Message}. Defaulting to 1x1x1.");
                width = height = depth = 1;
            }

            dimensions = new Vector3Int(width, height, depth);

            // Get voxel spacing (pixdim) safely
            float px = 1f, py = 1f, pz = 1f;
            try
            {
                var pix = nifti.Header?.pixdim;
                if (pix != null && pix.Length > 3)
                {
                    px = pix[1] > 0 ? pix[1] : 1f;
                    py = pix[2] > 0 ? pix[2] : 1f;
                    pz = pix[3] > 0 ? pix[3] : 1f;
                }
            }
            catch
            {
                // keep defaults
            }

            voxelSpacing = new Vector3(px, py, pz);

            Debug.Log($"Dimensions: {dimensions}, Voxel Spacing: {voxelSpacing}");

            // Extract voxel data
            volumeData = new float[width, height, depth];
            ExtractVoxelData(nifti, width, height, depth);

            // Normalize the data
            NormalizeVolumeData();

            IsLoaded = true;
            Debug.Log($"Successfully loaded NIfTI file. Intensity range: [{minIntensity}, {maxIntensity}]");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading NIfTI file: {e.Message}\n{e.StackTrace}");
            IsLoaded = false;
            return false;
        }
    }

    /// <summary>
    /// Extract voxel data from NIfTI file, handling different data types
    /// </summary>
    private void ExtractVoxelData(Nifti.NET.Nifti nifti, int width, int height, int depth)
    {
        var dataType = nifti.Header.datatype;

        // Get raw data as appropriate type
        if (dataType == NiftiHeader.DT_UINT8)
        {
            var data = nifti.AsType<byte>()?.Data;
            CopyDataFrom1DArray<byte>(data, width, height, depth);
        }
        else if (dataType == NiftiHeader.DT_INT16)
        {
            var data = nifti.AsType<short>()?.Data;
            CopyDataFrom1DArray<short>(data, width, height, depth);
        }
        else if (dataType == NiftiHeader.DT_INT32)
        {
            var data = nifti.AsType<int>()?.Data;
            CopyDataFrom1DArray<int>(data, width, height, depth);
        }
        else if (dataType == NiftiHeader.DT_FLOAT32)
        {
            var data = nifti.AsType<float>()?.Data;
            CopyDataFrom1DArray<float>(data, width, height, depth);
        }
        else if (dataType == NiftiHeader.DT_FLOAT64)
        {
            var data = nifti.AsType<double>()?.Data;
            CopyDataFrom1DArray<double>(data, width, height, depth);
        }
        else
        {
            Debug.LogWarning($"Unsupported data type: {dataType}. Attempting float conversion.");
            var data = nifti.AsType<float>()?.Data;
            CopyDataFrom1DArray<float>(data, width, height, depth);
        }
    }

    /// <summary>
    /// Copy data from 1D array to 3D array with Z-flip for Unity coordinate system
    /// Robust against mismatched data lengths.
    /// </summary>
    private void CopyData<T>(T[,,] sourceData, int width, int height, int depth) where T : struct, IConvertible
    {
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Flip Z axis to convert from NIfTI (RAS/LPS) to Unity coordinates
                    int zFlipped = depth - 1 - z;
                    float value = Convert.ToSingle(sourceData[x, y, z]);

                    // Handle NaN and Inf values
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        value = 0f;
                    }

                    volumeData[x, y, zFlipped] = value;
                }
            }
        }
    }

    /// <summary>
    /// Normalize volume data to [0, 1] range
    /// </summary>
    private void NormalizeVolumeData()
    {
        // First pass: find min and max
        minIntensity = float.MaxValue;
        maxIntensity = float.MinValue;

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    float val = volumeData[x, y, z];
                    if (val < minIntensity) minIntensity = val;
                    if (val > maxIntensity) maxIntensity = val;
                }
            }
        }

        // Use percentile-based normalization if enabled
        if (usePercentileNormalization)
        {
            CalculatePercentileRange();
        }

        float range = maxIntensity - minIntensity;
        if (range < 0.0001f) range = 1f; // Avoid division by zero

        // Second pass: normalize to [0, 1]
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    float normalized = (volumeData[x, y, z] - minIntensity) / range;
                    volumeData[x, y, z] = Mathf.Clamp01(normalized);
                }
            }
        }
    }

    /// <summary>
    /// Calculate intensity range based on percentiles for better contrast
    /// </summary>
    private void CalculatePercentileRange()
    {
        // Collect all intensity values
        int totalVoxels = dimensions.x * dimensions.y * dimensions.z;
        float[] allValues = new float[totalVoxels];
        int idx = 0;

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    allValues[idx++] = volumeData[x, y, z];
                }
            }
        }

        // Sort values
        Array.Sort(allValues);

        // Calculate percentile indices
        int lowerIdx = Mathf.FloorToInt(totalVoxels * lowerPercentile / 100f);
        int upperIdx = Mathf.FloorToInt(totalVoxels * upperPercentile / 100f);

        lowerIdx = Mathf.Clamp(lowerIdx, 0, totalVoxels - 1);
        upperIdx = Mathf.Clamp(upperIdx, 0, totalVoxels - 1);

        minIntensity = allValues[lowerIdx];
        maxIntensity = allValues[upperIdx];

        Debug.Log($"Percentile range [{lowerPercentile}%, {upperPercentile}%]: [{minIntensity}, {maxIntensity}]");
    }

    /// <summary>
    /// Get the world-space bounds of the volume
    /// </summary>
    public Bounds GetVolumeBounds()
    {
        Vector3 size = new Vector3(
            dimensions.x * voxelSpacing.x,
            dimensions.y * voxelSpacing.y,
            dimensions.z * voxelSpacing.z
        );

        return new Bounds(transform.position, size);
    }

    private void CopyDataFrom1DArray<T>(T[] sourceData, int width, int height, int depth) where T : struct, IConvertible
    {
        if (sourceData == null)
        {
            Debug.LogWarning("Source data is null. Volume will remain zeroed.");
            return;
        }

        int expected = width * height * depth;
        if (sourceData.Length < expected)
        {
            Debug.LogWarning($"Source data length ({sourceData.Length}) is smaller than expected ({expected}). Missing voxels will be filled with 0.");
        }
        else if (sourceData.Length > expected)
        {
            Debug.LogWarning($"Source data length ({sourceData.Length}) is larger than expected ({expected}). Extra values will be ignored.");
        }

        int idx = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int zFlipped = depth - 1 - z;
                    float value = 0f;
                    if (idx < sourceData.Length)
                    {
                        try
                        {
                            value = Convert.ToSingle(sourceData[idx]);
                        }
                        catch (Exception)
                        {
                            value = 0f;
                        }
                    }
                    idx++;

                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        value = 0f;
                    }
                    volumeData[x, y, zFlipped] = value;
                }
            }
        }
    }
}