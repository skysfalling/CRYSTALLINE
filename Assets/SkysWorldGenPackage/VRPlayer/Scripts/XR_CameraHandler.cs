using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XR_CameraHandler : MonoBehaviour
{
    public Transform headCam;
    public Transform headCamFocusPoint;

    public Transform handheldCam;



    // Update is called once per frame
    void Update()
    {
        LookAtPoint(headCam, headCamFocusPoint);
    }

    public void LookAtPoint(Transform cam , Transform point)
    {
        cam.LookAt(point);
    }
}
