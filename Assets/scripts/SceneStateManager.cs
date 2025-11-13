using UnityEngine;
using System.IO;

[System.Serializable]
public class SceneState
{
    public string focusedOrganName;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
}

public class SceneStateManager : MonoBehaviour
{
    public OrganFocusManager focusManager;
    public Transform cam;

    private string savePath;

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "scene_state.json");
    }

    void Start()
    {
        if (focusManager == null)
            focusManager = FindObjectOfType<OrganFocusManager>();
        if (cam == null)
            cam = Camera.main.transform;

        LoadSceneState();
    }

    void OnApplicationQuit()
    {
        SaveSceneState();
    }

    public void SaveSceneState()
    {
        if (focusManager == null || cam == null) return;

        SceneState state = new SceneState();

        // Save current focused organ (if any)
        if (focusManager.GetCurrentOrgan() != null)
            state.focusedOrganName = focusManager.GetCurrentOrgan().name;

        // Save camera position/rotation
        state.cameraPosition = cam.position;
        state.cameraRotation = cam.rotation;

        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"✅ Scene state saved to {savePath}");
    }

    public void LoadSceneState()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        SceneState state = JsonUtility.FromJson<SceneState>(json);
        if (state == null) return;

        Debug.Log("📂 Scene state loaded.");

        // Restore camera position & rotation
        cam.position = state.cameraPosition;
        cam.rotation = state.cameraRotation;

        // Restore focus if valid
        if (!string.IsNullOrEmpty(state.focusedOrganName))
        {
            foreach (Transform organ in focusManager.organs)
            {
                if (organ.name == state.focusedOrganName)
                {
                    focusManager.SetFocus(organ);
                    break;
                }
            }
        }
    }
}
