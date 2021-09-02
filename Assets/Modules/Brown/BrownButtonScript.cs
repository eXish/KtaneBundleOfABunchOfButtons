using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using BrownButton;
using Rnd = UnityEngine.Random;
using System.Collections.Generic;

// TODO: The color generation algorithm is currently bugged. It will generate mirrored colors sometimes.
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
    private int _moduleId;
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
        Debug.LogFormat("[The Brown Button #{0}] Chose net: {1}", _moduleId, _chosenNet.Select(v => v.ToString()).Join(", "));

        Vector3Int[] changes = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

        Vector3Int start = _chosenNet.PickRandom();
        Dictionary<Vector3Int, int> cubeAssignments = new Dictionary<Vector3Int, int>() { };
        List<Vector3Int> notVisited = new List<Vector3Int>(_chosenNet);
        List<Direction4D.Axis4d> taken = new List<Direction4D.Axis4d>();
        while(cubeAssignments.Count < _chosenNet.Count)
        {
            Vector3Int chosenAdjacent = notVisited.PickRandom();
            Direction4D.Axis4d chosenAxis = new Direction4D.Axis4d[] { Direction4D.Axis4d.X, Direction4D.Axis4d.Y, Direction4D.Axis4d.Z, Direction4D.Axis4d.W }.Where(a => !taken.Contains(a)).PickRandom();
            List<Vector3Int> goalDirs = changes.Where(c => _chosenNet.Contains(chosenAdjacent + c)).ToList();
            List<Func<Vector3Int, bool>> acceptable = goalDirs.Select<Vector3Int, Func<Vector3Int, bool>>(v =>
            {
                if(v.x == 1)
                    return delegate (Vector3Int c) { return c.x == chosenAdjacent.x + 2; };
                if(v.x == -1)
                    return delegate (Vector3Int c) { return c.x == chosenAdjacent.x - 2; };
                if(v.y == 1)
                    return delegate (Vector3Int c) { return c.y == chosenAdjacent.y + 2; };
                if(v.y == -1)
                    return delegate (Vector3Int c) { return c.y == chosenAdjacent.y - 2; };
                if(v.z == 1)
                    return delegate (Vector3Int c) { return c.z == chosenAdjacent.z + 2; };
                if(v.z == -1)
                    return delegate (Vector3Int c) { return c.z == chosenAdjacent.z - 2; };

                return null;
            }).ToList();
            List<Vector3Int> goalCells = notVisited.Where(c => acceptable.Any(f => f(c))).ToList();
            Debug.Log(goalCells.Join(", "));
            // Do a BFS for any of these cells.
            Queue<Vector3Int> bfsToSearch = new Queue<Vector3Int>();
            bfsToSearch.Enqueue(chosenAdjacent);
            List<Vector3Int> bfsSeen = new List<Vector3Int>();
            Vector3Int result = new Vector3Int(-100, -100, -100);
            while(bfsToSearch.Count > 0)
            {
                Vector3Int bfsCur = bfsToSearch.Dequeue();
                bfsSeen.Add(bfsCur);
                if(goalCells.Contains(bfsCur))
                {
                    result = bfsCur;
                    break;
                }
                foreach(Vector3Int change in changes)
                    if(_chosenNet.Contains(bfsCur + change) && !bfsSeen.Contains(bfsCur + change))
                        bfsToSearch.Enqueue(bfsCur + change);
            }
            if(!_chosenNet.Contains(result))
                throw new Exception();
            taken.Add(chosenAxis);
            Direction4D dir = new Direction4D(chosenAxis, Rnd.Range(0, 2) == 0);
            cubeAssignments.Add(chosenAdjacent, Axis4DToInt(dir.Axis, dir.Positive));
            cubeAssignments.Add(result, Axis4DToInt(dir.Axis, !dir.Positive));
            notVisited.Remove(chosenAdjacent);
            notVisited.Remove(result);
        }

        Debug.Log(cubeAssignments.Keys.Join(", "));
        for(int ix = 0; ix < _chosenNet.Count; ix++)
            Debug.LogFormat("[The Brown Button #{0}] Cube {1} corresponds to {2}.", _moduleId, _chosenNet[ix], cubeAssignments[_chosenNet[ix]]);

        for(int vix = 0; vix < _chosenNet.Count; vix++)
        {
            Vector3Int v = _chosenNet[vix];
            foreach(Vector3Int c in changes)
                if(!_chosenNet.Any(t => t == v + c))
                    AddWall(v, c, Materials[cubeAssignments[v] - 1]);
        }

        _currentPosition = _chosenNet.PickRandom();

        GetComponentInChildren<CameraScript>().UpdateChildren();

        StartCoroutine(RotateCamera());
    }

    private int Axis4DToInt(Direction4D.Axis4d chosenAxis, bool pos)
    {
        //   +Z
        //-X  5  +Y
        //  1   3
        //    6
        //  2   4
        //    7
        //  
        //    8
        //   +W
        if(pos)
        {
            switch(chosenAxis)
            {
                case Direction4D.Axis4d.W:
                    return 8;
                case Direction4D.Axis4d.X:
                    return 4;
                case Direction4D.Axis4d.Y:
                    return 3;
                case Direction4D.Axis4d.Z:
                    return 5;
            }
        }
        else
        {
            switch(chosenAxis)
            {
                case Direction4D.Axis4d.W:
                    return 6;
                case Direction4D.Axis4d.X:
                    return 1;
                case Direction4D.Axis4d.Y:
                    return 2;
                case Direction4D.Axis4d.Z:
                    return 7;
            }
        }
        return -1;
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

    private void AddWall(Vector3Int position, Vector3Int direction, Material m)
    {
        GameObject go = Instantiate(WallTemplate, WallsParent);
        go.transform.localPosition = position;
        Vector3 rot = DirectionToEuler(direction);
        go.transform.localEulerAngles = rot;
        go.transform.localScale = new Vector3(1f, 1f, 1f);
        go.GetComponentInChildren<CustomMaterialInfo>().Color = m;
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
                _moveRoutine = StartCoroutine(MoveMaze());
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

    private struct Layer
    {
        public int Axis;
        public bool Odd;

        public Layer(int a, bool o)
        {
            Axis = a;
            Odd = o;
        }

        public bool Contains(Vector3Int v)
        {
            if(Axis == 0)
                return Odd ^ ((Math.Abs(v.x) & 1) != 1);
            if(Axis == 1)
                return Odd ^ ((Math.Abs(v.y) & 1) != 1);
            if(Axis == 2)
                return Odd ^ ((Math.Abs(v.z) & 1) != 1);
            return false;
        }
    }

    private struct Direction4D
    {
        public Axis4d Axis;
        public bool Positive;

        public override bool Equals(object obj)
        {
            return obj is Direction4D && ((Direction4D)obj).Axis == Axis && ((Direction4D)obj).Positive == Positive;
        }

        public static bool operator ==(Direction4D a, Direction4D b) { return a.Equals(b); }
        public static bool operator !=(Direction4D a, Direction4D b) { return !a.Equals(b); }

        public Direction4D(Axis4d a, bool p)
        {
            Axis = a;
            Positive = p;
        }

        public enum Axis4d
        {
            X,
            Y,
            Z,
            W,
        }
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
