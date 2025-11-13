using UnityEngine;
using System.Collections.Generic; // Used for logging

/// <summary>
/// This script color-codes skull model parts based on keywords in their GameObject name.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g., "Skull_Parent").
/// 2. Drag all your skull .obj files into the scene so they are CHILDREN of "Skull_Parent".
///    Their names should match your file list (e.g., "Left first lower molar tooth", "Frontal bone").
/// 3. Attach this C# script ("SkullColorizer.cs") to the parent "Skull_Parent" GameObject.
/// 4. Adjust the colors in the Inspector for each category.
/// 5. Run the scene (if "Colorize On Start" is checked) or right-click the
///    script component in the Inspector and select "Colorize Skull Parts" to run it manually.
/// </summary>
public class TeethColorAssigner : MonoBehaviour
{
    [Header("Color Categories")]
    [Tooltip("Color for all parts with 'tooth', 'incisor', 'canine', 'premolar', or 'molar'.")]
    public Color teethColor = new Color(1f, 1f, 0.9f, 1f); // Default: Off-white

    [Tooltip("Color for the main cranium bones (frontal, parietal, occipital, etc.).")]
    public Color craniumColor = new Color(0.9f, 0.88f, 0.85f, 1f); // Default: Bone-white

    [Tooltip("Color for facial bones (maxilla, mandible, zygomatic, etc.).")]
    public Color faceColor = new Color(0.85f, 0.83f, 0.8f, 1f); // Default: Slightly darker bone

    [Tooltip("Color for vertebrae and hyoid bone.")]
    public Color vertebraeColor = new Color(0.8f, 0.8f, 0.78f, 1f); // Default: Even darker bone

    [Tooltip("Default color for any part that doesn't match the keywords.")]
    public Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Default: Light Gray

    [Header("Execution Settings")]
    [Tooltip("Check this to automatically run the colorizer when the scene starts.")]
    public bool colorizeOnStart = true;

    [Tooltip("Log all unmatched GameObjects to the console to help refine keywords.")]
    public bool logUnmatchedParts = true;

    // --- Keyword Lists ---
    // Based on your file list
    private readonly string[] teethKeywords = {
        "tooth", "incisor", "canine", "premolar", "molar"
    };

    private readonly string[] craniumKeywords = {
        "frontal", "parietal", "occipital", "temporal", "sphenoid", "ethmoid"
    };

    private readonly string[] faceKeywords = {
        "maxilla", "mandible", "zygomatic", "nasal", "lacrimal", "palatine", "vomer", "concha"
    };

    private readonly string[] vertebraeKeywords = {
        "atlas", "axis", "vertebra", "hyoid"
    };


    /// <summary>
    /// Automatically runs when the scene starts if enabled.
    /// </summary>
    void Start()
    {
        if (colorizeOnStart)
        {
            ColorizeSkullParts();
        }
    }

    /// <summary>
    /// This method can be called from other scripts or from the Inspector.
    /// It iterates through all child renderers and applies colors.
    /// </summary>
    [ContextMenu("Colorize Skull Parts")]
    public void ColorizeSkullParts()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true); // 'true' includes inactive

        if (renderers.Length == 0)
        {
            Debug.LogWarning("SkullColorizer: No MeshRenderers found in children of " + gameObject.name, this);
            return;
        }

        int coloredParts = 0;
        int unmatchedParts = 0;
        List<string> unmatchedNames = new List<string>(); // To store names for logging

        foreach (MeshRenderer renderer in renderers)
        {
            string objectName = renderer.gameObject.name.ToLower();

            // This creates a new material instance for this object,
            // so coloring one bone doesn't color all others.
            Material partMaterial = renderer.material;

            // --- Color Logic ---
            // We check for the most specific categories first (like teeth).

            // 1. Teeth (Off-white)
            if (NameContainsKeywords(objectName, teethKeywords))
            {
                partMaterial.color = teethColor;
            }
            // 2. Vertebrae & Hyoid (Darker Bone 1)
            else if (NameContainsKeywords(objectName, vertebraeKeywords))
            {
                partMaterial.color = vertebraeColor;
            }
            // 3. Facial Bones (Darker Bone 2)
            else if (NameContainsKeywords(objectName, faceKeywords))
            {
                partMaterial.color = faceColor;
            }
            // 4. Cranium Bones (Main Bone-white)
            else if (NameContainsKeywords(objectName, craniumKeywords))
            {
                partMaterial.color = craniumColor;
            }
            // 5. Default / Unmatched
            else
            {
                partMaterial.color = defaultColor;
                if (!unmatchedNames.Contains(renderer.gameObject.name))
                {
                    unmatchedNames.Add(renderer.gameObject.name);
                }
                unmatchedParts++;
                continue; // Skip the 'coloredParts++'
            }

            coloredParts++;
        }

        // Log results
        Debug.Log($"SkullColorizer: Finished. Colored {coloredParts} parts. {unmatchedParts} parts were unmatched (set to default).", this);

        if (logUnmatchedParts && unmatchedNames.Count > 0)
        {
            Debug.LogWarning("SkullColorizer: Unmatched parts: " + string.Join(", ", unmatchedNames));
        }
    }

    /// <summary>
    /// Helper function to check if a name contains any of the keywords.
    /// </summary>
    private bool NameContainsKeywords(string name, string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (name.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }
}