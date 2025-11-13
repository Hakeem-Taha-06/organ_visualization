using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeartOpacityController : MonoBehaviour
{
    [Header("UI References")]
    public Slider leftVentricleSlider;
    public Slider rightVentricleSlider;
    public Slider leftAtriumSlider;
    public Slider rightAtriumSlider;

    [Header("Heart Root")]
    public Transform heartRoot; // Parent containing all heart parts

    // Keywords for each chamber
    private readonly string[] leftVentricleKeys = { "left ventricle" };
    private readonly string[] rightVentricleKeys = { "right ventricle" };
    private readonly string[] leftAtriumKeys = { "left atrium" };
    private readonly string[] rightAtriumKeys = { "right atrium" };

    // Cached renderers
    private List<Renderer> leftVentricleRenderers = new List<Renderer>();
    private List<Renderer> rightVentricleRenderers = new List<Renderer>();
    private List<Renderer> leftAtriumRenderers = new List<Renderer>();
    private List<Renderer> rightAtriumRenderers = new List<Renderer>();

    void Start()
    {
        if (heartRoot == null)
        {
            Debug.LogError("❌ Heart root not assigned!");
            return;
        }

        // Find all renderers
        Renderer[] allRenderers = heartRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            string nameLower = r.name.ToLower();

            if (MatchesAny(nameLower, leftVentricleKeys)) leftVentricleRenderers.Add(r);
            if (MatchesAny(nameLower, rightVentricleKeys)) rightVentricleRenderers.Add(r);
            if (MatchesAny(nameLower, leftAtriumKeys)) leftAtriumRenderers.Add(r);
            if (MatchesAny(nameLower, rightAtriumKeys)) rightAtriumRenderers.Add(r);
        }

        Debug.Log($"✅ Heart parts found: LV={leftVentricleRenderers.Count}, RV={rightVentricleRenderers.Count}, LA={leftAtriumRenderers.Count}, RA={rightAtriumRenderers.Count}");

        // Connect sliders
        if (leftVentricleSlider != null) leftVentricleSlider.onValueChanged.AddListener((v) => UpdateOpacity(leftVentricleRenderers, v));
        if (rightVentricleSlider != null) rightVentricleSlider.onValueChanged.AddListener((v) => UpdateOpacity(rightVentricleRenderers, v));
        if (leftAtriumSlider != null) leftAtriumSlider.onValueChanged.AddListener((v) => UpdateOpacity(leftAtriumRenderers, v));
        if (rightAtriumSlider != null) rightAtriumSlider.onValueChanged.AddListener((v) => UpdateOpacity(rightAtriumRenderers, v));

        // Apply initial values
        if (leftVentricleSlider != null) UpdateOpacity(leftVentricleRenderers, leftVentricleSlider.value);
        if (rightVentricleSlider != null) UpdateOpacity(rightVentricleRenderers, rightVentricleSlider.value);
        if (leftAtriumSlider != null) UpdateOpacity(leftAtriumRenderers, leftAtriumSlider.value);
        if (rightAtriumSlider != null) UpdateOpacity(rightAtriumRenderers, rightAtriumSlider.value);
    }

    bool MatchesAny(string name, string[] keys)
    {
        foreach (var key in keys)
        {
            if (name.Contains(key.ToLower()))
                return true;
        }
        return false;
    }

    void UpdateOpacity(List<Renderer> renderers, float value)
    {
        float alpha = Mathf.Clamp01(value);

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            foreach (Material m in r.materials)
            {
                if (m == null || !m.HasProperty("_BaseColor")) continue;

                Color c = m.GetColor("_BaseColor");
                c.a = alpha;
                m.SetColor("_BaseColor", c);

                // URP transparent fix
                if (alpha < 0.99f)
                {
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.SetInt("_Surface", 1);
                    m.SetInt("_Blend", 0);
                    m.SetInt("_ZWrite", 0);
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                    m.EnableKeyword("_ALPHAPREMULTIPLY_ON");

                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                else
                {
                    m.SetOverrideTag("RenderType", "Opaque");
                    m.SetInt("_Surface", 0);
                    m.SetInt("_ZWrite", 1);
                    m.renderQueue = -1;

                    m.EnableKeyword("_SURFACE_TYPE_OPAQUE");
                    m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                }
            }
        }
    }
}
