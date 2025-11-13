using UnityEngine;
using Nifti.NET;
using System;

[ExecuteAlways]
public class NiftiLoader : MonoBehaviour
{
    public enum Orientation
    {
        Axial,      // XY plane (Z slices)
        Sagittal,   // YZ plane (X slices)
        Coronal     // XZ plane (Y slices)
    }

    [Header("NIFTI File")]
    public string niftiFilePath;

    // Volume dimensions (in NIfTI space)
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }

    // NIfTI object from Nifti.NET
    private dynamic niftiObject;

    // Min/max values for normalization
    private float minValue;
    private float maxValue;

    // Flag to indicate if data is loaded
    public bool IsLoaded { get; private set; }

    void Start()
    {
        if (!string.IsNullOrEmpty(niftiFilePath))
        {
            LoadNiftiFile(niftiFilePath);
        }
    }

    public bool LoadNiftiFile(string filePath)
    {
        try
        {
            Debug.Log($"Loading NIFTI file: {filePath}");

            // Read the NIfTI file using Nifti.NET
            niftiObject = NiftiFile.Read(filePath);

            if (niftiObject == null)
            {
                Debug.LogError("Failed to load NIFTI file");
                return false;
            }

            // Get dimensions from header
            Width = niftiObject.Header.dim[1];
            Height = niftiObject.Header.dim[2];
            Depth = niftiObject.Header.dim[3];

            Debug.Log($"NIFTI dimensions: {Width}x{Height}x{Depth}");
            Debug.Log($"Voxel size: {niftiObject.Header.pixdim[1]}x{niftiObject.Header.pixdim[2]}x{niftiObject.Header.pixdim[3]}mm");
            Debug.Log($"Data type: {niftiObject.Header.datatype}");

            // Calculate min/max for normalization
            CalculateMinMax();

            IsLoaded = true;
            Debug.Log($"NIFTI loaded successfully. Value range: {minValue} to {maxValue}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading NIFTI file: {e.Message}\n{e.StackTrace}");
            IsLoaded = false;
            return false;
        }
    }

    private void CalculateMinMax()
    {
        if (niftiObject == null || niftiObject.Data == null) return;

        minValue = float.MaxValue;
        maxValue = float.MinValue;

        Array dataArray = niftiObject.Data;

        foreach (var value in dataArray)
        {
            float floatValue = Convert.ToSingle(value);
            if (floatValue < minValue) minValue = floatValue;
            if (floatValue > maxValue) maxValue = floatValue;
        }
    }

    public Texture2D GetSliceTexture(Orientation orientation, int sliceIndex)
    {
        if (!IsLoaded || niftiObject == null)
        {
            Debug.LogError("No volume data loaded");
            return null;
        }

        Texture2D texture = null;

        switch (orientation)
        {
            case Orientation.Axial:
                texture = GetAxialSlice(sliceIndex);
                break;
            case Orientation.Sagittal:
                texture = GetSagittalSlice(sliceIndex);
                break;
            case Orientation.Coronal:
                texture = GetCoronalSlice(sliceIndex);
                break;
        }

        return texture;
    }

    private float GetVoxelValue(int x, int y, int z)
    {
        try
        {
            dynamic value = niftiObject[x, y, z];
            return Convert.ToSingle(value);
        }
        catch
        {
            return 0f;
        }
    }

    // Axial slice: Looking down Z-axis, shows XY plane
    private Texture2D GetAxialSlice(int z)
    {
        z = Mathf.Clamp(z, 0, Depth - 1);

        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float value = GetVoxelValue(x, y, z);
                float normalized = Mathf.InverseLerp(minValue, maxValue, value);
                colors[x + y * Width] = new Color(normalized, normalized, normalized, 1f);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    // Sagittal slice: Looking along X-axis, shows YZ plane
    // Width dimension should be Z (depth), height dimension should be Y
    private Texture2D GetSagittalSlice(int x)
    {
        x = Mathf.Clamp(x, 0, Width - 1);

        // Create texture with correct dimensions: Z (depth) as width, Y (height) as height
        Texture2D texture = new Texture2D(Depth, Height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[Depth * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Depth; z++)
            {
                float value = GetVoxelValue(x, y, z);
                float normalized = Mathf.InverseLerp(minValue, maxValue, value);
                // Z is horizontal (texture width), Y is vertical (texture height)
                colors[z + y * Depth] = new Color(normalized, normalized, normalized, 1f);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    // Coronal slice: Looking along Y-axis, shows XZ plane
    // Width dimension should be X, height dimension should be Z (depth)
    private Texture2D GetCoronalSlice(int y)
    {
        y = Mathf.Clamp(y, 0, Height - 1);

        // Create texture with correct dimensions: X as width, Z (depth) as height
        Texture2D texture = new Texture2D(Width, Depth, TextureFormat.RGBA32, false);
        Color[] colors = new Color[Width * Depth];

        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                float value = GetVoxelValue(x, y, z);
                float normalized = Mathf.InverseLerp(minValue, maxValue, value);
                // X is horizontal (texture width), Z is vertical (texture height)
                colors[x + z * Width] = new Color(normalized, normalized, normalized, 1f);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    public int GetSliceCount(Orientation orientation)
    {
        if (!IsLoaded) return 0;

        switch (orientation)
        {
            case Orientation.Axial: return Depth;
            case Orientation.Sagittal: return Width;
            case Orientation.Coronal: return Height;
            default: return 0;
        }
    }

    // Returns voxel size in Unity space (with Y/Z swapped)
    public Vector3 GetVoxelSize()
    {
        if (!IsLoaded || niftiObject == null)
            return Vector3.one;

        // Swap Y and Z to convert from NIfTI space to Unity space
        return new Vector3(
            niftiObject.Header.pixdim[1],  // X stays the same
            niftiObject.Header.pixdim[3],  // Unity Y = NIfTI Z
            niftiObject.Header.pixdim[2]   // Unity Z = NIfTI Y
        );
    }

    // Returns voxel size in original NIfTI space (no swapping)
    public Vector3 GetVoxelSizeNifti()
    {
        if (!IsLoaded || niftiObject == null)
            return Vector3.one;

        return new Vector3(
            niftiObject.Header.pixdim[1],
            niftiObject.Header.pixdim[2],
            niftiObject.Header.pixdim[3]
        );
    }

    public string GetDescription()
    {
        if (!IsLoaded || niftiObject == null)
            return "No data loaded";

        return niftiObject.Header.descrip ?? "No description";
    }

    // Helper method to get actual texture dimensions for each orientation
    public Vector2Int GetTextureDimensions(Orientation orientation)
    {
        if (!IsLoaded) return Vector2Int.zero;

        switch (orientation)
        {
            case Orientation.Axial:
                return new Vector2Int(Width, Height);
            case Orientation.Sagittal:
                return new Vector2Int(Depth, Height);
            case Orientation.Coronal:
                return new Vector2Int(Width, Depth);
            default:
                return Vector2Int.zero;
        }
    }
}