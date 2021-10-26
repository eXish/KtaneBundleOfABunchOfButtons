using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueButtonLib;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NavyButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;

    public Transform GreekLetterParent;
    public Transform NumbersParent;
    public Transform ShapesParent;
    public Transform WordsParent;
    public Transform ResetParent;

    // Objects for instantiating/animating
    public MaskShaderManager MaskShaderManager;
    public MeshRenderer Mask;
    public Mesh[] GreekLetters;
    public TextMesh NumberTemplate;
    public TextMesh[] WordTexts;
    public TextMesh WordResultText;
    public TextMesh ResetText;
    public Mesh[] Shapes;
    public Color[] ShapeColors;
    public Light ShapesSpotlight;

    // Solving process
    private NavyButtonPuzzle _puzzle;
    private Stage _stage;
    private int _numTaps;
    private Coroutine _numTapTimeout;
    private int _shapeHighlight;
    private int _wordHighlight;
    private int _wordSection;
    private int _wordProgress;

    // Internals
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Coroutine _pressHandler;
    private MaskMaterials _maskMaterials;

    private static readonly float[] _widths =/*widths-start*/new[] { 8.988f, 7.296f, 6.636f, 7.224f, 7.116f, 6.96f, 9.156f, 8.124f, 4.14f, 8.472f, 8.376f, 10.752f, 8.916f, 6.804f, 8.124f, 9.036f, 6.96f, 6.84f, 7.596f, 8.316f, 9.48f, 8.544f, 11.16f, 8.64f, 7.104f, 6.276f, 6.72f, 6.228f, 5.532f, 5.64f, 6.192f, 6.228f, 3.492f, 7.428f, 6.804f, 6.696f, 6.756f, 5.256f, 6.228f, 7.296f, 6.216f, 6.996f, 5.7f, 6.72f, 7.932f, 7.044f, 7.74f, 8.472f }/*widths-end*/;

    private static readonly string[] _colorNames = { "red", "yellow", "blue" };
    private static readonly string[] _shapeNames = { "sphere", "cube", "cone", "prism", "cylinder", "pyramid", "torus" };

    enum Stage
    {
        GreekLetters,
        Numbers,
        Shapes,
        Word,
        Reset,
        Solved
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;

        _maskMaterials = MaskShaderManager.MakeMaterials();
        _maskMaterials.Text.mainTexture = WordResultText.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
        _maskMaterials.DiffuseText.mainTexture = WordResultText.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
        Mask.sharedMaterial = _maskMaterials.Mask;

        GeneratePuzzle();
        _stage = Stage.GreekLetters;

        StartCoroutine(AnimationManager(Stage.GreekLetters, GreekLetterParent, AnimateGreekLetters));
        StartCoroutine(AnimationManager(Stage.Numbers, NumbersParent, AnimateNumbers));
        StartCoroutine(AnimationManager(Stage.Shapes, ShapesParent, AnimateShapes, ShapesSpotlight));
        StartCoroutine(AnimationManager(new[] { Stage.Word, Stage.Solved }, WordsParent, AnimateWordsAndSolve));
        StartCoroutine(AnimationManager(Stage.Reset, ResetParent, AnimateReset));
    }

    private void GeneratePuzzle()
    {
        var seed = Rnd.Range(0, int.MaxValue);
        Debug.LogFormat("<The Navy Button #{0}> Seed: {1}", _moduleId, seed);
        _puzzle = NavyButtonPuzzle.GeneratePuzzle(seed);

        Debug.LogFormat(@"[The Navy Button #{0}] Stage 1: Greek letters are: {1}", _moduleId, _puzzle.GreekLetterIxs.Select(ix => "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρστυφχψω"[ix]).Join(" "));
        Debug.LogFormat(@"<The Navy Button #{0}> Stage 1: Coordinates: {1}", _moduleId, Enumerable.Range(0, _puzzle.XCoordinates.Length).Select(ix => string.Format("({0}, {1})", _puzzle.XCoordinates[ix], _puzzle.YCoordinates[ix])).Join(", "));
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 1: Square distances are: {1}", _moduleId, _puzzle.SquaredDistances.Join(", "));

        Debug.LogFormat(@"[The Navy Button #{0}] Stage 2: Numbers shown: {1}", _moduleId, _puzzle.Numbers.Join(", "));
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 2: Differences: {1}", _moduleId, Enumerable.Range(0, _puzzle.Numbers.Length).Select(ix => _puzzle.Numbers[ix] - _puzzle.SquaredDistances[ix % _puzzle.SquaredDistances.Length]).Join(", "));
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 2: Given is at col {1}, row {2}, value {3}", _moduleId, _puzzle.GivenIndex % 4, _puzzle.GivenIndex / 4, _puzzle.GivenValue);
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 2: Tap the button {1} times.", _moduleId, _puzzle.TapsRequired);

        var rawGrid = @". A . B . C .;M   N   O   P;. D . E . F .;Q   R   S   T;. G . H . I .;U   V   W   X;. J . K . L .".Replace(";", "\n")
            .Select(ch => ch >= 'A' && ch <= 'X' ? _puzzle.GreekLetterIxs.Contains(ch - 'A') ? (ch <= 'L' ? '>' : '∨') : _puzzle.GreekLetterIxs.Contains(ch - 'A' + 24) ? (ch <= 'L' ? '<' : '∧') : ' ' : ch).Join("");
        var givenIxStr = 4 * (_puzzle.GivenIndex % 4) + 28 * (_puzzle.GivenIndex / 4);
        var grid = rawGrid.Substring(0, givenIxStr) + _puzzle.GivenValue.ToString() + rawGrid.Substring(givenIxStr + 1);
        Debug.LogFormat("[The Navy Button #{0}] Puzzle grid:\n{1}", _moduleId, grid);

        for (var cell = 0; cell < 16; cell++)
        {
            givenIxStr = 4 * (cell % 4) + 28 * (cell / 4);
            grid = grid.Substring(0, givenIxStr) + _puzzle.LatinSquare[cell].ToString() + grid.Substring(givenIxStr + 1);
        }
        Debug.LogFormat("[The Navy Button #{0}] Puzzle solution:\n{1}", _moduleId, grid);

        Debug.LogFormat(@"<The Navy Button #{0}> Stage 3: Stencil indexes: {1}", _moduleId, _puzzle.StencilIxs.Join(", "));
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 3: Shapes are: {1}", _moduleId,
            Enumerable.Range(0, 5).Select(ix => string.Format("{0} {1}", _colorNames[_puzzle.StencilIxs[ix] % 3], _shapeNames[_puzzle.StencilIxs[ix] / 3])).Join(", "));
        Debug.LogFormat(@"[The Navy Button #{0}] Stage 3: Decoy shape: {1} {2}", _moduleId, _colorNames[_puzzle.StencilIxs[4] % 3], _shapeNames[_puzzle.StencilIxs[4] / 3]);

        Debug.LogFormat(@"[The Navy Button #{0}] Stage 4: Answer is {1}", _moduleId, _puzzle.Answer);
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_numTapTimeout != null)
            StopCoroutine(_numTapTimeout);
        if (_stage != Stage.Solved)
            _pressHandler = StartCoroutine(HandlePress());
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (_pressHandler != null)
            StopCoroutine(_pressHandler);

        switch (_stage)
        {
            case Stage.GreekLetters:
                _stage = Stage.Numbers;
                break;

            case Stage.Numbers:
                if (_numTapTimeout != null)
                    StopCoroutine(_numTapTimeout);
                _numTaps++;
                _numTapTimeout = StartCoroutine(NumberTimeout());
                break;

            case Stage.Shapes:
                if (_shapeHighlight != 4)
                {
                    Debug.LogFormat(@"[The Navy Button #{0}] Stage 3: You submitted the {1} {2}. Strike!", _moduleId, _colorNames[_puzzle.StencilIxs[_shapeHighlight] % 3], _shapeNames[_puzzle.StencilIxs[_shapeHighlight] / 3]);
                    Debug.LogFormat(@"<The Navy Button #{0}> Stage 3: You submitted #{1} ({2}). Strike!", _moduleId, _shapeHighlight, _puzzle.StencilIxs[_shapeHighlight]);
                    Module.HandleStrike();
                }
                else
                    _stage = Stage.Word;
                break;

            case Stage.Word:
                if (_wordSection == 0)
                {
                    if (_wordHighlight != (_puzzle.Answer[_wordProgress] - 'A') / 9)
                    {
                        Debug.LogFormat(@"[The Navy Button #{0}] Stage 4: You selected section {1} for letter #{2}. Strike!", _moduleId, WordTexts[_wordHighlight].text, _wordProgress + 1);
                        Module.HandleStrike();
                    }
                    else
                        _wordSection = _wordHighlight + 1;
                }
                else if (_wordSection <= 3)
                {
                    if (_wordHighlight != ((_puzzle.Answer[_wordProgress] - 'A') % 9) / 3)
                    {
                        Debug.LogFormat(@"[The Navy Button #{0}] Stage 4: You selected section {1} for letter #{2}. Strike!", _moduleId, WordTexts[_wordHighlight].text, _wordProgress + 1);
                        Module.HandleStrike();
                        _wordSection = 0;
                    }
                    else
                        _wordSection = (_wordSection - 1) * 3 + _wordHighlight + 4;
                }
                else if (_wordSection + _wordHighlight >= 30)
                {
                    Debug.LogFormat(@"[The Navy Button #{0}] Stage 4: You submitted the empty slot after Z. Strike!", _moduleId);
                    Module.HandleStrike();
                    _wordSection = 0;
                }
                else
                {
                    var nextLetter = (char) ('A' + ((_wordSection - 4) * 3 + _wordHighlight));
                    if (nextLetter != _puzzle.Answer[_wordProgress])
                    {
                        Debug.LogFormat(@"[The Navy Button #{0}] Stage 4: You submitted {1} for letter #{2}. Strike!", _moduleId, nextLetter, _wordProgress + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        _wordProgress++;
                        if (_wordProgress == _puzzle.Answer.Length)
                        {
                            Module.HandlePass();
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                            _stage = Stage.Solved;
                        }
                    }
                    _wordSection = 0;
                }
                break;

            case Stage.Reset:
                _stage = Stage.GreekLetters;
                _wordProgress = 0;
                break;
        }
    }

    private IEnumerator HandlePress()
    {
        yield return new WaitForSeconds(.5f);
        Audio.PlaySoundAtTransform("BlueButtonSwoosh", transform);
        _stage = Stage.Reset;
    }

    private IEnumerator AnimationManager(Stage requiredStage, Transform parent, Func<Func<bool>, IEnumerator> gen, Light spotlight = null)
    {
        return AnimationManager(new[] { requiredStage }, parent, gen, spotlight);
    }

    private IEnumerator AnimationManager(Stage[] requiredStages, Transform parent, Func<Func<bool>, IEnumerator> gen, Light spotlight = null)
    {
        while (true)
        {
            parent.gameObject.SetActive(false);
            if (spotlight != null)
                spotlight.gameObject.SetActive(false);

            while (!requiredStages.Contains(_stage))
            {
                if (_stage == Stage.Solved)
                    yield break;
                yield return null;
            }

            var stop = false;
            StartCoroutine(gen(() => stop));
            yield return new WaitForSeconds(1f);
            parent.localPosition = new Vector3(0, 0, -.2f);
            parent.gameObject.SetActive(true);
            if (spotlight != null)
            {
                spotlight.intensity = 0;
                spotlight.gameObject.SetActive(true);
            }
            yield return Animation(.63f, t =>
            {
                parent.localPosition = new Vector3(0, 0, Easing.BackOut(t, -.2f, 0, 1));
                if (spotlight != null)
                    spotlight.intensity = 5 * t;
            });

            while (requiredStages.Contains(_stage))
            {
                if (_stage == Stage.Solved)
                    yield break;
                yield return null;
            }

            yield return Animation(.63f, t =>
            {
                parent.localPosition = new Vector3(0, 0, Easing.BackIn(t, 0, .2f, 1));
                if (spotlight != null)
                    spotlight.intensity = 5 * (1 - t);
            });

            stop = true;
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

    private IEnumerator AnimateGreekLetters(Func<bool> stop)
    {
        var scroller = MakeGameObject("Greek Letters scroller", GreekLetterParent, scale: .025f);
        var width = 0f;
        var numCopies = 0;

        while (width < 24 || numCopies < 2)
        {
            for (int i = 0; i < _puzzle.GreekLetterIxs.Length; i++)
            {
                var letterObj = MakeGameObject(string.Format("Letter {0}", i + 1), scroller.transform, position: new Vector3(width, 0, -1.5f), scale: new Vector3(-.35f, .35f, -.35f));
                letterObj.AddComponent<MeshFilter>().sharedMesh = GreekLetters[_puzzle.GreekLetterIxs[i]];
                var mr = letterObj.AddComponent<MeshRenderer>();
                mr.material = _maskMaterials.DiffuseTint;
                mr.material.color = new Color32(0x81, 0xb6, 0xff, 0xff);
                width += _widths[_puzzle.GreekLetterIxs[i]] * .35f + 2f;
            }
            numCopies++;
        }
        width /= numCopies;

        while (!stop())
        {
            scroller.transform.localPosition = new Vector3(-((4f * Time.time) % width) * .025f - 0.15f, -0.025f, 0);
            yield return null;
        }

        Destroy(scroller);
    }

    private IEnumerator AnimateNumbers(Func<bool> stop)
    {
        var scroller = MakeGameObject("Numbers scroller", NumbersParent, scale: .025f);
        var width = 0f;
        var numCopies = 0;

        while (width < 24 || numCopies < 2)
        {
            for (int i = 0; i < _puzzle.Numbers.Length; i++)
            {
                var equation = Instantiate(NumberTemplate, scroller.transform);
                equation.GetComponent<MeshRenderer>().sharedMaterial = _maskMaterials.DiffuseText;
                equation.name = string.Format("Number #{0}", i + 1);
                equation.transform.localPosition = new Vector3(width, 0, 0);
                equation.transform.localEulerAngles = new Vector3(90, 0, 0);
                equation.transform.localScale = new Vector3(1, 1, 1);
                equation.gameObject.SetActive(true);
                equation.text = _puzzle.Numbers[i].ToString();
                width += 1.5f * equation.text.Length + 2f;
            }
            numCopies++;
        }
        width /= numCopies;

        while (!stop())
        {
            scroller.transform.localPosition = new Vector3(-((6f * Time.time) % width) * .025f - 0.15f, -0.025f, 0);
            yield return null;
        }

        Destroy(scroller);
    }

    private T[] newArray<T>(params T[] array) { return array; }

    private Func<float, Quaternion> GetRandomAxisRotator()
    {
        var rv1 = Rnd.Range(0f, 360f);
        var rv2 = Rnd.Range(0f, 360f);
        switch (Rnd.Range(0, 3))
        {
            case 0: return v => Quaternion.Euler(v, rv1, rv2);
            case 1: return v => Quaternion.Euler(rv1, v, rv2);
            default: return v => Quaternion.Euler(rv1, rv2, v);
        }
    }

    private IEnumerator AnimateShapes(Func<bool> stop)
    {
        var scroller = MakeGameObject("Shapes scroller", ShapesParent);
        var width = 0f;
        var numCopies = 0;
        const float separation = .1f;
        const float spotlightDistance = 1f / 208 * 190;

        var axesRotators = newArray(GetRandomAxisRotator(), GetRandomAxisRotator(), GetRandomAxisRotator(), GetRandomAxisRotator(), GetRandomAxisRotator());
        var shapeObjs = new List<Transform>();

        while (width < .6f || numCopies < 2)
        {
            for (int i = 0; i < 5; i++)
            {
                var shapeObj = MakeGameObject(string.Format("Shape {0}", i + 1), scroller.transform, position: new Vector3(width, .01625f, 0), scale: new Vector3(.04f, .04f, .04f));
                shapeObj.AddComponent<MeshFilter>().sharedMesh = Shapes[_puzzle.StencilIxs[i] / 3];
                var mr = shapeObj.AddComponent<MeshRenderer>();
                mr.material = _maskMaterials.DiffuseTint;
                mr.material.color = ShapeColors[_puzzle.StencilIxs[i] % 3];
                width += separation;
                shapeObjs.Add(shapeObj.transform);
            }
            numCopies++;
        }
        width /= numCopies;

        while (!stop())
        {
            scroller.transform.localPosition = new Vector3(-((.1f * Time.time) % width) - .15f, -.025f, 0);

            var pos = (((.1f * Time.time) % width) + .15f) / separation;
            var selected = Mathf.RoundToInt(pos);

            // Generated from Maple code; see Blue Button
            var t = pos - selected;
            const float q = -.4f, r = -.3f, C2 = 1744.129529f, a = 5652.886846f, C5 = 430.6776816f, C1 = -2778.179948f, C4 = -473.7842137f;
            var calcAngle =
                t < q ? -.5f * a * Mathf.Pow(t, 2) + C1 * t + C4 :
                t < r ? .5f * a * Mathf.Pow(t, 2) + C2 * t + C5 :
                180 + Mathf.Atan2(t, spotlightDistance) * 180 / Mathf.PI;

            ShapesSpotlight.transform.localEulerAngles = new Vector3(40, calcAngle, 0);
            _shapeHighlight = selected % 5;

            var axisAngle = (90f * Time.time) % 360;
            var angle = (120f * Time.time) % 360;

            for (var i = 0; i < shapeObjs.Count; i++)
                shapeObjs[i].localRotation = Quaternion.AngleAxis(angle, axesRotators[i % 5](axisAngle) * Vector3.up);

            yield return null;
        }

        Destroy(scroller);
    }

    private IEnumerator AnimateWordsAndSolve(Func<bool> stop)
    {
        WordResultText.GetComponent<MeshRenderer>().sharedMaterial = _maskMaterials.DiffuseText;
        for (var i = 0; i < 3; i++)
            WordTexts[i].GetComponent<MeshRenderer>().sharedMaterial = _maskMaterials.DiffuseText;

        while (!stop())
        {
            if (_stage == Stage.Solved)
            {
                WordResultText.text = _puzzle.Answer;

                for (var i = 0; i < 3; i++)
                    WordTexts[i].gameObject.SetActive(false);

                yield return Animation(2.6f, t =>
                {
                    WordResultText.transform.localPosition = Vector3.Lerp(new Vector3(0, -.02f, -.03f), new Vector3(0, 0, 0), Easing.InOutQuad(t, 0, 1, 1));
                    WordResultText.transform.localScale = Vector3.Lerp(new Vector3(.015f, .015f, .015f), new Vector3(.025f, .025f, .025f), Easing.InOutQuad(t, 0, 1, 1));
                    WordResultText.color = Color.Lerp(new Color32(0xE1, 0xE1, 0xE1, 0xFF), new Color32(0x0D, 0xE1, 0x0F, 0xFF), Easing.InOutQuad(t, 0, 1, 1));
                });
                yield break;
            }
            else
            {
                _wordHighlight = (int) ((Time.time % 1.8f) / 1.8f * 3);
                for (var i = 0; i < 3; i++)
                {
                    WordTexts[i].gameObject.SetActive(true);
                    WordTexts[i].color = i == _wordHighlight ? Color.white : (Color) new Color32(0x3D, 0x69, 0xC7, 0xFF);
                }
                WordResultText.text = _puzzle.Answer.Substring(0, _wordProgress) + "_";

                if (_wordSection == 0)
                {
                    WordTexts[0].text = "A–I";
                    WordTexts[1].text = "J–R";
                    WordTexts[2].text = "S–Z";
                }
                else if (_wordSection <= 3)
                {
                    for (var triplet = 0; triplet < 3; triplet++)
                        WordTexts[triplet].text = _wordSection == 3 && triplet == 2 ? "YZ" : Enumerable.Range(0, 3).Select(ltr => (char) ('A' + (_wordSection - 1) * 9 + 3 * triplet + ltr)).Join("");
                }
                else
                {
                    for (var ltr = 0; ltr < 3; ltr++)
                        WordTexts[ltr].text = _wordSection == 12 && ltr == 2 ? "" : ((char) ('A' + ((_wordSection - 4) * 3 + ltr))).ToString();
                }

                yield return null;
            }
        }

        for (var i = 0; i < 3; i++)
            WordTexts[i].gameObject.SetActive(false);
    }

    private IEnumerator AnimateReset(Func<bool> stop)
    {
        ResetText.GetComponent<MeshRenderer>().sharedMaterial = _maskMaterials.DiffuseText;
        ResetText.gameObject.SetActive(true);
        while (!stop())
            yield return null;
        ResetText.gameObject.SetActive(false);
    }

    private IEnumerator NumberTimeout()
    {
        yield return new WaitForSeconds(1.5f);
        if (_numTaps != _puzzle.TapsRequired)
        {
            Debug.LogFormat(@"[The Navy Button #{0}] Stage 3: You tapped the button {1} times instead of {2}. Strike!", _moduleId, _numTaps, _puzzle.TapsRequired);
            Module.HandleStrike();
        }
        else
            _stage = Stage.Shapes;
        _numTaps = 0;
    }
}
