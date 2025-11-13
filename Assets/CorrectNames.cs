using UnityEngine;

public class CorrectNames : MonoBehaviour
{
    [SerializeField] private GameObject Heart;
    [SerializeField] private Material Artery;
    [SerializeField] private Material Vein;

    public string GetPartName(string inputString)
    {
        if (string.IsNullOrEmpty(inputString))
        {
            return inputString;
        }

        int firstSpaceIndex = inputString.IndexOf(' ');

        int delimiterIndex = -1;

        if (firstSpaceIndex == -1)
        {
            delimiterIndex = inputString.LastIndexOf('_');
        }
        else
        {
            string prefix = inputString.Substring(0, firstSpaceIndex);
            delimiterIndex = prefix.LastIndexOf('_');
        }

        if (delimiterIndex != -1)
        {
            return inputString.Substring(delimiterIndex + 1);
        }

        return inputString;
    }

    public void DoCorrection() { 
        Transform[] allChildrenTransforms = Heart.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildrenTransforms)
        {
            child.gameObject.name = GetPartName(child.gameObject.name);
        }
    }

    public void DoColoring()
    {
        Transform[] allChildrenTransforms = Heart.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildrenTransforms)
        {
            var renderer = child.gameObject.GetComponent<MeshRenderer>();
            if (child.gameObject.name.Contains("artery") || child.gameObject.name.Contains("coronary"))
                renderer.material = Artery;
            if (child.gameObject.name.Contains("vein"))
                renderer.material = Vein;
        }
    }
}
