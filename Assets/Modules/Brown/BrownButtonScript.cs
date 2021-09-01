using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using BrownButton;
using Rnd = UnityEngine.Random;
using System.Collections.Generic;

public class BrownButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable BrownButtonSelectable;
    public GameObject BrownButtonCap;
    public MeshRenderer WideMazeScreen;
    public Camera WideMazeCamera;
    public Transform WallsParent;
    public GameObject WallTemplate;
    public GameObject Camera;
    public Material[] Materials;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _chosenVolume;
    private bool _moduleSolved;
    List<Vector3Int> _chosenNet;
    private Vector3 _currentRotation;
    private Vector3Int _currentPosition;
    private Coroutine _moveRoutine = null;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        BrownButtonSelectable.OnInteract += BrownButtonPress;
        BrownButtonSelectable.OnInteractEnded += BrownButtonRelease;
        WideMazeCamera.targetTexture = WideMazeScreen.material.mainTexture as RenderTexture;

        _chosenNet = CubeNets.AllNets.PickRandom();
        _chosenVolume = Rnd.Range(0, 8);
        Debug.LogFormat("[The Brown Button #{0}] Chose net: {1}", _moduleId, _chosenNet.Select(v => v.ToString()).Join(", "));
        Debug.LogFormat("[The Brown Button #{0}] Chose colored cell {1}.", _moduleId, _chosenNet[_chosenVolume].ToString());

        Vector3Int[] changes = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        for(int vix = 0; vix < _chosenNet.Count; vix++)
        {
            Vector3Int v = _chosenNet[vix];
            foreach(Vector3Int c in changes)
                if(!_chosenNet.Any(t => t == v + c))
                    AddWall(v, c, vix == _chosenVolume ? 1 : 0);
        }

        _currentPosition = _chosenNet.PickRandom();

        GetComponentInChildren<CameraScript>().UpdateChildren();

        StartCoroutine(RotateCamera());
    }

    private const float DELAY_A = 0.5f;
    private const float DELAY_B = 1f;

    private IEnumerator RotateCamera()
    {
        Vector3Int[] dirs = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        while(true)
        {
            foreach(Vector3Int dir in dirs)
            {
                _currentRotation = dir;
                Vector3 rot = DirectionToEuler(dir);
                Quaternion start = Camera.transform.localRotation;
                Quaternion target = Quaternion.Euler(rot.x, rot.y, rot.z);
                float startTime = Time.time;
                float progress = 0f;
                while(startTime + DELAY_A > Time.time)
                {
                    yield return null;
                    progress += Time.deltaTime;
                    Camera.transform.localRotation = Quaternion.Lerp(start, target, progress / DELAY_A);
                }
                Camera.transform.localRotation = target;
                yield return new WaitForSeconds(DELAY_B);
            }
            yield return null;
        }
    }

    private IEnumerator MoveMaze()
    {
        Vector3 start = WallsParent.localPosition;
        Vector3 end = new Vector3(_currentPosition.x, _currentPosition.y, _currentPosition.z) * -0.1f - _currentRotation * 0.1f;
        float startTime = Time.time;
        float progress = 0f;
        while(startTime + DELAY_A > Time.time)
        {
            yield return null;
            progress += Time.deltaTime;
            WallsParent.localPosition = Vector3.Lerp(start, end, progress / DELAY_A);
        }
        WallsParent.localPosition = end;
    }

    private void AddWall(Vector3Int position, Vector3Int direction, int c)
    {
        GameObject go = Instantiate(WallTemplate, WallsParent);
        go.transform.localPosition = position;
        Vector3 rot = DirectionToEuler(direction);
        go.transform.localEulerAngles = rot;
        go.transform.localScale = new Vector3(1f, 1f, 1f);
        go.GetComponentInChildren<CustomMaterialInfo>().Color = Materials[c];
    }

    private Vector3 DirectionToEuler(Vector3Int direction)
    {
        Vector3 rot = new Vector3(0f, 0f, 0f);
        if(direction.x == 1)
            rot = new Vector3(0f, 0f, 90f);
        if(direction.x == -1)
            rot = new Vector3(0f, 0f, -90f);
        if(direction.y == 1)
            rot = new Vector3(180f, 0f, 0f);
        if(direction.y == -1)
            rot = new Vector3(0f, 0f, 0f);
        if(direction.z == 1)
            rot = new Vector3(-90f, 0f, 0f);
        if(direction.z == -1)
            rot = new Vector3(90f, 0f, 0f);
        return rot;
    }

    private bool BrownButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if(!_moduleSolved)
        {
            if(_chosenNet.Any(t => t == _currentPosition + _currentRotation))
            {
                if(_moveRoutine != null)
                    StopCoroutine(_moveRoutine);
                _moveRoutine  = StartCoroutine(MoveMaze());
                _currentPosition += new Vector3Int((int)_currentRotation.x, (int)_currentRotation.y, (int)_currentRotation.z);
            }
        }
        return false;
    }

    private void BrownButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while(elapsed < duration)
        {
            BrownButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        BrownButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 1 5 [hold on 1, release on 5] | !{0} tap";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if(!_moduleSolved)
            yield break;
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
