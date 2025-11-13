using UnityEngine;
using System; // For [Serializable] (though not used, matches brain script)

/// <summary>
/// This script color-codes heart model parts based on keywords in their GameObject name.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene (e.g., "HeartModel").
/// 2. Drag all your heart .obj files into the scene so they are CHILDREN of "HeartModel".
///    Their names should match the list (e.g., "Left Ventricle Wall").
/// 3. Attach this C# script ("HeartColorAssigner.cs") to the parent "HeartModel" GameObject.
/// 4. Adjust the colors in the Inspector for each category.
/// 5. Run the scene (if "Colorize On Start" is checked) or right-click the
///    script component in the Inspector and select "Colorize Heart Parts" to run it manually.
/// </summary>
public class HeartColorAssigner : MonoBehaviour
{
    [Header("Color Categories")]
    [Tooltip("Color for parts like 'artery', 'aorta', or 'trunk'.")]
    public Color arteryColor = new Color(1f, 0.2f, 0.2f);       // bright red

    [Tooltip("Color for parts like 'vein', 'sinus', or 'vena cava'.")]
    public Color veinColor = new Color(0.2f, 0.4f, 1f);         // blue

    [Tooltip("Color for 'ventricle', 'atrium', 'wall', or 'muscle'.")]
    public Color muscleColor = new Color(0.8f, 0.2f, 0.2f);     // dark red

    [Tooltip("Color for 'valve', 'leaflet', 'cusp', or 'anulus'.")]
    public Color valveColor = new Color(1f, 1f, 0.9f);          // off-white

    [Tooltip("Color for 'fibrous', 'tendon', 'septum', 'node', etc.")]
    public Color fibrousColor = new Color(1f, 0.9f, 0.6f);      // beige/yellow

    [Tooltip("Default color for any part that doesn't match the keywords.")]
    public Color defaultColor = new Color(0.9f, 0.6f, 0.6f);    // fallback

    [Header("Execution Settings")]
    [Tooltip("Check this to automatically run the colorizer when the scene starts.")]
    public bool colorizeOnStart = true;

    [Tooltip("Log all unmatched GameObjects to the console to help refine keywords.")]
    public bool logUnmatchedParts = true;

    // --- Keyword Lists ---
    // We define these here to make them easy to modify if needed.
    private readonly string[] arteryKeywords = { "artery", "aorta", "trunk" };
    private readonly string[] veinKeywords = { "vein", "sinus", "vena cava" };
    private readonly string[] muscleKeywords = { "ventricle", "atrium", "wall", "muscle" };
    private readonly string[] valveKeywords = { "valve", "leaflet", "cusp", "anulus" };
    private readonly string[] fibrousKeywords = { "fibrous", "tendon", "trigone", "bundle", "node", "septum" };

    /// <summary>
    /// Automatically runs when the scene starts if enabled.
    /// </summary>
    void Start()
    {
        if (colorizeOnStart)
        {
            ColorizeHeartParts();
        }
    }

    /// <summary>
    /// This method can be called from other scripts or from the Inspector.
    /// It iterates through all child renderers and applies colors.
    /// </summary>
    [ContextMenu("Colorize Heart Parts")]
    public void ColorizeHeartParts()
    {
        // Get all MeshRenderers in this object and all its children.
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true); // 'true' includes inactive

        if (renderers.Length == 0)
        {
            Debug.LogWarning("HeartColorAssigner: No MeshRenderers found in children of " + gameObject.name, this);
            return;
        }

        int coloredParts = 0;
        int unmatchedParts = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            string objectName = renderer.gameObject.name.ToLower();

            // Using renderer.material creates a new instance of the material
            Material partMaterial = renderer.material;

            // --- Color Logic ---
            // 1. Arteries
            if (NameContainsKeywords(objectName, arteryKeywords))
            {
                partMaterial.color = arteryColor;
            }
            // 2. Veins
            else if (NameContainsKeywords(objectName, veinKeywords))
            {
                partMaterial.color = veinColor;
            }
            // 3. Muscle
            else if (NameContainsKeywords(objectName, muscleKeywords))
            {
                partMaterial.color = muscleColor;
            }
            // 4. Valves
            else if (NameContainsKeywords(objectName, valveKeywords))
            {
                partMaterial.color = valveColor;
            }
            // 5. Fibrous/Conduction
            else if (NameContainsKeywords(objectName, fibrousKeywords))
            {
                partMaterial.color = fibrousColor;
            }
            // 6. Default / Unmatched
            else
            {
                partMaterial.color = defaultColor;
                if (logUnmatchedParts)
                {
                    Debug.Log("HeartColorAssigner: Unmatched part - " + renderer.gameObject.name);
                }
                unmatchedParts++;
                continue; // Skip the 'coloredParts++'
            }

            coloredParts++;
        }

        Debug.Log($"HeartColorAssigner: Finished. Colored {coloredParts} parts. {unmatchedParts} parts were unmatched (set to default).", this);
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