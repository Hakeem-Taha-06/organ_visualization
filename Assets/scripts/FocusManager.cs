using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FocusManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform organRoot;
    public GameObject floatingLabelPrefab;

    [Header("Focus Settings")]
    public float focusTransitionSpeed = 3f;
    public float unfocusedOpacity = 0f; // Set to 0 as requested
    public float focusPadding = 1.3f;
    public float minFocusDistance = 0.00f;
    public float maxFocusDistance = 10.0f;
    public float zoomSpeed = 5f;

    [Header("Camera Controls")]
    public float rotationSpeed = 2.0f;
    public float panSpeed = 0.1f;

    [Header("Outline Settings")]
    public Color outlineColor = Color.yellow;
    public Color hoverColor = Color.cyan;
    public Material outlineMaterialTemplate; // No longer used for color, but still used for transparency shader settings

    private Transform currentHoverPart;
    private Transform currentSelectedPart;
    private GameObject floatingLabel;
    private bool isInFocusMode;
    private bool inTransition;
    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;
    private Coroutine cameraTransition;
    private Coroutine fadeRoutine;
    private Dictionary<Renderer, Material[]> originalMaterials = new();

    // Private variable to store the instanced materials of the
    // highlighted object so we can restore them later.
    private Dictionary<Renderer, Material[]> instancedMaterials = new();

    private float camYaw = 0.0f;
    private float camPitch = 0.0f;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        CacheMaterials();

        Vector3 startAngles = mainCamera.transform.eulerAngles;
        camYaw = startAngles.y;
        camPitch = startAngles.x;
    }

    void CacheMaterials()
    {
        originalMaterials.Clear();
        foreach (Renderer r in organRoot.GetComponentsInChildren<Renderer>())
        {
            if (!originalMaterials.ContainsKey(r))
            {
                // Cache the original shared materials
                originalMaterials[r] = r.sharedMaterials;
            }
        }
    }

    void Update()
    {
        if (inTransition) return;

        HandleZoom(); // Always handle zoom

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            HandleCameraMovement();
        }
        else if (!isInFocusMode)
        {
            HandleHoverAndSelection();
        }

        if (Input.GetKeyDown(KeyCode.F) && currentSelectedPart != null)
        {
            if (isInFocusMode)
                StartCoroutine(RunExitFocusMode());
            else
                StartCoroutine(RunEnterFocusMode());
        }

        if (floatingLabel != null)
            floatingLabel.transform.LookAt(mainCamera.transform.position);
    }

    void HandleCameraMovement()
    {
        if (Input.GetMouseButton(1))
        {
            camYaw += Input.GetAxis("Mouse X") * rotationSpeed;
            camPitch -= Input.GetAxis("Mouse Y") * rotationSpeed; // Invert Y
            camPitch = Mathf.Clamp(camPitch, -89f, 89f);
            mainCamera.transform.eulerAngles = new Vector3(camPitch, camYaw, 0.0f);
        }

        if (Input.GetMouseButton(2))
        {
            float mouseX = -Input.GetAxis("Mouse X") * panSpeed;
            float mouseY = -Input.GetAxis("Mouse Y") * panSpeed;
            Vector3 move = (mainCamera.transform.right * mouseX) + (mainCamera.transform.up * mouseY);
            mainCamera.transform.position += move;
        }
    }

    void HandleHoverAndSelection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Transform newHoverPart = null;
        bool hitValidPart = false;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.IsChildOf(organRoot))
            {
                newHoverPart = hit.transform;
                hitValidPart = true;
            }
        }

        // Handle Click (Selection)
        if (hitValidPart && Input.GetMouseButtonDown(0))
        {
            SelectPart(newHoverPart);
        }

        // Handle Hover Logic
        if (currentHoverPart != newHoverPart)
        {
            // Un-highlight the old part, but ONLY if it's not the selected part.
            if (currentHoverPart != null && currentHoverPart != currentSelectedPart)
            {
                RestoreSinglePart(currentHoverPart);
            }

            // Highlight the new part, but ONLY if it's not the selected part.
            if (newHoverPart != null && newHoverPart != currentSelectedPart)
            {
                HighlightHoverPart(newHoverPart);
            }

            currentHoverPart = newHoverPart;
        }
    }


    void SelectPart(Transform part)
    {
        if (currentSelectedPart == part)
            return;

        // Restore the previously selected part (if any)
        RestoreSinglePart(currentSelectedPart);

        // Set and highlight the new part (yellow)
        currentSelectedPart = part;
        HighlightPart(currentSelectedPart, outlineColor); // Use new generic function
        Debug.Log("Selected part: " + part.name);
    }

    // --- MODIFIED ---
    // This function now just calls the new generic HighlightPart function
    void HighlightSelectedPart(Transform part)
    {
        HighlightPart(part, outlineColor);
    }

    // --- MODIFIED ---
    // This function now just calls the new generic HighlightPart function
    void HighlightHoverPart(Transform part)
    {
        HighlightPart(part, hoverColor);
    }

    // --- *** NEW, BETTER HIGHLIGHT FUNCTION *** ---
    // This function takes the original materials, instances them,
    // and just changes their color. This fixes the "upside down" bug.
    // --- FIXED HIGHLIGHT FUNCTION ---
    // Now tints the material by blending with original color instead of replacing it
    void HighlightPart(Transform part, Color color)
    {
        if (part == null) return;
        Renderer rend = part.GetComponent<Renderer>();
        if (rend == null) return;

        // Store the current materials (which are instances) so we can
        // put them back when we un-hover.
        if (!instancedMaterials.ContainsKey(rend))
        {
            instancedMaterials[rend] = rend.materials;
        }

        // Create new material instances to add highlight
        Material[] newMats = new Material[rend.materials.Length];
        for (int i = 0; i < rend.materials.Length; i++)
        {
            // Create a new instance from the original cached material
            // This prevents "stacking" highlights
            newMats[i] = new Material(originalMaterials[rend][i]);

            // Get the original color from the cached material
            Color originalColor = originalMaterials[rend][i].color;

            // Blend the highlight color with the original color (70% highlight, 30% original)
            // Adjust the blend amount as needed - higher value = more highlight visible
            newMats[i].color = Color.Lerp(originalColor, color, 0.7f);
        }

        // Apply the new highlighted materials
        rend.materials = newMats;
    }


    // --- MODIFIED RestoreSinglePart ---
    void RestoreSinglePart(Transform part)
    {
        if (part == null) return;

        Renderer rend = part.GetComponent<Renderer>();
        // Check if we have an original and we actually instanced it
        if (rend != null && originalMaterials.ContainsKey(rend))
        {
            // Restore the original cached shared materials
            rend.materials = originalMaterials[rend];

            // Since we restored it, remove it from the instanced list
            if (instancedMaterials.ContainsKey(rend))
            {
                instancedMaterials.Remove(rend);
            }
        }
    }

    IEnumerator RunEnterFocusMode()
    {
        inTransition = true;
        yield return StartCoroutine(EnterFocusMode());
        inTransition = false;
    }

    IEnumerator RunExitFocusMode()
    {
        inTransition = true;
        yield return StartCoroutine(ExitFocusMode());
        inTransition = false;
    }

    IEnumerator EnterFocusMode()
    {
        if (currentSelectedPart == null) yield break;
        isInFocusMode = true;
        originalCamPosition = mainCamera.transform.position;
        originalCamRotation = mainCamera.transform.rotation;

        // **FIX: Remove the yellow highlight from the selected part before fading**
        RestoreSinglePart(currentSelectedPart);

        Bounds bounds = CalculateBounds(currentSelectedPart);
        Vector3 center = bounds.center;

        float desiredDistance = ComputeFitDistance(bounds, mainCamera);
        desiredDistance = Mathf.Clamp(desiredDistance * focusPadding, minFocusDistance, maxFocusDistance);
        Vector3 desiredCamPos = center - mainCamera.transform.forward * desiredDistance;

        Vector3 startCamPos = mainCamera.transform.position;
        Quaternion startCamRot = mainCamera.transform.rotation;
        Quaternion desiredRot = Quaternion.LookRotation(center - desiredCamPos);

        if (cameraTransition != null)
            StopCoroutine(cameraTransition);
        cameraTransition = StartCoroutine(SmoothTransitionCamera(startCamPos, desiredCamPos, startCamRot, desiredRot));

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOthers());

        CreateFloatingLabel(center, currentSelectedPart.name);

        while (cameraTransition != null)
            yield return null;
    }

    IEnumerator ExitFocusMode()
    {
        isInFocusMode = false;
        ClearFloatingLabel();

        if (cameraTransition != null)
            StopCoroutine(cameraTransition);
        cameraTransition = StartCoroutine(SmoothTransitionCamera(mainCamera.transform.position, originalCamPosition, mainCamera.transform.rotation, originalCamRotation));

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        RestoreOriginalMaterials();

        currentSelectedPart = null;
        currentHoverPart = null;

        while (cameraTransition != null)
            yield return null;
    }

    IEnumerator SmoothTransitionCamera(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot)
    {
        float duration = 1f / focusTransitionSpeed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        mainCamera.transform.position = endPos;
        mainCamera.transform.rotation = endRot;

        Vector3 finalAngles = mainCamera.transform.eulerAngles;
        camYaw = finalAngles.y;
        camPitch = finalAngles.x;

        cameraTransition = null;
    }

    IEnumerator FadeOthers()
    {
        foreach (Renderer r in originalMaterials.Keys)
        {
            if (r == null || currentSelectedPart == null) continue;
            bool isFocusedPart = (r.transform == currentSelectedPart) || r.transform.IsChildOf(currentSelectedPart);

            if (!isFocusedPart)
            {
                StartCoroutine(FadeRenderer(r));
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        yield return null;
    }

    IEnumerator FadeRenderer(Renderer rend)
    {
        float targetAlpha = unfocusedOpacity;
        float duration = 1f / focusTransitionSpeed;

        // This creates new instances of the materials
        Material[] mats = rend.materials;
        float elapsed = 0f;

        float[] startAlpha = new float[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            startAlpha[i] = mats[i].color.a;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < mats.Length; i++)
            {
                Color c = mats[i].color;
                c.a = Mathf.Lerp(startAlpha[i], targetAlpha, t);
                mats[i].color = c;
                MakeMaterialTransparent(mats[i]);
            }
            yield return null;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            Color c = mats[i].color;
            c.a = targetAlpha;
            mats[i].color = c;
            MakeMaterialTransparent(mats[i]);
        }
    }

    void MakeMaterialTransparent(Material m)
    {
        // Use properties from the template material if it's assigned
        if (outlineMaterialTemplate != null)
        {
            m.SetInt("_SrcBlend", outlineMaterialTemplate.GetInt("_SrcBlend"));
            m.SetInt("_DstBlend", outlineMaterialTemplate.GetInt("_DstBlend"));
            m.SetInt("_ZWrite", outlineMaterialTemplate.GetInt("_ZWrite"));
            if (outlineMaterialTemplate.IsKeywordEnabled("_ALPHATEST_ON")) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            if (outlineMaterialTemplate.IsKeywordEnabled("_ALPHABLEND_ON")) m.EnableKeyword("_ALPHABLEND_ON"); else m.DisableKeyword("_ALPHABLEND_ON");
            m.renderQueue = outlineMaterialTemplate.renderQueue;
        }
        else // Fallback to default transparent settings
        {
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
        }
    }

    void CreateFloatingLabel(Vector3 position, string name)
    {
        if (floatingLabelPrefab == null) return;
        floatingLabel = Instantiate(floatingLabelPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        floatingLabel.name = "FloatingLabel_" + name;
        var textMesh = floatingLabel.GetComponent<TextMesh>();
        if (textMesh != null) textMesh.text = name;
    }

    void ClearFloatingLabel()
    {
        if (floatingLabel != null)
        {
            Destroy(floatingLabel);
            floatingLabel = null;
        }
    }

    void RestoreOriginalMaterials()
    {
        foreach (var pair in originalMaterials)
        {
            Renderer r = pair.Key;
            Material[] originalMats = pair.Value;

            if (r != null)
            {
                r.materials = originalMats;
            }
        }
        // Clear the instanced materials cache
        instancedMaterials.Clear();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Vector3 move = mainCamera.transform.forward * scroll * zoomSpeed;
            mainCamera.transform.position += move;
        }
    }

    Bounds CalculateBounds(Transform obj)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(obj.position, Vector3.one * 0.1f);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);
        return b;
    }

    float ComputeFitDistance(Bounds bounds, Camera cam)
    {
        Vector3 size = bounds.size;

        float verticalFOV = cam.fieldOfView * Mathf.Deg2Rad;
        float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(verticalFOV / 2f) * cam.aspect);

        float distanceVertical = size.y * 0.5f / Mathf.Tan(verticalFOV / 2f);
        float distanceHorizontal = size.x * 0.5f / Mathf.Tan(horizontalFOV / 2f);

        return Mathf.Max(distanceVertical, distanceHorizontal);
    }
}