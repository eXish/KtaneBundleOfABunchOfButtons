using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueButtonLib;
using SingleSelectablePack;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class YellowButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public Transform ArrowRotator;
    public MeshRenderer[] ColorSquares;
    public Material[] Colors;
    public MeshRenderer[] Segments;
    public Material SegmentOff;
    public Material SegmentOn;
    public Material SegmentOnHighlighted;
    public Transform Symbol;
    public Transform Indicator;
    public FakeStatusLight FakeStatusLight;

    private static readonly Dictionary<int, int[][]> _ruleSeededColorGrids = new Dictionary<int, int[][]>();
    public static readonly string[] _colorNames = { "red", "yellow", "green", "cyan", "blue", "pink" };
    public static readonly string[] _directionNames = { "up", "up-right", "right", "down-right", "down", "down-left", "left", "up-left" };

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private int[] _solution;
    private int _solutionProgress;
    private int _curDirection;
    private bool _curDirectionHighlighted;
    private bool _allowedToPress = true;

    private const int GridSize = 6;
    private const int SnakeLength = 8;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;

        // Rule seed
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Yellow Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);
        var colorGrids = _ruleSeededColorGrids.ContainsKey(rnd.Seed)
            ? _ruleSeededColorGrids[rnd.Seed]
            : (_ruleSeededColorGrids[rnd.Seed] = GenerateColorGrids(rnd));

        // Generate puzzle
        var whichColorGrid = Rnd.Range(0, 4);
        var numIterations = 0;
        tryAgain:
        numIterations++;

        var inf = FindSnakyPath(new Coord[SnakeLength], 0, SnakeLength).FirstOrDefault();
        if (FindSolutions(new Coord[SnakeLength], 0, colorGrids[whichColorGrid], inf.Squares, SnakeLength).Skip(1).Any())
            goto tryAgain;

        _solution = inf.Directions;

        for (var i = 0; i < ColorSquares.Length; i++)
            ColorSquares[i].sharedMaterial = Colors[colorGrids[whichColorGrid][inf.Squares[i].Index]];
        Symbol.localEulerAngles = new Vector3(90, new[] { 0, 90, 270, 180 }[whichColorGrid], 0);

        StartCoroutine(AnimateArrow());
        StartCoroutine(AnimateColorStrip());

        Debug.LogFormat(@"[The Yellow Button #{0}] Dance floor: {1}", _moduleId, "top-left,top-right,bottom-left,bottom-right".Split(',')[whichColorGrid]);
        Debug.LogFormat(@"[The Yellow Button #{0}] Hip color sequence: {1}", _moduleId, inf.Squares.Select(sq => _colorNames[colorGrids[whichColorGrid][sq.Index]]).Join(", "));
        Debug.LogFormat(@"[The Yellow Button #{0}] Starting position: {1}{2}", _moduleId, (char) ('A' + inf.Squares[0].X), inf.Squares[0].Y + 1);
        Debug.LogFormat(@"[The Yellow Button #{0}] Sick moves: {1}", _moduleId, inf.Directions.Select(dir => _directionNames[dir]).Join(", "));
        Debug.LogFormat(@"[The Yellow Button #{0}] Dance: {1}", _moduleId, inf.Squares.Select(c => (char) ('A' + c.X) + (c.Y + 1).ToString()).Join(" → "));

        FakeStatusLight = Instantiate(FakeStatusLight);
        FakeStatusLight.GetStatusLights(transform);
        FakeStatusLight.Module = Module;
    }

    private bool ButtonPress()
    {
        if (_allowedToPress)
        {
            StartCoroutine(AnimateButton(0f, -0.05f));
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        }
        if (_moduleSolved)
            return false;
        _curDirectionHighlighted = true;
        if (_curDirection != _solution[_solutionProgress])
        {
            Debug.LogFormat(@"[The Yellow Button #{0}] Stepping {1} was WRONG at position #{2}. Strike!", _moduleId, _directionNames[_curDirection], _solutionProgress + 1);
            Module.HandleStrike();
        }
        else
        {
            _solutionProgress++;
            Debug.LogFormat(@"[The Yellow Button #{0}] Stepping {1} was correct at position #{2}.", _moduleId, _directionNames[_curDirection], _solutionProgress);
            if (_solutionProgress == _solution.Length)
            {
                Debug.LogFormat(@"[The Yellow Button #{0}] Module solved.", _moduleId);
                StartCoroutine(SolveAnimation());
                _moduleSolved = true;
                Audio.PlaySoundAtTransform("YellowButtonSound8", transform);
                // Module.HandlePass() is delayed until the button release so that the TP handler can finish
            }
            else
                Audio.PlaySoundAtTransform("YellowButtonSound" + Rnd.Range(1, 8), transform);
        }
        return false;
    }

    private void ButtonRelease()
    {
        if (_allowedToPress)
        {
            StartCoroutine(AnimateButton(-0.05f, 0f));
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        }
        _curDirectionHighlighted = false;
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private IEnumerator AnimateArrow()
    {
        const float speed = 64f;
        while (!_moduleSolved)
        {
            var angle = speed * Time.time;
            ArrowRotator.localEulerAngles = new Vector3(0, angle - 22.5f, 0);
            _curDirection = ((int) angle / 45) % 8;
            for (var i = 0; i < Segments.Length; i++)
                Segments[i].sharedMaterial = i == _curDirection ? _curDirectionHighlighted ? SegmentOnHighlighted : SegmentOn : SegmentOff;
            yield return null;
        }

        const float decelDuration = 4.7f;

        var startAngle = speed * Time.time - 22.5f;
        yield return Animation(decelDuration, useElapsed: true,
            action: t => ArrowRotator.localEulerAngles = new Vector3(0, startAngle + speed * (t - .5f * Mathf.Pow(t, 2) / decelDuration), 0));
    }

    private IEnumerator AnimateColorStrip()
    {
        var prevProgress = -1;
        while (!_moduleSolved)
        {
            while (_solutionProgress == prevProgress)
                yield return null;
            var newProgress = prevProgress + 1;

            yield return Animation(.6f, t =>
            {
                for (var i = 0; i < ColorSquares.Length; i++)
                    ColorSquares[i].transform.localPosition = new Vector3(Easing.OutCubic(t,
                        -0.039f + .011f * i + (i <= prevProgress ? -.0025f : .0025f),
                        -0.039f + .011f * i + (i <= newProgress ? -.0025f : .0025f), 1), 0, 0);
                Indicator.localPosition = new Vector3(Easing.OutCubic(t, -.032f + prevProgress * .011f, -.032f + newProgress * .011f, 1), .0001f, 0);
            });
            prevProgress = newProgress;
        }

        var indicatorMaterial = Indicator.gameObject.GetComponent<MeshRenderer>().material;
        yield return Animation(1.7f, t =>
        {
            for (var i = 0; i < ColorSquares.Length; i++)
                ColorSquares[i].transform.localPosition = new Vector3(Easing.OutCubic(t, -0.039f + .011f * i + (i <= prevProgress ? -.0025f : .0025f), -0.039f + .011f * i, 1), 0, 0);
            Indicator.localPosition = new Vector3(Easing.OutCubic(t, -.032f + prevProgress * .011f, .06f, 1), .0001f, 0);
            indicatorMaterial.color = new Color(1, 1, 1, 1 - t);
        });
    }

    private IEnumerator Animation(float duration, Action<float> action, bool useElapsed = false)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            action(useElapsed ? elapsed : elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        action(useElapsed ? duration : 1);
    }

    private int[][] GenerateColorGrids(MonoRandom rnd)
    {
        var result = new int[4][];
        for (int i = 0; i < 4; i++)
            result[i] = LatinSquare.Generate(rnd, GridSize);
        return result;
    }

    struct Snake
    {
        public Coord[] Squares;
        public int[] Directions;
    }

    private IEnumerable<Snake> FindSnakyPath(Coord[] sofar, int ix, int length)
    {
        if (ix == length)
        {
            var directions = new int[length - 1];
            for (var i = 1; i < length; i++)
                directions[i - 1] = Enumerable.Range(0, 8).First(dir => sofar[i - 1].NeighborWrap((GridDirection) dir) == sofar[i]);
            yield return new Snake { Directions = directions, Squares = sofar.ToArray() };
            yield break;
        }

        var available = (ix == 0 ? Coord.Cells(GridSize, GridSize) : sofar[ix - 1].Neighbors.Where(neigh => !sofar.Take(ix).Contains(neigh))).ToArray();
        var offset = Rnd.Range(0, available.Length);
        for (var fAvIx = 0; fAvIx < available.Length; fAvIx++)
        {
            var avIx = (fAvIx + offset) % available.Length;
            sofar[ix] = available[avIx];
            foreach (var solution in FindSnakyPath(sofar, ix + 1, length))
                yield return solution;
        }
    }

    private IEnumerable<Coord[]> FindSolutions(Coord[] sofar, int ix, int[] colorGrid, Coord[] snake, int length)
    {
        if (ix == length)
        {
            yield return sofar.ToArray();
            yield break;
        }

        var available = (ix == 0 ? Coord.Cells(GridSize, GridSize) : sofar[ix - 1].Neighbors.Where(neigh => !sofar.Take(ix).Contains(neigh)))
            .Where(cell => colorGrid[cell.Index] == colorGrid[snake[ix].Index])
            .ToArray();
        for (var avIx = 0; avIx < available.Length; avIx++)
        {
            sofar[ix] = available[avIx];
            foreach (var solution in FindSolutions(sofar, ix + 1, colorGrid, snake, length))
                yield return solution;
        }
    }

    private IEnumerator SolveAnimation()
    {
        _allowedToPress = false;
        yield return new WaitForSeconds(1f);
        Audio.PlaySoundAtTransform("YellowButtonSolve", transform);

        var startTime = Time.time;
        var prevSegment = 0;
        var segmentMaterials = new[] { SegmentOff, SegmentOn, SegmentOnHighlighted };
        while (Time.time - startTime < 6.5f)
        {
            var segment = (int) ((Time.time - startTime) / .122f) % 8;
            if (segment == 0)
            {
                StartCoroutine(AnimateButton(0f, -0.05f));
                FakeStatusLight.SetPass();
            }
            if (segment == 2)
                FakeStatusLight.SetInActive();
            if (segment == 4)
            {
                StartCoroutine(AnimateButton(-0.05f, 0f));
                FakeStatusLight.SetStrike();
            }
            if (segment == 6)
                FakeStatusLight.SetInActive();
            if (segment != prevSegment)
            {
                foreach (var cs in ColorSquares)
                    cs.sharedMaterial = Colors[Rnd.Range(0, Colors.Length)];
                foreach (var seg in Segments)
                    seg.sharedMaterial = segmentMaterials[Rnd.Range(0, segmentMaterials.Length)];
                prevSegment = segment;
            }
            yield return null;
        }
        FakeStatusLight.SetPass();
        foreach (var seg in Segments)
            seg.sharedMaterial = SegmentOff;
        _allowedToPress = true;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} NW N SE | !{0} UL U DR";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;
        var dirs = new List<int>();
        foreach (var piece in command.Trim().ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (piece)
            {
                case "n": case "u": dirs.Add(0); break;
                case "ne": case "ur": case "ru": dirs.Add(1); break;
                case "e": case "r": dirs.Add(2); break;
                case "se": case "dr": case "rd": dirs.Add(3); break;
                case "s": case "d": dirs.Add(4); break;
                case "sw": case "dl": case "ld": dirs.Add(5); break;
                case "w": case "l": dirs.Add(6); break;
                case "nw": case "ul": case "lu": dirs.Add(7); break;
                default: yield break;
            }
        }
        foreach (var dir in dirs)
        {
            while (_curDirection != dir)
                yield return null;
            ButtonSelectable.OnInteract();
            yield return new WaitForSeconds(.1f);
            ButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleSolved)
        {
            while (_curDirection != _solution[_solutionProgress])
                yield return true;
            ButtonSelectable.OnInteract();
            yield return new WaitForSeconds(.1f);
            ButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }
}
