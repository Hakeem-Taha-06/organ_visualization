using UnityEngine;
using System; // For [Serializable]

/// <summary>
/// This script color-codes femur model parts based on keywords in their GameObject name.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g., "FemurModel").
/// 2. Drag all your femur .obj files into the scene so they are CHILDREN of "FemurModel".
///    Their names should match the list (e.g., "Left rectus femoris").
/// 3. Attach this C# script ("FemurColorAssigner.cs") to the parent "FemurModel" GameObject.
/// 4. Adjust the colors in the Inspector for each category.
/// 5. Run the scene (if "Colorize On Start" is checked) or right-click the
///    script component in the Inspector and select "Colorize Femur Parts" to run it manually.
/// </summary>
public class FemurColorAssigner : MonoBehaviour
{
    [Header("Color Categories")]
    [Tooltip("Color for parts with 'artery' in the name.")]
    public Color arteryColor = new Color(1f, 0f, 0f, 1f); // Default: Red

    [Tooltip("Color for parts with 'vein' in the name.")]
    public Color veinColor = new Color(0.2f, 0.4f, 1f, 1f); // Default: Blue

    [Tooltip("Color for muscles (adductor, vastus, biceps, etc.).")]
    public Color muscleColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Default: Dark Red

    [Tooltip("Color for 'femur' and 'hip bone'.")]
    public Color boneColor = new Color(0.95f, 0.9f, 0.8f, 1f); // Default: Off-White/Beige

    [Tooltip("Default color for any part that doesn't match the keywords.")]
    public Color defaultColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Default: Light Gray

    [Header("Execution Settings")]
    [Tooltip("Check this to automatically run the colorizer when the scene starts.")]
    public bool colorizeOnStart = true;

    [Tooltip("Log all unmatched GameObjects to the console to help refine keywords.")]
    public bool logUnmatchedParts = true;

    // --- Keyword Lists ---
    // Based on the .obj list you provided
    private readonly string[] arteryKeywords = { "artery" };
    private readonly string[] veinKeywords = { "vein" };
    private readonly string[] muscleKeywords = {
        "adductor", "gracilis", "pectineus", "sartorius",
        "semimembranosus", "semitendinosus", "vastus", "biceps", "rectus"
    };
    private readonly string[] boneKeywords = { "femur", "bone" };


    /// <summary>
    /// Automatically runs when the scene starts if enabled.
    /// </summary>
    void Start()
    {
        if (colorizeOnStart)
        {
            ColorizeFemurParts();
        }
    }

    /// <summary>
    /// This method can be called from other scripts or from the Inspector.
    /// It iterates through all child renderers and applies colors.
    /// </summary>
    [ContextMenu("Colorize Femur Parts")]
    public void ColorizeFemurParts()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("FemurColorAssigner: No MeshRenderers found in children of " + gameObject.name, this);
            return;
        }

        int coloredParts = 0;
        int unmatchedParts = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            string objectName = renderer.gameObject.name.ToLower();
            Material partMaterial = renderer.material;

            // --- Color Logic ---
            // Order is important to prevent mis-coloring (e.g., "biceps femoris" as bone)

            // 1. Arteries (Red)
            if (NameContainsKeywords(objectName, arteryKeywords))
            {
                partMaterial.color = arteryColor;
            }
            // 2. Veins (Blue)
            else if (NameContainsKeywords(objectName, veinKeywords))
            {
                partMaterial.color = veinColor;
            }
            // 3. Muscles (Dark Red)
            else if (NameContainsKeywords(objectName, muscleKeywords))
            {
                partMaterial.color = muscleColor;
            }
            // 4. Bones (Beige)
            else if (NameContainsKeywords(objectName, boneKeywords))
            {
                partMaterial.color = boneColor;
            }
            // 5. Default / Unmatched
            else
            {
                partMaterial.color = defaultColor;
                if (logUnmatchedParts)
                {
                    Debug.Log("FemurColorAssigner: Unmatched part - " + renderer.gameObject.name);
                }
                unmatchedParts++;
                continue;
            }

            coloredParts++;
        }

        Debug.Log($"FemurColorAssigner: Finished. Colored {coloredParts} parts. {unmatchedParts} parts were unmatched (set to default).", this);
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