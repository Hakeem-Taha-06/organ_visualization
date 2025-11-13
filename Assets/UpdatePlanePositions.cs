using UnityEngine;

[ExecuteAlways] // So it updates in edit mode too
public class UpdatePlanePositions : MonoBehaviour
{
    [Header("Shader Property Names (must match your shader)")]
    public string plane1Property = "_Plane1Pos";
    public string plane2Property = "_Plane2Pos";
    public string plane3Property = "_Plane3Pos";

    [Header("Scene References")]
    public GameObject plane1;
    public GameObject plane2;
    public GameObject plane3;

    [Header("Material to update")]
    public Material targetMaterial;

    void Update()
    {
        if (targetMaterial == null) return;

        if (plane1 != null)
            targetMaterial.SetVector(plane1Property, plane1.transform.position);

        if (plane2 != null)
            targetMaterial.SetVector(plane2Property, plane2.transform.position);

        if (plane3 != null)
            targetMaterial.SetVector(plane3Property, plane3.transform.position);
    }
}
