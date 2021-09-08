using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueButtonLib;
using SingleSelectablePack;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class TealButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMSelectable TealButtonSelectable;
    public GameObject TealButtonCap;
    public Transform ArrowRotator;
    public MeshRenderer[] ColorSquares;
    public Material[] Colors;
    public MeshRenderer[] Segments;
    public Material SegmentOn;
    public Material SegmentOff;

    private static readonly Dictionary<int, int[][]> _ruleSeededColorGrids = new Dictionary<int, int[][]>();

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private const int GridSize = 6;
    private const int SnakeLength = 8;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        TealButtonSelectable.OnInteract += TealButtonPress;
        TealButtonSelectable.OnInteractEnded += TealButtonRelease;

        var rnd = RuleSeedable.GetRNG();
        var colorGrids = _ruleSeededColorGrids.ContainsKey(rnd.Seed)
            ? _ruleSeededColorGrids[rnd.Seed]
            : (_ruleSeededColorGrids[rnd.Seed] = GenerateColorGrids(rnd));
        var whichColorGrid = Rnd.Range(0, 4);
        GeneratePuzzle(colorGrids[whichColorGrid]);
    }

    private int[][] GenerateColorGrids(MonoRandom rnd)
    {
        var result = new int[4][];
        for (int i = 0; i < 4; i++)
            result[i] = LatinSquare.Generate(rnd, GridSize);
        return result;
    }

    private bool TealButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            //code
        }
        return false;
    }

    private void TealButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            //code
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            TealButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        TealButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private IEnumerable<Coord[]> FindSnakyPath(Coord[] sofar, int ix, int length)
    {
        if (ix == length)
        {
            yield return sofar.ToArray();
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
            var cell = sofar[ix] = available[avIx];
            var color = colorGrid[cell.Index];
            foreach (var solution in FindSolutions(sofar, ix + 1, colorGrid, snake, length))
                yield return solution;
        }
    }

    private int[] GeneratePuzzle(int[] colorGrid)
    {
        var monoRnd = new MonoRandom(1);

        var numIterations = 0;
        tryAgain:
        numIterations++;

        var snake = FindSnakyPath(new Coord[SnakeLength], 0, SnakeLength).FirstOrDefault();
        if (FindSolutions(new Coord[SnakeLength], 0, colorGrid, snake, SnakeLength).Skip(1).Any())
            goto tryAgain;

        return snake.Select(sq => colorGrid[sq.Index]).ToArray();
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} NW N SE | !{0} UL U DR";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (!_moduleSolved)
            yield break;
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
