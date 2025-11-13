using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BrainOpacityController_Final : MonoBehaviour
{
    [Header("UI References")]
    public Slider arteriesVeinsSlider;
    public Slider leftBrainSlider;
    public Slider rightBrainSlider;

    [Header("Brain Root")]
    public Transform brainRoot;

    // Keyword lists
    private readonly string[] arteriesVeinsKeywords = { "artery", "vein", "sinus", "branch" };
    private readonly string[] leftBrainKeywords = { "left" };
    private readonly string[] rightBrainKeywords = { "right" };

    // Cached renderers
    private List<Renderer> arteriesVeinsRenderers = new List<Renderer>();
    private List<Renderer> leftBrainRenderers = new List<Renderer>();
    private List<Renderer> rightBrainRenderers = new List<Renderer>();

    void Start()
    {
        if (brainRoot == null)
        {
            Debug.LogError("❌ Brain root not assigned!");
            return;
        }

        Renderer[] allRenderers = brainRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            string nameLower = r.gameObject.name.ToLower();

            // Arteries & Veins
            if (MatchesAny(nameLower, arteriesVeinsKeywords))
            {
                arteriesVeinsRenderers.Add(r);
                continue; // skip for left/right brain to avoid interference
            }

            // Left/Right Brain assignment (excluding vessels)
            if (MatchesAny(nameLower, leftBrainKeywords))
                leftBrainRenderers.Add(r);
            if (MatchesAny(nameLower, rightBrainKeywords))
                rightBrainRenderers.Add(r);
        }

        Debug.Log($"✅ Brain Parts Found - Arteries/Veins: {arteriesVeinsRenderers.Count}, Left: {leftBrainRenderers.Count}, Right: {rightBrainRenderers.Count}");

        // Connect sliders
        if (arteriesVeinsSlider != null) arteriesVeinsSlider.onValueChanged.AddListener(v => UpdateOpacity(arteriesVeinsRenderers, v));
        if (leftBrainSlider != null) leftBrainSlider.onValueChanged.AddListener(v => UpdateOpacity(leftBrainRenderers, v));
        if (rightBrainSlider != null) rightBrainSlider.onValueChanged.AddListener(v => UpdateOpacity(rightBrainRenderers, v));

        // Apply initial slider values
        if (arteriesVeinsSlider != null) UpdateOpacity(arteriesVeinsRenderers, arteriesVeinsSlider.value);
        if (leftBrainSlider != null) UpdateOpacity(leftBrainRenderers, leftBrainSlider.value);
        if (rightBrainSlider != null) UpdateOpacity(rightBrainRenderers, rightBrainSlider.value);
    }

    bool MatchesAny(string name, string[] keywords)
    {
        foreach (var key in keywords)
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

                // URP Transparent Setup
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
