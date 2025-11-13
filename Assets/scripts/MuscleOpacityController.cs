using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MuscleOpacityController : MonoBehaviour
{
    [Header("Setup")]
    public Transform femurRoot;      // Parent containing all muscle parts
    public Slider opacitySlider;     // The UI slider

    [Header("Keyword Matching")]
    public List<string> keywords = new List<string>
    {
        "adductor", "gracilis", "pectineus", "rectus femoris",
        "sartorius", "semimembranosus", "semitendinosus", "vastus", "biceps femoris"
    };

    private List<Renderer> muscleRenderers = new List<Renderer>();

    void Start()
    {
        if (femurRoot == null)
        {
            Debug.LogError("❌ Femur root not assigned!");
            return;
        }

        // Collect all renderers whose names contain any keyword
        Renderer[] allRenderers = femurRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in allRenderers)
        {
            string lowerName = r.name.ToLower();
            foreach (string k in keywords)
            {
                if (lowerName.Contains(k.ToLower()))
                {
                    muscleRenderers.Add(r);
                    break;
                }
            }
        }

        Debug.Log($"✅ Found {muscleRenderers.Count} muscle parts matching keywords.");

        // Assign slider event if available
        if (opacitySlider != null)
        {
            opacitySlider.onValueChanged.AddListener(UpdateOpacity);
        }
        else
        {
            Debug.LogWarning("⚠️ No slider assigned!");
        }
    }

    void UpdateOpacity(float value)
    {
        float alpha = Mathf.Clamp01(value);

        foreach (Renderer r in muscleRenderers)
        {
            if (r == null) continue;

            foreach (Material m in r.materials)
            {
                if (m == null || !m.HasProperty("_BaseColor")) continue;

                Color c = m.GetColor("_BaseColor");
                c.a = alpha;
                m.SetColor("_BaseColor", c);

                if (alpha < 0.99f)
                {
                    // --- Force URP transparency ---
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
                    // --- Back to opaque ---
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

        Debug.Log($"🎚 Updated opacity to {alpha:0.00}");
    }
}
