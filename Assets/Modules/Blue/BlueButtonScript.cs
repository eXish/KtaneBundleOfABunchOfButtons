using System;
using System.Collections;
using System.Linq;
using BlueButtonLib;
using JetBrains.Annotations;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class BlueButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;

    public Transform PolyominoesParent;

    // Objects for instantiating
    public Mesh PolyominoCubelet;
    public Material[] PolyominoColors;

    // Puzzle
    private PolyominoPlacement[] _polyominoes;
    private int[] _polyominoColors;
    private int[] _colorStageColors;
    private int[] _equationOffsets;
    private int[] _suitsGoal;
    private string _word;

    // Internals
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Coroutine _pressHandler;
    private Stage _stage;

    enum Stage
    {
        Polyominoes,
        Colors,
        Equations,
        Suits,
        Word,
        Reset,
        Solved
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += BlueButtonPress;
        ButtonSelectable.OnInteractEnded += BlueButtonRelease;

        GeneratePuzzle();
        _stage = Stage.Polyominoes;

        StartCoroutine(AnimationManager(Stage.Polyominoes, PolyominoesParent, AnimatePolyominoSequence));
    }

    private IEnumerator AnimationManager(Stage requiredStage, Transform parent, Func<IEnumerator> gen)
    {
        while (true)
        {
            while (_stage != requiredStage)
                yield return null;

            var coroutine = StartCoroutine(gen());
            parent.localPosition = new Vector3(0, 0, -.2f);
            yield return new WaitForSeconds(2f);
            yield return Animation(.63f, t => parent.localPosition = new Vector3(0, 0, Easing.BackOut(t, -.2f, 0, 1)));

            while (_stage == requiredStage)
                yield return null;

            yield return Animation(.63f, t => parent.localPosition = new Vector3(0, 0, Easing.BackIn(t, 0, .2f, 1)));
            StopCoroutine(coroutine);
        }
    }

    private IEnumerator Animation(float duration, Action<float> action)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            action(elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        action(1);
    }

    private IEnumerator AnimatePolyominoSequence()
    {
        var subparent = MakeGameObject("Polyominoes scroller", PolyominoesParent, scale: .025f);
        var width = 0f;
        var numCopies = 0;

        while (width < 20 || numCopies < 2)
        {
            for (int i = 0; i < _polyominoes.Length; i++)
            {
                Debug.LogFormat("Polyomino #{0}:\n{1}", i + 1, _polyominoes[i].Polyomino);
                var polyParent = MakeGameObject(string.Format("Polyomino #{0}", i + 1), subparent.transform);

                var h = 0;
                foreach (var block in _polyominoes[i].Polyomino.Cells)
                {
                    h = Math.Max(block.Y, h);
                    var blockObj = MakeGameObject(string.Format("Block {0}", block), polyParent.transform, position: new Vector3(block.X, 0, -block.Y));
                    blockObj.AddComponent<MeshFilter>().sharedMesh = PolyominoCubelet;
                    blockObj.AddComponent<MeshRenderer>().sharedMaterial = PolyominoColors[_polyominoColors[i]];
                }

                polyParent.transform.localPosition = new Vector3(width, -1, h * .5f);
                width += _polyominoes[i].Polyomino.Cells.Max(cell => cell.X) + 2.5f;
            }
            numCopies++;
        }

        while (true)
        {
            subparent.transform.localPosition = new Vector3(((-2 * Time.time) % (width / numCopies)) * .025f - 0.15f, 0, 0);
            yield return null;
        }
    }

    private GameObject MakeGameObject(string name, Transform parent, float scale, Vector3? position = null, Quaternion? rotation = null)
    {
        return MakeGameObject(name, parent, position, rotation, scale: new Vector3(scale, scale, scale));
    }
    private GameObject MakeGameObject(string name, Transform parent, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        var obj = new GameObject(name);
        obj.transform.parent = parent;
        obj.transform.localPosition = position ?? new Vector3(0, 0, 0);
        obj.transform.localRotation = rotation ?? Quaternion.identity;
        obj.transform.localScale = scale ?? new Vector3(1, 1, 1);
        return obj;
    }

    private void GeneratePuzzle()
    {
        var puzzle = BlueButtonPuzzle.GeneratePuzzle(Rnd.Range(0, int.MaxValue));
        _polyominoes = puzzle.Polyominoes;
        _polyominoColors = puzzle.PolyominoColors;
        _colorStageColors = puzzle.ColorStageColors;
        _equationOffsets = puzzle.EquationOffsets;
        _suitsGoal = puzzle.Suits;
        _word = puzzle.Word;
    }

    private bool BlueButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_stage == Stage.Solved)
            return false;
        _pressHandler = StartCoroutine(HandlePress());
        return false;
    }

    private void BlueButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (_pressHandler != null)
            StopCoroutine(_pressHandler);

        switch (_stage)
        {
            case Stage.Polyominoes:
                _stage = Stage.Colors;
                break;

            case Stage.Colors:
                break;
            case Stage.Equations:
                break;
            case Stage.Suits:
                break;
            case Stage.Word:
                break;

            case Stage.Reset:
                GeneratePuzzle();
                _stage = Stage.Polyominoes;
                break;
        }
    }

    private IEnumerator HandlePress()
    {
        yield return new WaitForSeconds(.5f);
        Audio.PlaySoundAtTransform("BlueButtonSwoosh", transform);
        _stage = Stage.Reset;
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

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 1 5 [hold on 1, release on 5] | !{0} tap";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield break;
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
