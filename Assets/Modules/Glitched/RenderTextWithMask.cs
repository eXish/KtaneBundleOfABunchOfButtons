using UnityEngine;

public class RenderTextWithMask : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer _renderer;
    [SerializeField]
    private MeshFilter _filter;
    [SerializeField]
    private Camera _cam;

    private void Start()
    {
        MeshRenderer r = GetComponent<MeshRenderer>();
        r.material = Instantiate(r.material);
        RenderTexture rtex = new RenderTexture(Camera.main.scaledPixelWidth, Camera.main.scaledPixelHeight, 24);
        _cam.transform.parent = Camera.main.transform.parent;
        _cam.nearClipPlane = Camera.main.nearClipPlane;
        _cam.farClipPlane = Camera.main.farClipPlane;
        _cam.targetTexture = rtex;
        _cam.cullingMask = 1 << 10;
        _cam.clearFlags = CameraClearFlags.Color;
        _cam.backgroundColor = new Color(0f, 0f, 0f);
        r.material.SetTexture("_MaskTex", rtex);
    }

    private void LateUpdate()
    {
        _cam.transform.localPosition = Camera.main.transform.localPosition;
        _cam.transform.localRotation = Camera.main.transform.localRotation;
        _cam.transform.localScale = Camera.main.transform.localScale;
        _cam.fieldOfView = Camera.main.fieldOfView;
        Graphics.DrawMesh(_filter.mesh, _filter.transform.localToWorldMatrix, _renderer.material, 10, _cam);
    }
}
