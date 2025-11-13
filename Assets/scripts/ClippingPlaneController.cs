using UnityEngine;

/// <summary>
/// Controls interactive movement of a clipping plane through the volume
/// </summary>
public class ClippingPlaneController : MonoBehaviour
{
    [Header("References")]
    public VolumeVisualizer visualizer;

    [Header("Interaction Settings")]
    public bool allowMouseDrag = true;
    public bool allowKeyboardControl = true;
    public float keyboardMoveSpeed = 0.01f;
    public KeyCode moveForwardKey = KeyCode.UpArrow;
    public KeyCode moveBackwardKey = KeyCode.DownArrow;

    [Header("Movement Constraints")]
    [Range(0f, 1f)]
    public float minNormalizedPosition = 0f;
    [Range(0f, 1f)]
    public float maxNormalizedPosition = 1f;

    // Internal state
    private bool isDragging = false;
    private Vector3 dragStartMousePos;
    private Vector3 dragStartPlanePos;
    private Camera mainCamera;
    private VolumeVisualizer.SliceAxis sliceAxis;

    void Start()
    {
        mainCamera = Camera.main;

        // Determine which axis this plane moves along based on its orientation
        DetermineSliceAxis();
    }

    /// <summary>
    /// Determine which axis this plane represents based on its local rotation
    /// </summary>
    void DetermineSliceAxis()
    {
        // Check plane's forward direction in parent's local space
        Vector3 localForward = transform.parent.InverseTransformDirection(transform.forward);

        // Find which axis has the largest component
        float absX = Mathf.Abs(localForward.x);
        float absY = Mathf.Abs(localForward.y);
        float absZ = Mathf.Abs(localForward.z);

        if (absX > absY && absX > absZ)
            sliceAxis = VolumeVisualizer.SliceAxis.X;
        else if (absY > absX && absY > absZ)
            sliceAxis = VolumeVisualizer.SliceAxis.Y;
        else
            sliceAxis = VolumeVisualizer.SliceAxis.Z;

        Debug.Log($"{gameObject.name} controls axis: {sliceAxis}");
    }

    void Update()
    {
        if (visualizer == null) return;

        // Keyboard control
        if (allowKeyboardControl)
        {
            HandleKeyboardInput();
        }
    }

    /// <summary>
    /// Handle keyboard-based plane movement
    /// </summary>
    void HandleKeyboardInput()
    {
        float movement = 0f;

        if (Input.GetKey(moveForwardKey))
            movement = keyboardMoveSpeed * Time.deltaTime;
        else if (Input.GetKey(moveBackwardKey))
            movement = -keyboardMoveSpeed * Time.deltaTime;

        if (movement != 0f)
        {
            // Get current normalized position
            float currentPos = GetNormalizedPosition();
            float newPos = Mathf.Clamp(currentPos + movement, minNormalizedPosition, maxNormalizedPosition);

            // Update plane position
            SetNormalizedPosition(newPos);
        }
    }

    void OnMouseDown()
    {
        if (!allowMouseDrag || mainCamera == null) return;

        isDragging = true;
        dragStartMousePos = Input.mousePosition;
        dragStartPlanePos = transform.localPosition;
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        // Calculate mouse delta
        Vector3 mouseDelta = Input.mousePosition - dragStartMousePos;

        // Project mouse movement onto the plane's normal direction
        Vector3 movementDir = GetMovementDirection();

        // Convert screen space movement to world space
        // Use a sensitivity factor to make dragging feel natural
        float sensitivity = 0.001f;
        float screenDelta = GetScreenSpaceDelta(mouseDelta, movementDir);

        // Calculate new position
        Vector3 newLocalPos = dragStartPlanePos;
        switch (sliceAxis)
        {
            case VolumeVisualizer.SliceAxis.X:
                newLocalPos.x = dragStartPlanePos.x + screenDelta * sensitivity;
                newLocalPos.x = Mathf.Clamp(newLocalPos.x, -0.5f, 0.5f);
                break;
            case VolumeVisualizer.SliceAxis.Y:
                newLocalPos.y = dragStartPlanePos.y + screenDelta * sensitivity;
                newLocalPos.y = Mathf.Clamp(newLocalPos.y, -0.5f, 0.5f);
                break;
            case VolumeVisualizer.SliceAxis.Z:
                newLocalPos.z = dragStartPlanePos.z + screenDelta * sensitivity;
                newLocalPos.z = Mathf.Clamp(newLocalPos.z, -0.5f, 0.5f);
                break;
        }

        transform.localPosition = newLocalPos;

        // Update visualization
        float normalizedPos = GetNormalizedPosition();
        SetNormalizedPosition(normalizedPos);
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    /// <summary>
    /// Get the direction this plane should move in world space
    /// </summary>
    Vector3 GetMovementDirection()
    {
        // The plane moves along its normal (forward) direction
        return transform.forward;
    }

    /// <summary>
    /// Calculate screen space delta projected onto movement direction
    /// </summary>
    float GetScreenSpaceDelta(Vector3 mouseDelta, Vector3 worldDir)
    {
        // Convert world direction to screen space
        Vector3 screenDir = mainCamera.WorldToScreenPoint(transform.position + worldDir) -
                           mainCamera.WorldToScreenPoint(transform.position);

        screenDir.z = 0; // We only care about 2D screen movement

        if (screenDir.magnitude < 0.001f) return 0f;

        screenDir.Normalize();

        // Project mouse delta onto screen direction
        float projection = Vector3.Dot(new Vector3(mouseDelta.x, mouseDelta.y, 0), screenDir);

        return projection;
    }

    /// <summary>
    /// Get the current normalized position [0, 1] of this plane
    /// </summary>
    float GetNormalizedPosition()
    {
        Vector3 localPos = transform.localPosition;

        switch (sliceAxis)
        {
            case VolumeVisualizer.SliceAxis.X:
                return localPos.x + 0.5f;
            case VolumeVisualizer.SliceAxis.Y:
                return localPos.y + 0.5f;
            case VolumeVisualizer.SliceAxis.Z:
                return localPos.z + 0.5f;
            default:
                return 0.5f;
        }
    }

    /// <summary>
    /// Set the normalized position [0, 1] and update the visualizer
    /// </summary>
    public void SetNormalizedPosition(float normalizedPos)
    {
        normalizedPos = Mathf.Clamp(normalizedPos, minNormalizedPosition, maxNormalizedPosition);

        // Update visualizer based on which plane this is
        switch (sliceAxis)
        {
            case VolumeVisualizer.SliceAxis.X:
                visualizer.UpdateSagittalPlane(normalizedPos);
                break;
            case VolumeVisualizer.SliceAxis.Y:
                visualizer.UpdateCoronalPlane(normalizedPos);
                break;
            case VolumeVisualizer.SliceAxis.Z:
                visualizer.UpdateAxialPlane(normalizedPos);
                break;
        }
    }

    /// <summary>
    /// Enable or disable plane visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        GetComponent<MeshRenderer>().enabled = visible;
    }

    /// <summary>
    /// Jump to a specific slice index
    /// </summary>
    public void JumpToSlice(int sliceIndex, int maxSlices)
    {
        float normalizedPos = (float)sliceIndex / Mathf.Max(1, maxSlices - 1);
        SetNormalizedPosition(normalizedPos);
    }

    // Gizmo visualization for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        // Draw normal direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
    }
}