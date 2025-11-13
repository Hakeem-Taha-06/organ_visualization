using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FemurColorAssigner))]
public class FemurColorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default Inspector
        // This shows all the normal public fields (like 'message' and 'clickCount')
        DrawDefaultInspector();

        // 2. Get a reference to the script we are inspecting
        FemurColorAssigner myScript = (FemurColorAssigner)target;

        // 3. Add some space
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Change color"))
        {
            // 5. If clicked, call the public function on our script
            myScript.ColorizeFemurParts();
        }
    }
}
