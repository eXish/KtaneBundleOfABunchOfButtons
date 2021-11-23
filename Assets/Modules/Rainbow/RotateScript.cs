using UnityEngine;

public class RotateScript : MonoBehaviour
{
    [SerializeField]
    private float _rate;
    [SerializeField]
    private AnimationCurve _xCurve, _yCurve, _zCurve;

    private float _time;

    private void FixedUpdate()
    {
        _time += Time.fixedDeltaTime;
        transform.localRotation = transform.localRotation 
            * Quaternion.AngleAxis(_rate * _xCurve.Evaluate(_time / 10f), Vector3.right) 
            * Quaternion.AngleAxis(_rate * _yCurve.Evaluate(_time / 10f), Vector3.up)
            * Quaternion.AngleAxis(_rate * _zCurve.Evaluate(_time / 10f), Vector3.forward);
    }
}
