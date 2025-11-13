using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrganFocusManager : MonoBehaviour
{
    [Header("Heart Parent")]
    public Transform heartParent;
    private Collider heartCollider;

    [Header("Surface Organs")]
    public List<Transform> organs = new List<Transform>();

    [Header("Camera Settings")]
    public Transform cam; // Assign your Main Camera
    public float focusDistance = 1.2f;
    public float safetyMargin = 0.15f;
    public float moveSpeed = 3f;
    public float rotateSpeed = 5f;

    [Header("Input")]
    public KeyCode nextKey = KeyCode.RightArrow;
    public KeyCode prevKey = KeyCode.LeftArrow;
    public KeyCode exitFocusKey = KeyCode.Escape;

    private int currentIndex = -1;
    private Transform currentTarget;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private bool isFocusing = false;
    private bool isTransitioning = false;

    void Start()
    {
        if (heartParent != null)
        {
            heartCollider = heartParent.GetComponent<Collider>();
            if (heartCollider == null)
                Debug.LogError("Heart parent needs a collider! Please add a MeshCollider.");
        }

        if (cam == null)
            cam = Camera.main.transform;

        originalCamPos = cam.position;
        originalCamRot = cam.rotation;

        // Load the saved camera state and organ focus
        LoadFocusState();
    }

    void Update()
    {
        HandleInput();

        if (isFocusing && currentTarget && !isTransitioning)
            MaintainFocus();
    }

    void HandleInput()
    {
        // Mouse click to focus
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.IsChildOf(heartParent) && organs.Contains(hit.transform))
                    SetFocus(hit.transform);
            }
        }

        // Next organ
        if (Input.GetKeyDown(nextKey))
            CycleOrgan(1);

        // Previous organ
        if (Input.GetKeyDown(prevKey))
            CycleOrgan(-1);

        // Exit focus
        if (Input.GetKeyDown(exitFocusKey))
            ExitFocus();
    }

    void CycleOrgan(int direction)
    {
        if (organs.Count == 0) return;

        if (currentIndex == -1)
            currentIndex = 0;
        else
            currentIndex = (currentIndex + direction + organs.Count) % organs.Count;

        SetFocus(organs[currentIndex]);
    }

    public void SetFocus(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        currentIndex = organs.IndexOf(target);
        isFocusing = true;

        StopAllCoroutines();
        StartCoroutine(TransitionToOrgan());

        SaveFocusState();
    }

    void ExitFocus()
    {
        isFocusing = false;
        currentTarget = null;
        currentIndex = -1;

        StopAllCoroutines();
        StartCoroutine(ReturnToOriginal());

        PlayerPrefs.DeleteKey("LastFocusedOrgan");
        PlayerPrefs.DeleteKey("CameraPosX");
        PlayerPrefs.DeleteKey("CameraPosY");
        PlayerPrefs.DeleteKey("CameraPosZ");
        PlayerPrefs.DeleteKey("CameraRotX");
        PlayerPrefs.DeleteKey("CameraRotY");
        PlayerPrefs.DeleteKey("CameraRotZ");
        PlayerPrefs.DeleteKey("CameraRotW");
        PlayerPrefs.Save();
    }

    IEnumerator ReturnToOriginal()
    {
        isTransitioning = true;
        float t = 0;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            cam.position = Vector3.Lerp(startPos, originalCamPos, t);
            cam.rotation = Quaternion.Slerp(startRot, originalCamRot, t);
            yield return null;
        }

        cam.position = originalCamPos;
        cam.rotation = originalCamRot;
        isTransitioning = false;
    }

    IEnumerator TransitionToOrgan()
    {
        if (!currentTarget || heartCollider == null) yield break;

        isTransitioning = true;
        Vector3 targetPosition = CalculateSafeCameraPosition();
        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - targetPosition);

        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            cam.position = Vector3.Lerp(startPos, targetPosition, t);
            cam.rotation = Quaternion.Slerp(startRot, targetRotation, t);
            yield return null;
        }

        cam.position = targetPosition;
        cam.rotation = targetRotation;
        isTransitioning = false;

        SaveFocusState(); // Save after completing focus movement
    }

    void MaintainFocus()
    {
        if (currentTarget)
        {
            Quaternion targetRot = Quaternion.LookRotation(currentTarget.position - cam.position);
            cam.rotation = Quaternion.Slerp(cam.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }
    }

    Vector3 CalculateSafeCameraPosition()
    {
        Vector3 organPos = currentTarget.position;
        Vector3 directionFromHeart = (organPos - heartParent.position).normalized;
        Vector3 desiredPos = organPos + directionFromHeart * focusDistance;

        Vector3 closestPointOnHeart = heartCollider.ClosestPoint(desiredPos);
        float distanceToSurface = Vector3.Distance(desiredPos, closestPointOnHeart);

        if (distanceToSurface < 0.01f)
        {
            Vector3 outwardDirection = (desiredPos - closestPointOnHeart).normalized;
            if (outwardDirection.magnitude < 0.01f)
                outwardDirection = (organPos - heartParent.position).normalized;

            desiredPos = closestPointOnHeart + outwardDirection * (focusDistance + safetyMargin);
        }

        Vector3 finalCheck = heartCollider.ClosestPoint(desiredPos);
        float finalDistance = Vector3.Distance(desiredPos, finalCheck);
        if (finalDistance < safetyMargin)
        {
            Vector3 pushOut = (desiredPos - finalCheck).normalized;
            desiredPos = finalCheck + pushOut * safetyMargin;
        }

        return desiredPos;
    }

    // --- SAVE / LOAD STATE ---
    void SaveFocusState()
    {
        // Save organ
        if (currentTarget != null)
            PlayerPrefs.SetString("LastFocusedOrgan", currentTarget.name);
        else
            PlayerPrefs.DeleteKey("LastFocusedOrgan");

        // Save camera transform
        PlayerPrefs.SetFloat("CameraPosX", cam.position.x);
        PlayerPrefs.SetFloat("CameraPosY", cam.position.y);
        PlayerPrefs.SetFloat("CameraPosZ", cam.position.z);

        PlayerPrefs.SetFloat("CameraRotX", cam.rotation.x);
        PlayerPrefs.SetFloat("CameraRotY", cam.rotation.y);
        PlayerPrefs.SetFloat("CameraRotZ", cam.rotation.z);
        PlayerPrefs.SetFloat("CameraRotW", cam.rotation.w);

        PlayerPrefs.Save();
    }

    void LoadFocusState()
    {
        // Load camera transform
        if (PlayerPrefs.HasKey("CameraPosX"))
        {
            Vector3 savedPos = new Vector3(
                PlayerPrefs.GetFloat("CameraPosX"),
                PlayerPrefs.GetFloat("CameraPosY"),
                PlayerPrefs.GetFloat("CameraPosZ")
            );

            Quaternion savedRot = new Quaternion(
                PlayerPrefs.GetFloat("CameraRotX"),
                PlayerPrefs.GetFloat("CameraRotY"),
                PlayerPrefs.GetFloat("CameraRotZ"),
                PlayerPrefs.GetFloat("CameraRotW")
            );

            cam.position = savedPos;
            cam.rotation = savedRot;
        }

        // Load focused organ
        string lastOrganName = PlayerPrefs.GetString("LastFocusedOrgan", "");
        if (!string.IsNullOrEmpty(lastOrganName))
        {
            Transform found = organs.Find(o => o.name == lastOrganName);
            if (found != null)
                SetFocus(found);
        }
    }

    public Transform GetCurrentOrgan()
    {
        return currentTarget;
    }

    void OnDrawGizmos()
    {
        if (isFocusing && currentTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cam.position, currentTarget.position);
        }
    }
}
