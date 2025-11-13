using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    public Transform target;              // Object to orbit around
    public float xSpeed = 250f;           // Horizontal orbit speed
    public float ySpeed = 250f;           // Vertical orbit speed
    public float yMinLimit = -80f;        // Vertical down limit
    public float yMaxLimit = 80f;         // Vertical up limit
    public float zoomSpeed = 50f;         // Scroll zoom speed
    public float minDistance = 1f;        // Closest zoom
    public float maxDistance = 10000f;    // Farthest zoom

    private float distance;               // Distance to target
    private float xAngle;                 // Azimuth angle
    private float yAngle;                 // Elevation angle
    private Vector3 offset;               // Starting offset from target

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("OrbitCamera: No target assigned!");
            enabled = false;
            return;
        }

        // Store current relative offset and distance
        offset = transform.position - target.position;
        distance = offset.magnitude;

        // Keep the current camera rotation angles (no auto-centering)
        Vector3 angles = transform.eulerAngles;
        xAngle = angles.y;
        yAngle = angles.x;
    }

    void LateUpdate()
    {
        if (!target) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Rotate with left mouse button
        if (Input.GetMouseButton(0))
        {
            xAngle += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            yAngle -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
            yAngle = Mathf.Clamp(yAngle, yMinLimit, yMaxLimit);
        }

        // Zoom with scroll wheel
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Compute new rotation and position relative to target
        Quaternion rotation = Quaternion.Euler(yAngle, xAngle, 0);
        Vector3 newPos = target.position + rotation * Vector3.back * distance;

        // Apply to camera
        transform.rotation = rotation;
        transform.position = newPos;
    }
}
