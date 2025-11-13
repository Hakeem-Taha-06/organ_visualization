using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkullOpacityController : MonoBehaviour
{
    [Header("UI Reference")]
    public Slider skullSlider;       // The slider controlling skull opacity

    [Header("Skull Root")]
    public Transform skullRoot;      // Parent containing all skull parts

    [Header("Keywords to Exclude")]
    public List<string> excludeKeywords = new List<string> { "mandible", "maxilla", "tooth" };

    private List<Renderer> skullRenderers = new List<Renderer>();

    void Start()
    {
        if (skullRoot == null || skullSlider == null)
        {
            Debug.LogError("❌ Missing references!");
            return;
        }

        // Collect all renderers that **do NOT match exclude keywords**
        Renderer[] allRenderers = skullRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            string lowerName = r.gameObject.name.ToLower();
            bool exclude = false;

            foreach (string key in excludeKeywords)
            {
                if (lowerName.Contains(key.ToLower()))
                {
                    exclude = true;
                    break;
                }
            }

            if (!exclude)
                skullRenderers.Add(r);
        }

        Debug.Log($"✅ Skull parts controlled by slider: {skullRenderers.Count}");

        // Connect slider event
        skullSlider.onValueChanged.AddListener(UpdateOpacity);

        // Apply initial value
        UpdateOpacity(skullSlider.value);
    }

    void UpdateOpacity(float value)
    {
        float alpha = Mathf.Clamp01(value);

        foreach (Renderer r in skullRenderers)
        {
            if (r == null) continue;

            foreach (Material m in r.materials)
            {
                if (m == null || !m.HasProperty("_BaseColor")) continue;

                Color c = m.GetColor("_BaseColor");
                c.a = alpha;
                m.SetColor("_BaseColor", c);

                // URP transparency
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
