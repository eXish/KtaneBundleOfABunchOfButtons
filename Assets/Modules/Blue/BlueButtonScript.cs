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
    public Transform ColorsParent;

    // Objects for instantiating
    public Material[] DiffuseColorsMasked;
    public Mesh PolyominoCubelet;
    public Mesh ColorBlob;
    public Light ColorsSpotlight;

    // Puzzle
    private PolyominoPlacement[] _polyominoes;
    private int[] _polyominoColors;
    private int[] _colorStageColors;
    private int[] _equationOffsets;
    private int[] _suitsGoal;
    private string _word;

    // Solving process
    private Stage _stage;
    private int _highlightedColor;

    // Internals
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Coroutine _pressHandler;

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
        ColorsSpotlight.range *= transform.lossyScale.x;

        GeneratePuzzle();
        _stage = Stage.Polyominoes;

        StartCoroutine(AnimationManager(Stage.Polyominoes, PolyominoesParent, AnimatePolyominoSequence));
        StartCoroutine(AnimationManager(Stage.Colors, ColorsParent, AnimateColorSequence, ColorsSpotlight));
    }

    private IEnumerator AnimationManager(Stage requiredStage, Transform parent, Func<Func<bool>, IEnumerator> gen, Light spotlight = null)
    {
        while (true)
        {
            while (_stage != requiredStage)
                yield return null;

            var stop = false;
            StartCoroutine(gen(() => stop));
            parent.localPosition = new Vector3(0, 0, -.2f);
            yield return new WaitForSeconds(1f);
            if (spotlight != null)
            {
                spotlight.intensity = 0;
                spotlight.gameObject.SetActive(true);
            }
            yield return Animation(.63f, t =>
            {
                parent.localPosition = new Vector3(0, 0, Easing.BackOut(t, -.2f, 0, 1));
                if (spotlight != null)
                    spotlight.intensity = 10 * t;
            });

            while (_stage == requiredStage)
                yield return null;

            yield return Animation(.63f, t =>
            {
                parent.localPosition = new Vector3(0, 0, Easing.BackIn(t, 0, .2f, 1));
                if (spotlight != null)
                    spotlight.intensity = 10 * (1 - t);
            });

            stop = true;
            if (spotlight != null)
                spotlight.gameObject.SetActive(false);
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

    private IEnumerator AnimatePolyominoSequence(Func<bool> stop)
    {
        var scroller = MakeGameObject("Polyominoes scroller", PolyominoesParent, scale: .025f);
        var width = 0f;
        var numCopies = 0;

        while (width < 20 || numCopies < 2)
        {
            for (int i = 0; i < _polyominoes.Length; i++)
            {
                var polyParent = MakeGameObject(string.Format("Polyomino #{0}", i + 1), scroller.transform);

                var h = 0;
                foreach (var block in _polyominoes[i].Polyomino.Cells)
                {
                    h = Math.Max(block.Y, h);
                    var blockObj = MakeGameObject(string.Format("Block {0}", block), polyParent.transform, position: new Vector3(block.X, 0, -block.Y));
                    blockObj.AddComponent<MeshFilter>().sharedMesh = PolyominoCubelet;
                    blockObj.AddComponent<MeshRenderer>().sharedMaterial = DiffuseColorsMasked[_polyominoColors[i]];
                }

                polyParent.transform.localPosition = new Vector3(width, 0, h * .5f);
                width += _polyominoes[i].Polyomino.Cells.Max(cell => cell.X) + 2.5f;
            }
            numCopies++;
        }
        width /= numCopies;

        while (!stop())
        {
            scroller.transform.localPosition = new Vector3(-((2f * Time.time) % width) * .025f - 0.15f, -0.025f, 0);
            yield return null;
        }

        Destroy(scroller);
    }

    private IEnumerator AnimateColorSequence(Func<bool> stop)
    {
        Debug.Log("Color sequence: " + _colorStageColors.Select(c => c + 1).Join(", "));
        var scroller = MakeGameObject("Colors scroller", ColorsParent);
        var width = 0f;
        var numCopies = 0;
        const float separation = .125f;
        const float spotlightDistance = 1f / 208 * 190;

        while (numCopies < 2)
        {
            for (int i = 0; i < _colorStageColors.Length; i++)
            {
                var blobObj = MakeGameObject(string.Format("Color {0}", i), scroller.transform, position: new Vector3(width, 0, 0), scale: .04f);
                blobObj.AddComponent<MeshFilter>().sharedMesh = ColorBlob;
                blobObj.AddComponent<MeshRenderer>().sharedMaterial = DiffuseColorsMasked[_colorStageColors[i]];
                width += separation;
            }
            numCopies++;
        }
        width /= numCopies;

        while (!stop())
        {
            scroller.transform.localPosition = new Vector3(-((.1f * Time.time) % width) - 0.15f, -.025f, 0);

            var pos = (((.1f * Time.time) % width) + 0.15f) / separation;
            var selected = Mathf.RoundToInt(pos);

            /*
                restart;
                v1 := t -> -a*t+C1;
                v2 := t -> a*t+C2;
                v3 := unapply(diff(arctan(t/s)*180/3.1415926535897932384626433832795+180, t), t);
                d1 := t -> -1/2*a*t^2 + C1*t + C4;
                d2 := t -> 1/2*a*t^2 + C2*t + C5;
                d3 := t -> arctan(t/s)*180/3.1415926535897932384626433832795+180;
                q := -.4;
                r := -.3;
                s := 1.0 / 208 * 190;
                solve({
                  v1(-1/2) = v3(1/2),
                  v1(q) = v2(q),
                  d1(-1/2) = d3(1/2),
                  d1(q) = d2(q),
                  d2(r) = d3(r)
                }, { C1, C2, C4, C5, a });
            */
            var t = pos - selected;
            const float q = -.4f, r = -.3f, C2 = 1744.129529f, a = 5652.886846f, C5 = 430.6776816f, C1 = -2778.179948f, C4 = -473.7842137f;
            var calcAngle =
                t < q ? -.5f * a * Mathf.Pow(t, 2) + C1 * t + C4 :
                t < r ? .5f * a * Mathf.Pow(t, 2) + C2 * t + C5 :
                180 + Mathf.Atan2(t, spotlightDistance) * 180 / Mathf.PI;

            ColorsSpotlight.transform.localEulerAngles = new Vector3(40, calcAngle, 0);
            _highlightedColor = selected % _colorStageColors.Length;
            yield return null;
        }

        Destroy(scroller);
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
