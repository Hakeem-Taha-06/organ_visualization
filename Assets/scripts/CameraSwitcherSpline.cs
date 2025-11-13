using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcherSpline : MonoBehaviour
{
    public Camera mainCamera;
    public CinemachineCamera dollyCamera;
    public CinemachineSplineDolly splineDolly;   // drag the Spline Dolly Body component here

    private bool usingMain = true;

    void Start()
    {
        mainCamera.gameObject.SetActive(true);
        dollyCamera.gameObject.SetActive(false);

        if (splineDolly != null)
            splineDolly.CameraPosition = 0f;
    }

    void Update()
    {
        if (!usingMain && splineDolly != null && splineDolly.CameraPosition >= 1f)
            splineDolly.CameraPosition = 0f;
    }

    public void SwitchCamera()
    {
        usingMain = !usingMain;
        mainCamera.gameObject.SetActive(usingMain);
        dollyCamera.gameObject.SetActive(!usingMain);

        if (!usingMain && splineDolly != null)
            splineDolly.CameraPosition = 0f;
    }
}
