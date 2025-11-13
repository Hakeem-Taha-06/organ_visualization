using UnityEngine;
using UnityEngine.UI;

public class ClippingUIController : MonoBehaviour
{
    [Header("UI References")]
    public Toggle clippingToggle;        // The toggle that controls visibility
    public GameObject slidersParent;     // Parent containing ONLY the sliders

    private Slider[] sliders;

    void Start()
    {
        if (clippingToggle == null || slidersParent == null)
        {
            Debug.LogError("❌ Missing references in ClippingUIController!");
            return;
        }

        // Get all sliders under the parent
        sliders = slidersParent.GetComponentsInChildren<Slider>(true);

        // Listen to toggle changes
        clippingToggle.onValueChanged.AddListener(OnToggleChanged);

        // Initialize once at start
        OnToggleChanged(clippingToggle.isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        foreach (Slider s in sliders)
        {
            s.interactable = isOn;  // Grey out or enable
        }

        Debug.Log($"🎚 Clipping sliders {(isOn ? "enabled" : "disabled")} (visual only)");
    }
}
