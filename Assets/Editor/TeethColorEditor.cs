using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TeethColorAssigner))]
public class TeethColorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default Inspector
        // This shows all the normal public fields (like 'message' and 'clickCount')
        DrawDefaultInspector();

        // 2. Get a reference to the script we are inspecting
        TeethColorAssigner myScript = (TeethColorAssigner)target;

        // 3. Add some space
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Change Color"))
        {
            // 5. If clicked, call the public function on our script
            myScript.ColorizeSkullParts();
        }
    }
}
