using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BrownButton;
using UnityEngine;
using Rnd = UnityEngine.Random;

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
    public TextMesh TPText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private bool _moduleActivated = false;
    List<Vector3Int> _chosenNet;
    private Vector3 _currentRotation;
    private Vector3Int _currentPosition, _correctCell;
    private Coroutine _moveRoutine = null;
    private Dictionary<Vector3Int, string> _TPLetters = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) }.ToDictionary(v => v, v => "");

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        BrownButtonSelectable.OnInteract += BrownButtonPress;
        BrownButtonSelectable.OnInteractEnded += BrownButtonRelease;
        WideMazeScreen.material.mainTexture = new RenderTexture(WideMazeScreen.material.mainTexture as RenderTexture);
        WideMazeCamera.targetTexture = WideMazeScreen.material.mainTexture as RenderTexture;

        _chosenNet = CubeNets.AllNets.PickRandom();
        Debug.LogFormat("[The Brown Button #{0}] Chose net: {1}", _moduleId, _chosenNet.Select(v => v.ToString()).Join(", "));

        Vector3Int[] changes = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

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
            bool pos;
            if(taken.Count != 0)
                pos = Rnd.Range(0, 2) == 0; // Choose any orientation for the first three axes, as a rotation will always exist
            else
            {
                // Choose the orientation for the last axis that will result in no mirroring
                Dictionary<Vector3Int, Dictionary<Vector3Int, Vector3Int>> CellToDirectionToCell = new Dictionary<Vector3Int, Dictionary<Vector3Int, Vector3Int>>();
                foreach(Vector3Int cell in _chosenNet)
                {
                    Dictionary<Vector3Int, Vector3Int> DirectionToCell = new Dictionary<Vector3Int, Vector3Int>();
                    foreach(Vector3Int d2cdir in changes)
                    {
                        if(_chosenNet.Contains(cell + d2cdir))
                            DirectionToCell.Add(d2cdir, cell + d2cdir);
                    }
                    while(DirectionToCell.Count < 6)
                    {
                        Vector3Int a = DirectionToCell.PickRandom().Key;
                        Vector3Int b;
                        try
                        {
                            b = changes.Where(c => c != a && c != new Vector3Int(-a.x, -a.y, -a.z) && _chosenNet.Contains(cell + a + c)).PickRandom();
                        }
                        catch(InvalidOperationException) { continue; }
                        if(DirectionToCell.ContainsKey(b))
                            continue;
                        DirectionToCell.Add(b, cell + a + b);
                    }

                    CellToDirectionToCell.Add(cell, DirectionToCell);
                }

                Vector3Int dirToX = CellToDirectionToCell[chosenAdjacent].FirstOrDefault(kvp => cubeAssignments[kvp.Value] == 4).Key;
                Vector3Int dirToY = CellToDirectionToCell[chosenAdjacent].FirstOrDefault(kvp => cubeAssignments[kvp.Value] == 3).Key;
                Vector3Int dirToZ = CellToDirectionToCell[chosenAdjacent].FirstOrDefault(kvp => cubeAssignments[kvp.Value] == 5).Key;
                Vector3Int dirToW = CellToDirectionToCell[chosenAdjacent].FirstOrDefault(kvp => cubeAssignments[kvp.Value] == 8).Key;

                pos = false;

                switch(chosenAxis)
                {
                    case Direction4D.Axis4d.W:
                        // DirToX is right, DirToY is up => DirToZ is front => -W
                        pos = DirectionToPositivity(dirToX) ^ DirectionToPositivity(dirToY) ^ !DirectionToPositivity(dirToZ);
                        break;
                    case Direction4D.Axis4d.X:
                        // DirToW is right, DirToY is up => DirToZ is front => X
                        pos = DirectionToPositivity(dirToW) ^ DirectionToPositivity(dirToY) ^ DirectionToPositivity(dirToZ);
                        break;
                    case Direction4D.Axis4d.Y:
                        // DirToX is right, DirToW is up => DirToZ is front => Y
                        pos = DirectionToPositivity(dirToX) ^ DirectionToPositivity(dirToW) ^ DirectionToPositivity(dirToZ);
                        break;
                    case Direction4D.Axis4d.Z:
                        // DirToX is right, DirToY is up => DirToW is front => Z
                        pos = DirectionToPositivity(dirToX) ^ DirectionToPositivity(dirToY) ^ DirectionToPositivity(dirToW);
                        break;
                }
            }
            Direction4D dir = new Direction4D(chosenAxis, pos);
            cubeAssignments.Add(chosenAdjacent, Axis4DToInt(dir.Axis, dir.Positive));
            cubeAssignments.Add(result, Axis4DToInt(dir.Axis, !dir.Positive));
            notVisited.Remove(chosenAdjacent);
            notVisited.Remove(result);
        }

        for(int ix = 0; ix < _chosenNet.Count; ix++)
            Debug.LogFormat("[The Brown Button #{0}] Cube {1} corresponds to {2}.", _moduleId, _chosenNet[ix], cubeAssignments[_chosenNet[ix]]);

        Direction4D.Axis4d submissionAxis = new Direction4D.Axis4d[] { Direction4D.Axis4d.W, Direction4D.Axis4d.X, Direction4D.Axis4d.Y, Direction4D.Axis4d.Z }.PickRandom();
        int numPos = Rnd.Range(0, 4);
        bool submissionPos = numPos >= 2;
        int viewId = 0;
        Dictionary<Vector3Int, Material> mats = _chosenNet.ToDictionary(v => v, v => Materials[0]);
        List<Direction4D.Axis4d> axesToDisplay = new Direction4D.Axis4d[] { Direction4D.Axis4d.W, Direction4D.Axis4d.X, Direction4D.Axis4d.Y, Direction4D.Axis4d.Z }.ToList().Shuffle();
        axesToDisplay.Remove(submissionAxis);
        foreach(Direction4D.Axis4d a in axesToDisplay)
        {
            int l = Axis4DToInt(a, numPos > 0);
            numPos--;
            Vector3Int key = cubeAssignments.First(kvp => kvp.Value == l).Key;
            mats[key] = Materials[l + 8 * viewId];
            viewId++;
            Debug.LogFormat("[The Brown Button #{0}] Cube {1} is displaying {2}.", _moduleId, key, mats[key].name.Substring(7));
        }

        _correctCell = cubeAssignments.First(kvp => kvp.Value == Axis4DToInt(submissionAxis, submissionPos)).Key;
        Debug.LogFormat("[The Brown Button #{0}] The correct cell to submit is {1}.", _moduleId, _correctCell);


        for(int vix = 0; vix < _chosenNet.Count; vix++)
        {
            Vector3Int[] shuf = changes.Shuffle();
            bool displayed = false;
            Vector3Int v = _chosenNet[vix];
            foreach(Vector3Int c in shuf)
            {
                if(!_chosenNet.Any(t => t == v + c))
                {
                    AddWall(v, c, displayed ? Materials[0] : mats[v]);
                    displayed = true;
                }
            }
        }

        _currentPosition = _chosenNet.PickRandom();
        _currentRotation = new Vector3Int(0, 0, 0);
        Vector3 end = new Vector3(_currentPosition.x, _currentPosition.y, _currentPosition.z) * -0.1f - _currentRotation * 0.1f;
        WallsParent.localPosition = end;

        GetComponentInChildren<CameraScript>().UpdateChildren();
        Module.OnActivate += delegate
        {
            StartCoroutine(RotateCamera());
            _moduleActivated = true;
        };
    }

    private bool DirectionToPositivity(Vector3Int dir)
    {
        if(dir.x == 1 || dir.y == 1 || dir.z == 1)
            return true;
        if(dir.x == -1 || dir.y == -1 || dir.z == -1)
            return false;
        throw new Exception();
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
                    return 2;
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
                    return 3;
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
        yield return null;
        Vector3Int[] dirs = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        if(TwitchPlaysActive)
        {
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToList().Shuffle().Join("");
            _TPLetters = dirs.ToDictionary(v => v, v => letters[Array.IndexOf(dirs, v)].ToString());
        }
        while(true)
        {
            foreach(Vector3Int dir in dirs)
            {
                _currentRotation = dir;
                TPText.text = _TPLetters[dir];
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
        if (_moduleActivated && !_moduleSolved)
        {
            if(_chosenNet.Any(t => t == _currentPosition + _currentRotation))
            {
                if(_moveRoutine != null)
                    StopCoroutine(_moveRoutine);
                _moveRoutine = StartCoroutine(MoveMaze());
                _currentPosition += new Vector3Int((int)_currentRotation.x, (int)_currentRotation.y, (int)_currentRotation.z);
            }
            else
            {
                if(_currentPosition == _correctCell)
                {
                    Debug.LogFormat("[The Brown Button #{0}] You submitted correctly. Good job!", _moduleId);
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    Module.HandlePass();
                    _moduleSolved = true;
                    StartCoroutine(FadeScreen());
                }
                else
                {
                    Debug.LogFormat("[The Brown Button #{0}] You tried to submit at {1}. That is incorrect. Strike!", _moduleId, _currentPosition);
                    Module.HandleStrike();
                }
            }
        }
        return false;
    }

    private IEnumerator FadeScreen()
    {
        float time = Time.time;
        while(Time.time - time < 5f)
        {
            WideMazeScreen.material.color = Color.Lerp(new Color(1f, 1f, 1f), new Color(0f, 0f, 0f), (Time.time - time) / 5f);
            TPText.color = WideMazeScreen.material.color;
            yield return null;
        }
        WideMazeScreen.material.color = new Color(0f, 0f, 0f);
        TPText.color = WideMazeScreen.material.color;
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

    private struct Direction4D
    {
        public Axis4d Axis;
        public bool Positive;

        public override bool Equals(object obj)
        {
            return obj is Direction4D && ((Direction4D)obj).Axis == Axis && ((Direction4D)obj).Positive == Positive;
        }

        public override int GetHashCode()
        {
            return (int)Axis + (Positive ? 4 : 0);
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
#pragma warning disable 0649
    private readonly string TwitchHelpMessage = "!{0} tap jq r [Taps while those letters are displayed, in order]";
    private bool TwitchPlaysActive;
#pragma warning restore 0414
#pragma warning restore 0649

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (!_moduleActivated || _moduleSolved)
            yield break;
        string allLetters = _TPLetters.Values.Join("");
        Match m = Regex.Match(command.Trim().ToUpperInvariant(), @"^(?:(?:TAP|PRESS|GO|MOVE|SUBMIT)\s*)?((?:[" + allLetters + @"]\s*)*)$");
        if(m.Success)
        {
            yield return null;
            string[] presses = m.Groups[1].Value.Where(c => allLetters.Contains(c)).Select(c => c.ToString()).ToArray();
            foreach(string press in presses)
            {
                while(TPText.text != press)
                    yield return null;
                BrownButtonSelectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
                BrownButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if(_moduleSolved)
            yield break;

        Vector3Int[] changes = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };

        Queue<Vector3Int> bfsToSearch = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> backtrack = new Dictionary<Vector3Int, Vector3Int>();
        bfsToSearch.Enqueue(_currentPosition);
        List<Vector3Int> bfsSeen = new List<Vector3Int>();
        Vector3Int result = new Vector3Int(-100, -100, -100);
        while(bfsToSearch.Count > 0)
        {
            Vector3Int bfsCur = bfsToSearch.Dequeue();
            bfsSeen.Add(bfsCur);
            if(bfsCur == _correctCell)
            {
                result = bfsCur;
                break;
            }
            foreach(Vector3Int change in changes)
            {
                if(_chosenNet.Contains(bfsCur + change) && !bfsSeen.Contains(bfsCur + change))
                {
                    bfsToSearch.Enqueue(bfsCur + change);
                    backtrack.Add(bfsCur + change, bfsCur);
                }
            }
        }
        if(!_chosenNet.Contains(result))
            throw new Exception();
        List<Vector3Int> path = new List<Vector3Int> { result };
        while(!path.Contains(_currentPosition))
            path.Add(backtrack[path.Last()]);
        path.Reverse();
        for(int i = 1; i < path.Count; i++)
        {
            while(_currentRotation != path[i] - _currentPosition)
                yield return true;
            BrownButtonSelectable.OnInteract();
            yield return new WaitForSeconds(0.1f);
            BrownButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        while(_chosenNet.Select(v => new Vector3(v.x, v.y, v.z)).Contains(new Vector3(_currentPosition.x, _currentPosition.y, _currentPosition.z) + _currentRotation))
            yield return true;
        BrownButtonSelectable.OnInteract();
        yield return new WaitForSeconds(0.1f);
        BrownButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(0.1f);
    }
}
