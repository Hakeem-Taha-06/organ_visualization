using UnityEngine;
using UnityEditor;
using System.IO;

public class BrainMeshLister : MonoBehaviour
{
    [MenuItem("Tools/Export Brain Mesh Names")]
    static void ExportMeshes()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Select the Brain root object in the Hierarchy!");
            return;
        }

        Transform root = Selection.activeGameObject.transform;
        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);

        string path = Application.dataPath + "/BrainMeshNames.txt";
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("---- Brain Mesh Names ----");
            foreach (Renderer r in allRenderers)
            {
                writer.WriteLine(r.gameObject.name);
            }
            writer.WriteLine($"Total meshes: {allRenderers.Length}");
        }

        Debug.Log($"✅ Brain mesh names exported to: {path}");
        AssetDatabase.Refresh();
    }
}
