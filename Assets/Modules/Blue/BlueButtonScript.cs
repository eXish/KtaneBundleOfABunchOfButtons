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
    public Transform EquationsParent;
    public Transform SuitsParent;
    public Transform WordsParent;
    public Transform ResetParent;

    // Objects for instantiating/animating
    public Material[] DiffuseColorsMasked;
    public Mesh PolyominoCubelet;
    public Mesh ColorBlob;
    public Light ColorsSpotlight;
    public TextMesh EquationTemplate;
    public TextMesh ResetText;
    public MeshFilter[] SuitObjects;
    public MeshRenderer[] SuitRenderers;
    public MeshRenderer[] SuitSeparators;
    public Mesh[] SuitMeshes;
    public Material SuitMasked;
    public Material SuitUnmasked;
    public Material SuitHighlighted;
    public TextMesh[] WordTexts;
    public TextMesh WordResultText;

    // Puzzle
    private PolyominoPlacement[] _polyominoes;
    private int[] _polyominoColors;
    private int[] _colorStageColors;
    private int[] _equationOffsets;
    private string[] _equations;
    private int[] _suitsCurrent;
    private int[] _suitsGoal;
    private int[] _jumps;
    private string _word;

    // Solving process
    private Stage _stage;
    private int _colorHighlight;
    private int _eqTaps;
    private Coroutine _eqTapTimeout;
    private readonly float[] _suitTapTimes = new float[4];
    private int _suitTapIx;
    private int _wordHighlight;
    private int _wordSection;
    private string _wordProgress = "";

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

    public static readonly string[] _colorNames = { "Blue", "Green", "Cyan", "Red", "Magenta", "Yellow" };

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
        StartCoroutine(AnimationManager(Stage.Equations, EquationsParent, AnimateEquations));
        StartCoroutine(AnimationManager(Stage.Suits, SuitsParent, AnimateSuits));
        StartCoroutine(AnimationManager(new[] { Stage.Word, Stage.Solved }, WordsParent, AnimateWordsAndSolve));
        StartCoroutine(AnimationManager(Stage.Reset, ResetParent, AnimateReset));
    }

    private bool BlueButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_eqTapTimeout != null)
            StopCoroutine(_eqTapTimeout);
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
                if (_colorHighlight != 3)
                {
                    Debug.LogFormat(@"[The Blue Button #{0}] Stage 2: You submitted {1} (position {2}). Strike!", _moduleId, _colorNames[_colorStageColors[_colorHighlight]], _colorHighlight + 1);
                    Module.HandleStrike();
                }
                else
                {
                    _stage = Stage.Equations;
                    _eqTaps = 0;
                }
                break;

            case Stage.Equations:
                if (_eqTapTimeout != null)
                    StopCoroutine(_eqTapTimeout);
                _eqTaps++;
                _eqTapTimeout = StartCoroutine(EquationTimeout());
                break;

            case Stage.Suits:
                _suitTapTimes[_suitTapIx] = Time.time;
                _suitTapIx++;
                if (_suitTapIx == 4)
                {
                    interpretSuitInput();
                    _suitTapIx = 0;
                }
                break;

            case Stage.Word:
                if (_wordSection == 0)
                {
                    if (_wordHighlight != (_word[_wordProgress.Length] - 'A') / 9)
                    {
                        Debug.LogFormat(@"[The Blue Button #{0}] Stage 5: You selected section {1} for letter #{2}. Strike!", _moduleId, WordTexts[_wordHighlight].text, _wordProgress.Length + 1);
                        Module.HandleStrike();
                    }
                    else
                        _wordSection = _wordHighlight + 1;
                }
                else if (_wordSection <= 3)
                {
                    if (_wordHighlight != ((_word[_wordProgress.Length] - 'A') % 9) / 3)
                    {
                        Debug.LogFormat(@"[The Blue Button #{0}] Stage 5: You selected section {1} for letter #{2}. Strike!", _moduleId, WordTexts[_wordHighlight].text, _wordProgress.Length + 1);
                        Module.HandleStrike();
                        _wordSection = 0;
                    }
                    else
                        _wordSection = (_wordSection - 1) * 3 + _wordHighlight + 4;
                }
                else if (_wordSection + _wordHighlight >= 30)
                {
                    Debug.LogFormat(@"[The Blue Button #{0}] Stage 5: You submitted the empty slot after Z. Strike!", _moduleId);
                    Module.HandleStrike();
                    _wordSection = 0;
                }
                else
                {
                    var nextLetter = (char) ('A' + ((_wordSection - 4) * 3 + _wordHighlight));
                    if (nextLetter != _word[_wordProgress.Length])
                    {
                        Debug.LogFormat(@"[The Blue Button #{0}] Stage 5: You submitted {1} for letter #{2}. Strike!", _moduleId, nextLetter, _wordProgress.Length + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        _wordProgress += nextLetter;
                        if (_wordProgress.Length == _word.Length)
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
                _stage = Stage.Polyominoes;
                _wordProgress = "";
                break;
        }
    }

    private void interpretSuitInput()
    {
        var gap1 = _suitTapTimes[1] - _suitTapTimes[0];
        var gap2 = _suitTapTimes[2] - _suitTapTimes[1];
        var gap3 = _suitTapTimes[3] - _suitTapTimes[2];

        if (gap2 > gap1 && gap3 > gap1)
        {
            // Submit
            if (_suitsCurrent.SequenceEqual(_suitsGoal))
            {
                _stage = Stage.Word;
            }
            else
            {
                Debug.LogFormat(@"[The Blue Button #{0}] Stage 4: You submitted {1}. Strike!", _moduleId, _suitsCurrent.Select(suit => "♠♥♣♦"[suit]).Join(""));
                Module.HandleStrike();
            }
            return;
        }

        // Which positions to swap: { swap, swap + 1 }
        var swap = gap3 > gap1 ? 2 : gap2 > gap1 ? 1 : 0;
        var t = _suitsCurrent[swap];
        _suitsCurrent[swap] = _suitsCurrent[swap + 1];
        _suitsCurrent[swap + 1] = t;
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
                    spotlight.intensity = 10 * t;
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
                    spotlight.intensity = 10 * (1 - t);
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
            _colorHighlight = selected % _colorStageColors.Length;
            yield return null;
        }

        Destroy(scroller);
    }

    private IEnumerator AnimateEquations(Func<bool> stop)
    {
        var scroller = MakeGameObject("Equations scroller", EquationsParent, scale: .025f);
        var width = 0f;
        var numCopies = 0;

        while (numCopies < 2)
        {
            for (int i = 0; i < _equations.Length; i++)
            {
                var equation = Instantiate(EquationTemplate, scroller.transform);
                equation.name = string.Format("Equation #{0}", i + 1);
                equation.transform.localPosition = new Vector3(width, 0, 0);
                equation.transform.localEulerAngles = new Vector3(90, 0, 0);
                equation.transform.localScale = new Vector3(1, 1, 1);
                equation.gameObject.SetActive(true);
                equation.text = _equations[i];
                width += 20f;
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

    private IEnumerator AnimateSuits(Func<bool> stop)
    {
        var suitsPrev = _suitsCurrent.ToArray();
        SetSuitMeshes(suitsPrev);
        while (true)
        {
            while (!stop() && suitsPrev.SequenceEqual(_suitsCurrent))
                yield return null;
            if (stop())
                break;
            var suitsNew = _suitsCurrent.ToArray();

            var randomRotations = suitsPrev.Select(_ => Quaternion.Euler(Rnd.Range(0f, 360f), Rnd.Range(0f, 360f), Rnd.Range(0f, 360f))).ToArray();
            var newPositions = suitsPrev.Select(suit => Array.IndexOf(suitsNew, suit)).ToArray();
            for (var rIx = 0; rIx < SuitRenderers.Length; rIx++)
                SuitRenderers[rIx].sharedMaterial = rIx == newPositions[rIx] ? SuitUnmasked : SuitHighlighted;
            SuitSeparators[Enumerable.Range(0, 4).First(i => i != newPositions[i])].sharedMaterial = SuitHighlighted;

            yield return Animation(1.3f, t =>
            {
                for (var i = 0; i < 4; i++)
                    if (newPositions[i] != i)
                    {
                        //SuitObjects[i].transform.localPosition = Vector3.Lerp(SuitPos(i), SuitPos(newPositions[i]), t);
                        SuitObjects[i].transform.localPosition = new Vector3(
                            Easing.InOutQuart(t, SuitX(i), SuitX(newPositions[i]), 1),
                            .3f * t * (t - 1) * (t - 1) - 0.01f,
                            .3f * t * (t - 1) * (t - 1) * Mathf.Sign(i - newPositions[i]));
                        SuitObjects[i].transform.localRotation = Quaternion.Slerp(Quaternion.identity, randomRotations[i], t < .5 ? Easing.OutQuad(t, 0, 1, .5f) : Easing.InOutQuad(t - .5f, 1, 0, .5f));
                    }
            });

            SetSuitMeshes(suitsNew);
            suitsPrev = suitsNew;
        }
    }

    private IEnumerator AnimateWordsAndSolve(Func<bool> stop)
    {
        while (!stop())
        {
            if (_stage == Stage.Solved)
            {
                WordResultText.text = _word;

                for (var i = 0; i < 3; i++)
                    WordTexts[i].gameObject.SetActive(false);

                yield return Animation(3f, t =>
                {
                    WordResultText.transform.localPosition = Vector3.Lerp(new Vector3(0, -.02f, -.03f), new Vector3(0, 0, 0), Easing.InOutQuad(t, 0, 1, 1));
                    WordResultText.transform.localScale = Vector3.Lerp(new Vector3(.015f, .015f, .015f), new Vector3(.02f, .02f, .02f), Easing.InOutQuad(t, 0, 1, 1));
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
                    WordTexts[i].color = i == _wordHighlight ? Color.white : (Color) new Color32(0x9F, 0xB6, 0xE8, 0xFF);
                }
                WordResultText.text = _wordProgress + "﹍";

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

    private void SetSuitMeshes(int[] suits)
    {
        for (var i = 0; i < suits.Length; i++)
        {
            SuitObjects[i].transform.localPosition = new Vector3(SuitX(i), -0.01f, 0);
            SuitObjects[i].transform.localRotation = Quaternion.identity;
            SuitObjects[i].sharedMesh = SuitMeshes[4 * _jumps[Array.IndexOf(_suitsGoal, suits[i])] + suits[i]];
            SuitRenderers[i].sharedMaterial = SuitMasked;
        }
        for (var i = 0; i < SuitSeparators.Length; i++)
            SuitSeparators[i].sharedMaterial = SuitMasked;
    }

    private static float SuitX(int i)
    {
        return -0.114f + .076f * i;
    }

    private IEnumerator AnimateReset(Func<bool> stop)
    {
        ResetText.gameObject.SetActive(true);
        while (!stop())
            yield return null;
        ResetText.gameObject.SetActive(false);
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
        _equations = _equationOffsets.Select(offset => GenerateEquation(offset)).ToArray();
        _suitsGoal = puzzle.Suits;
        _jumps = puzzle.Jumps;
        _word = puzzle.Word;

        _suitsCurrent = Enumerable.Range(0, 4).ToArray().Shuffle();

        Debug.LogFormat(@"[The Blue Button #{0}] Stage 2: Color sequence is: {1}", _moduleId, _colorStageColors.Select(c => _colorNames[c]).Join(", "));
        Debug.LogFormat(@"[The Blue Button #{0}] Stage 2: Submit on {1} after the {2}.", _moduleId, _colorNames[_colorStageColors[3]], _colorStageColors.Take(3).Select(c => _colorNames[c]).Join(", "));

        var explanations = "number of colors;position of diamonds;position within key polyomino;number of taps to submit".Split(';');
        for (var i = 0; i < _equations.Length; i++)
            Debug.LogFormat(@"[The Blue Button #{0}] Stage 3: Equation #{1}: {2} (offset {3} = {4}).", _moduleId, i + 1, _equations[i], _equationOffsets[i], explanations[i]);

        Debug.LogFormat(@"[The Blue Button #{0}] Stage 4: Suits are shown as {1}. Desired order is {2}.", _moduleId, _suitsCurrent.Select(s => "♠♥♣♦"[s]).Join(""), _suitsGoal.Select(s => "♠♥♣♦"[s]).Join(""));
        Debug.LogFormat(@"[The Blue Button #{0}] Stage 5: The solution word is {1}.", _moduleId, _word);
    }

    private string GenerateEquation(int offset)
    {
        int op1, op2;
        switch (Rnd.Range(0, 3))
        {
            case 0:
                op1 = Rnd.Range(11, 90);
                op2 = Rnd.Range(11, 90);
                return string.Format("{0} + {1} = {2}", op1, op2, op1 + op2 <= offset || Rnd.Range(0, 2) != 0 ? op1 + op2 + offset : op1 + op2 - offset);

            case 1:
                op1 = Rnd.Range(11, 90);
                op2 = Rnd.Range(11, 90);
                var sm = Math.Min(op1, op2);
                var la = Math.Max(op1, op2);
                return string.Format("{0} − {1} = {2}", la, sm, la - sm <= offset || Rnd.Range(0, 2) != 0 ? la - sm + offset : la - sm - offset);

            default:
                op1 = Rnd.Range(3, 10);
                op2 = Rnd.Range(3, 10);
                return string.Format("{0} × {1} = {2}", op1, op2, op1 * op2 <= offset || Rnd.Range(0, 2) != 0 ? op1 * op2 + offset : op1 * op2 - offset);
        }
    }

    private IEnumerator EquationTimeout()
    {
        yield return new WaitForSeconds(1.5f);
        if (_eqTaps != _equationOffsets[3])
        {
            Debug.LogFormat(@"[The Blue Button #{0}] Stage 3: You tapped the button {1} times instead of {2}. Strike!", _moduleId, _eqTaps, _equationOffsets[3]);
            Module.HandleStrike();
        }
        else
        {
            _stage = Stage.Suits;
            _suitTapIx = 0;
        }
        _eqTaps = 0;
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
