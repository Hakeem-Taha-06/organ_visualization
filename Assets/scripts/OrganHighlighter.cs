using System.Collections.Generic;
using UnityEngine;

public class HoverHighlighter : MonoBehaviour
{
    [Tooltip("Drag your OrganFocusManager here (auto-find if null).")]
    public OrganFocusManager focusManager;

    [Tooltip("Hover color (visible tint).")]
    public Color hoverColor = Color.yellow;

    private Transform lastHoveredOrgan;
    private Renderer[] lastHoveredRenderers;
    private MaterialPropertyBlock mpb;

    private readonly string[] colorProps = { "_BaseColor", "_Color", "_TintColor", "_EmissiveColor" };

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (focusManager == null)
            focusManager = FindObjectOfType<OrganFocusManager>();
    }

    void Update()
    {
        if (focusManager == null || Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform focusable = FindFocusableAncestor(hit.transform);
            if (focusable != null)
            {
                if (focusable != lastHoveredOrgan)
                {
                    ClearLastHover();
                    ApplyHover(focusable);
                }
                return;
            }
        }

        ClearLastHover();
    }

    private Transform FindFocusableAncestor(Transform t)
    {
        while (t != null)
        {
            if (focusManager.organs.Contains(t))
                return t;
            t = t.parent;
        }
        return null;
    }

    private void ApplyHover(Transform organ)
    {
        Renderer[] renderers = organ.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0) return;

        string propName = FindColorProperty(renderers[0]);

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            mpb.Clear();

            // set both base and emissive (for HDRP/URP support)
            mpb.SetColor(propName, hoverColor);
            if (rend.sharedMaterial.HasProperty("_EmissionColor"))
                mpb.SetColor("_EmissionColor", hoverColor * 1.2f);

            rend.SetPropertyBlock(mpb);
        }

        lastHoveredOrgan = organ;
        lastHoveredRenderers = renderers;
    }

    private void ClearLastHover()
    {
        if (lastHoveredRenderers == null) return;
        foreach (Renderer rend in lastHoveredRenderers)
        {
            if (rend == null) continue;
            rend.SetPropertyBlock(null);
        }
        lastHoveredRenderers = null;
        lastHoveredOrgan = null;
    }

    private string FindColorProperty(Renderer rend)
    {
        if (rend == null || rend.sharedMaterial == null) return "_Color";
        foreach (var prop in colorProps)
        {
            if (rend.sharedMaterial.HasProperty(prop))
                return prop;
        }
        return "_Color";
    }

    void OnDisable() => ClearLastHover();
    void OnDestroy() => ClearLastHover();
}
