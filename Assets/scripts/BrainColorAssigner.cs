using UnityEngine;
using System; // For [Serializable]

/// <summary>
/// This script color-codes brain model parts based on keywords in their GameObject name.
/// 
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g., "BrainModel").
/// 2. Drag all your brain .obj files into the scene so they are CHILDREN of "BrainModel".
///    Their names should match the list (e.g., "Anterior communicating artery_1_1_1_2").
/// 3. Attach this C# script ("BrainColorizer.cs") to the parent "BrainModel" GameObject.
/// 4. Adjust the colors in the Inspector for each category.
/// 5. Run the scene (if "Colorize On Start" is checked) or right-click the
///    script component in the Inspector and select "Colorize Brain Parts" to run it manually.
/// </summary>
public class BrainColorAssigner : MonoBehaviour
{
    [Header("Color Categories")]
    [Tooltip("Color for parts with 'artery' or 'arteri' in the name.")]
    public Color arteryColor = new Color(1f, 0f, 0f, 1f); // Default: Red

    [Tooltip("Color for parts with 'vein' or 'sinus' in the name.")]
    public Color veinColor = new Color(0.2f, 0.4f, 1f, 1f); // Default: Blue

    [Tooltip("Color for brain tissue (gyri, lobes, cortex).")]
    public Color gyrusColor = new Color(0.9f, 0.75f, 0.7f, 1f); // Default: Pinkish-Beige

    [Tooltip("Color for brainstem parts like 'medulla' or 'colliculus'.")]
    public Color brainstemColor = new Color(0.8f, 0.7f, 0.65f, 1f); // Default: Darker Beige

    [Tooltip("Default color for any part that doesn't match the keywords.")]
    public Color defaultColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Default: Light Gray

    [Header("Execution Settings")]
    [Tooltip("Check this to automatically run the colorizer when the scene starts.")]
    public bool colorizeOnStart = true;

    [Tooltip("Log all unmatched GameObjects to the console to help refine keywords.")]
    public bool logUnmatchedParts = true;

    // --- Keyword Lists ---
    // We define these here to make them easy to modify if needed.
    private readonly string[] arteryKeywords = { "artery", "arteri" };
    private readonly string[] veinKeywords = { "vein", "sinus" };
    private readonly string[] gyrusKeywords = {
        "gyrus", "gyri", "lobe", "lobule", "cuneus", "precuneus",
        "insula", "hippocampus", "parahippocampal", "frontal",
        "parietal", "temporal", "occipital"
    };
    private readonly string[] brainstemKeywords = { "medulla", "colliculus" };


    /// <summary>
    /// Automatically runs when the scene starts if enabled.
    /// </summary>
    

    /// <summary>
    /// This method can be called from other scripts or from the Inspector.
    /// It iterates through all child renderers and applies colors.
    /// </summary>
    [ContextMenu("Colorize Brain Parts")]
    public void ColorizeBrainParts()
    {
        // Get all MeshRenderers in this object and all its children.
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true); // 'true' includes inactive

        if (renderers.Length == 0)
        {
            Debug.LogWarning("BrainColorizer: No MeshRenderers found in children of " + gameObject.name, this);
            return;
        }

        int coloredParts = 0;
        int unmatchedParts = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            string objectName = renderer.gameObject.name.ToLower();

            // Using renderer.material creates a new instance of the material for this object.
            // This is crucial so that coloring one "artery" doesn't color all other
            // objects that might have shared the same default material.
            Material partMaterial = renderer.material;

            // --- Color Logic ---
            // The order is important! We check for arteries/veins first,
            // because many of them ALSO have gyrus keywords (e.g., "temporal artery").

            // 1. Arteries (Red)
            if (NameContainsKeywords(objectName, arteryKeywords))
            {
                partMaterial.color = arteryColor;
            }
            // 2. Veins & Sinuses (Blue)
            else if (NameContainsKeywords(objectName, veinKeywords))
            {
                partMaterial.color = veinColor;
            }
            // 3. Brain Tissue / Gyri (Beige/Pink)
            else if (NameContainsKeywords(objectName, gyrusKeywords))
            {
                partMaterial.color = gyrusColor;
            }
            // 4. Brainstem (Darker Beige)
            else if (NameContainsKeywords(objectName, brainstemKeywords))
            {
                partMaterial.color = brainstemColor;
            }
            // 5. Default / Unmatched
            else
            {
                partMaterial.color = defaultColor;
                if (logUnmatchedParts)
                {
                    Debug.Log("BrainColorizer: Unmatched part - " + renderer.gameObject.name);
                }
                unmatchedParts++;
                continue; // Skip the 'coloredParts++'
            }

            coloredParts++;
        }

        Debug.Log($"BrainColorizer: Finished. Colored {coloredParts} parts. {unmatchedParts} parts were unmatched (set to default).", this);
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