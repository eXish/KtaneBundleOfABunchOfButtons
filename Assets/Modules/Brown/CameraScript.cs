using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {

    public Camera CameraObj;
    public MeshFilter[] MeshFilters;
    public Material WallMat;
    private void LateUpdate()
    {
        foreach (var f in MeshFilters)
            Graphics.DrawMesh(f.sharedMesh, f.transform.localToWorldMatrix, WallMat, 10, CameraObj);
    }
}
