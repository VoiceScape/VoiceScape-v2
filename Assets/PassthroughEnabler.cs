using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PassthroughEnabler : MonoBehaviour
{
    public bool isEnabled;
    public ARCameraManager arCameraManager;
    
    private void Start()
    {
        if (arCameraManager == null)
        {
            arCameraManager = Camera.main.GetComponent<ARCameraManager>();
        }
    }

    private void Update()
    {
        isEnabled = arCameraManager.isActiveAndEnabled;
    }

    public void SetIsEnabled()
    {
        if (isEnabled)
        {
            arCameraManager.enabled = false;
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            arCameraManager.enabled = true;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
