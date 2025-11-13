using UnityEngine;
using UnityEngine.UI;

public class PlanePositionController : MonoBehaviour
{
    [Header("Planes")]
    public Transform xPlane;
    public Transform yPlane;
    public Transform zPlane;

    [Header("Sliders")]
    public Slider xSlider;
    public Slider ySlider;
    public Slider zSlider;

    private void Start()
    {
        // Set up listeners for slider value changes
        xSlider.onValueChanged.AddListener(UpdateXPlane);
        ySlider.onValueChanged.AddListener(UpdateYPlane);
        zSlider.onValueChanged.AddListener(UpdateZPlane);
    }

    private void UpdateXPlane(float value)
    {
        if (xPlane != null)
        {
            Vector3 pos = xPlane.position;
            pos.x = value;
            xPlane.position = pos;
        }
    }

    private void UpdateYPlane(float value)
    {
        if (yPlane != null)
        {
            Vector3 pos = yPlane.position;
            pos.y = value;
            yPlane.position = pos;
        }
    }

    private void UpdateZPlane(float value)
    {
        if (zPlane != null)
        {
            Vector3 pos = zPlane.position;
            pos.z = value;
            zPlane.position = pos;
        }
    }
}
