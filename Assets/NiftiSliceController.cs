using UnityEngine;
using UnityEngine.UI;

public class NiftiSliceController : MonoBehaviour
{
    [Header("Plane References")]
    public NiftiPlaneAssigner axialPlane;
    public NiftiPlaneAssigner sagittalPlane;
    public NiftiPlaneAssigner coronalPlane;

    [Header("Slider References")]
    public Slider axialSlider;
    public Slider sagittalSlider;
    public Slider coronalSlider;

    [Header("Optional Text Labels")]
    public Text axialLabel;
    public Text sagittalLabel;
    public Text coronalLabel;

    private bool isInitialized = false;

    void Start()
    {
        // Wait a frame to ensure NiftiLoader is ready
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        if (axialPlane != null && axialPlane.niftiLoader != null && axialPlane.niftiLoader.IsLoaded)
        {
            SetupSlider(axialSlider, axialPlane, axialLabel, "Axial");
        }

        if (sagittalPlane != null && sagittalPlane.niftiLoader != null && sagittalPlane.niftiLoader.IsLoaded)
        {
            SetupSlider(sagittalSlider, sagittalPlane, sagittalLabel, "Sagittal");
        }

        if (coronalPlane != null && coronalPlane.niftiLoader != null && coronalPlane.niftiLoader.IsLoaded)
        {
            SetupSlider(coronalSlider, coronalPlane, coronalLabel, "Coronal");
        }

        isInitialized = true;
    }

    private void SetupSlider(Slider slider, NiftiPlaneAssigner plane, Text label, string planeName)
    {
        if (slider == null || plane == null || plane.niftiLoader == null) return;

        // Get the number of slices for this orientation
        int sliceCount = plane.niftiLoader.GetSliceCount(plane.orientation);

        // Configure slider
        slider.minValue = 0;
        slider.maxValue = sliceCount - 1;
        slider.wholeNumbers = true;
        slider.value = plane.GetCurrentSliceIndex();

        // Add listener to update plane when slider changes
        slider.onValueChanged.AddListener((value) => OnSliderChanged(value, plane, label, planeName));

        // Update initial label
        UpdateLabel(label, planeName, (int)slider.value, sliceCount);

        Debug.Log($"Setup {planeName} slider: 0 to {sliceCount - 1}");
    }

    private void OnSliderChanged(float value, NiftiPlaneAssigner plane, Text label, string planeName)
    {
        if (plane == null || plane.niftiLoader == null) return;

        int sliceIndex = (int)value;
        plane.SetSliceIndex(sliceIndex);

        int sliceCount = plane.niftiLoader.GetSliceCount(plane.orientation);
        UpdateLabel(label, planeName, sliceIndex, sliceCount);
    }

    private void UpdateLabel(Text label, string planeName, int currentSlice, int totalSlices)
    {
        if (label != null)
        {
            label.text = $"{planeName}: {currentSlice + 1} / {totalSlices}";
        }
    }

    // Public methods to set slices programmatically
    public void SetAxialSlice(int sliceIndex)
    {
        if (axialSlider != null)
        {
            axialSlider.value = sliceIndex;
        }
    }

    public void SetSagittalSlice(int sliceIndex)
    {
        if (sagittalSlider != null)
        {
            sagittalSlider.value = sliceIndex;
        }
    }

    public void SetCoronalSlice(int sliceIndex)
    {
        if (coronalSlider != null)
        {
            coronalSlider.value = sliceIndex;
        }
    }

    // Reset all sliders to middle position
    public void ResetToCenter()
    {
        if (axialSlider != null)
        {
            axialSlider.value = axialSlider.maxValue / 2f;
        }
        if (sagittalSlider != null)
        {
            sagittalSlider.value = sagittalSlider.maxValue / 2f;
        }
        if (coronalSlider != null)
        {
            coronalSlider.value = coronalSlider.maxValue / 2f;
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (axialSlider != null)
        {
            axialSlider.onValueChanged.RemoveAllListeners();
        }
        if (sagittalSlider != null)
        {
            sagittalSlider.onValueChanged.RemoveAllListeners();
        }
        if (coronalSlider != null)
        {
            coronalSlider.onValueChanged.RemoveAllListeners();
        }
    }
}