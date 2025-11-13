using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrainColorAssigner))]
public class BrainColorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default Inspector
        // This shows all the normal public fields (like 'message' and 'clickCount')
        DrawDefaultInspector();

        // 2. Get a reference to the script we are inspecting
        BrainColorAssigner myScript = (BrainColorAssigner)target;

        // 3. Add some space
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Change Color"))
        {
            // 5. If clicked, call the public function on our script
            myScript.ColorizeBrainParts();
        }
    }
}
