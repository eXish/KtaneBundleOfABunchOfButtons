using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class WhiteButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;
    public KMSelectable WhiteButtonSelectable;
    public GameObject WhiteButtonCap, ColorblindScreen;
    public TextMesh ColorblindText;
    public MeshRenderer[] ColorSegments, ColorBlobs, LeftLeds, RightLeds;
    public Material[] OnColors, OffColors;
    public Material OnLight, OffLight;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved, _colorblindMode;

    private bool _buttonHeld;
    private bool _longPress;
    private int _lastTimerSeconds;
    private readonly int[] _blobColors = new int[5];
    private readonly int[] _targetBlobColors = new int[5];
    private int _currentColor;
    private int _currentBlob;
    private bool _isAdding, _isStriking;
    private readonly string[] COLORNAMES = { "Iridium", "East Bay", "Cerulean", "Laurel", "Celadon", "Seaport", "Apple", "Emerald", "Pelorous", "Lotus", "Plum", "Orchid", "Sycamore", "Battleship", "Cove", "Atlantis", "Pistachio", "Neptune", "Mahogany", "Mulberry", "Amethyst", "Sienna", "Puce", "Viola", "Turmeric", "Pine", "Silver" };
    private readonly string[] CBNAMES = { "Ird", "Est", "Cer", "Lau", "Cdn", "Spt", "Apl", "Emd", "Pls", "Lot", "Plm", "Orc", "Syc", "Btl", "Cov", "Atl", "Pch", "Npt", "Mhg", "Mlb", "Amt", "Snn", "Pce", "Vio", "Trm", "Pin", "Slv" };
    private readonly string[] POSITIONS = { "first", "second", "third", "fourth", "fifth" };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        SetColorblindMode(ColorblindMode.ColorblindModeActive);

        WhiteButtonSelectable.OnInteract += WhiteButtonPress;
        WhiteButtonSelectable.OnInteractEnded += WhiteButtonRelease;

        var snDigits = BombInfo.GetSerialNumber().Select(ch => (ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 1)).ToArray();
        for (int i = 0; i < _targetBlobColors.Length; i++)
        {
            if (i == 4)
            {
                _targetBlobColors[i] = snDigits[2] + snDigits[5];
                continue;
            }
            if (i > 1)
            {
                _targetBlobColors[i] = snDigits[i + 1];
                continue;
            }
            _targetBlobColors[i] = snDigits[i];
        }
        for (int i = 0; i < 5; i++)
        {
            _blobColors[i] = Rnd.Range(0, 26);
            var color = new Color(
                (60 + 70 * ((float) (_blobColors[i] / 9))) / 255,
                (60 + 70 * ((float) (_blobColors[i] % 9) / 3)) / 255,
                (60 + 70 * ((float) (_blobColors[i] % 3))) / 255);
            ColorBlobs[i].material.color = color;
            Debug.LogFormat("[The White Button #{0}] The {1} determined number is {2}. Converting this to base-3 yields {3}{4}{5}, giving you color {6}",
                _moduleId, POSITIONS[i], _targetBlobColors[i],
                _targetBlobColors[i] / 9, _targetBlobColors[i] % 9 / 3, _targetBlobColors[i] % 3,
                COLORNAMES[_targetBlobColors[i]]);
        }
    }

    private void SetColorblindMode(bool mode)
    {
        _colorblindMode = mode;
        ColorblindScreen.SetActive(_colorblindMode);
    }

    private bool WhiteButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            _longPress = false;
            _buttonHeld = true;
        }
        return false;
    }

    private void WhiteButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            _buttonHeld = false;
            if (!_longPress)
            {
                AdjustColor((_currentColor + 2) % 3, _currentBlob, _isAdding);
            }
            else
            {
                bool correct = true;
                for (int i = 0; i < _blobColors.Length; i++)
                {
                    if (_blobColors[i] != _targetBlobColors[i])
                        correct = false;
                }
                StartCoroutine(CheckLogic(correct));
            }
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            WhiteButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        WhiteButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private void Update()
    {
        var seconds = (int) BombInfo.GetTime() % 30;
        if (seconds != _lastTimerSeconds)
        {
            _lastTimerSeconds = seconds;
            if (_buttonHeld)
                _longPress = true;
        }
        if (!_moduleSolved)
        {
            for (int i = 0; i < 3; i++)
                ColorSegments[i].material = i == (seconds % 3) ? OnColors[i] : OffColors[i];

            if (!_isStriking)
            {
                for (int i = 0; i < 5; i++)
                {
                    LeftLeds[i].material = i == (seconds % 5) ? OnLight : OffLight;
                    RightLeds[i].material = i == (seconds % 5) ? OnLight : OffLight;
                }
                _currentColor = seconds % 3;
                _currentBlob = 4 - ((seconds + 4) % 5);
                _isAdding = seconds % 2 == 0;
                ColorblindText.text = CBNAMES[_blobColors[_currentBlob]];
            }
        }
        else
            ColorblindText.text = "";

    }

    private void AdjustColor(int channel, int blob, bool add)
    {
        var r = _blobColors[blob] / 9;
        var g = _blobColors[blob] % 9 / 3;
        var b = _blobColors[blob] % 3;
        var offset = add ? 1 : -1;
        if (channel == 2)
            r = Math.Max(0, Math.Min(2, r + offset));
        if (channel == 1)
            g = Math.Max(0, Math.Min(2, g + offset));
        if (channel == 0)
            b = Math.Max(0, Math.Min(2, b + offset));
        _blobColors[blob] = r * 9 + g * 3 + b;
        ColorBlobs[blob].material.color = new Color(
            (60 + 70 * ((float) r)) / 255,
            (60 + 70 * ((float) g)) / 255,
            (60 + 70 * ((float) b)) / 255);
    }

    private IEnumerator CheckLogic(bool c)
    {
        if (_moduleSolved)
            yield break;
        if (c)
        {
            Module.HandlePass();
            _moduleSolved = true;
            for (int i = 0; i < 5; i++)
            {
                LeftLeds[i].material = OnColors[2];
                RightLeds[i].material = OnColors[2];
                if (i < 3)
                    ColorSegments[i].material = OnColors[i];
            }
            Debug.LogFormat("[The White Button #{0}] You submitted {1}, {2}, {3}, {4}, {5}. Module solved.", _moduleId,
                _blobColors[0], _blobColors[1], _blobColors[2], _blobColors[3], _blobColors[4]);
        }
        else
        {
            Module.HandleStrike();
            for (int i = 0; i < 5; i++)
            {
                if (_blobColors[i] != _targetBlobColors[i])
                {
                    LeftLeds[4 - (i + 4) % 5].material = OnColors[0];
                    RightLeds[4 - (i + 4) % 5].material = OnColors[0];
                    Debug.LogFormat("[The White Button #{0}] The {1} blob should have been {2}, instead of {3}.", _moduleId, POSITIONS[i], _targetBlobColors[i], _blobColors[i]);
                }
                else
                {
                    LeftLeds[4 - (i + 4) % 5].material = OnColors[2];
                    RightLeds[4 - (i + 4) % 5].material = OnColors[2];
                }
            }
            Debug.LogFormat("[The White Button #{0}] Not all correct colors have been submitted. Strike.", _moduleId);
            _isStriking = true;
            yield return new WaitForSeconds(1.5f);
            _isStriking = false;
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} tap 23 17 5 [tap when the seconds on the timer are these values exactly] | !{0} submit | !{0} colorblind/cb";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*(colou?rblind|cb)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            SetColorblindMode(!_colorblindMode);
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(submit|hold)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            WhiteButtonSelectable.OnInteract();
            yield return new WaitForSeconds(1f);
            WhiteButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
            yield break;
        }

        var pieces = command.Trim().ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var skip = pieces[0].EqualsAny("tap", "press", "click", "push") ? 1 : 0;
        var numbers = new List<int>();
        foreach (var numPiece in pieces.Skip(skip))
        {
            int value;
            if (!int.TryParse(numPiece, out value) || value < 0 || value >= 60)
                yield break;
            numbers.Add(value);
        }
        yield return null;
        yield return "waiting music";
        while (numbers.Count > 0)
        {
            keepWaiting:
            var ix = numbers.IndexOf((int) BombInfo.GetTime() % 60);
            if (ix == -1)
            {
                yield return "trycancel";
                goto keepWaiting;
            }
            yield return new[] { WhiteButtonSelectable };
            numbers.RemoveAt(ix);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        var corrections = new List<int>();
        for (var blob = 0; blob < 5; blob++)
        {
            var curR = _blobColors[blob] / 9;
            var curG = _blobColors[blob] % 9 / 3;
            var curB = _blobColors[blob] % 3;
            var goalR = _targetBlobColors[blob] / 9;
            var goalG = _targetBlobColors[blob] % 9 / 3;
            var goalB = _targetBlobColors[blob] % 3;

            if (curR > goalR)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 0 && i % 2 == 1), curR - goalR));
            else if (curR < goalR)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 0 && i % 2 == 0), goalR - curR));

            if (curG > goalG)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 2 && i % 2 == 1), curG - goalG));
            else if (curG < goalG)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 2 && i % 2 == 0), goalG - curG));

            if (curB > goalB)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 1 && i % 2 == 1), curB - goalB));
            else if (curB < goalB)
                corrections.AddRange(Enumerable.Repeat(Enumerable.Range(0, 30).First(i => 4 - (i + 4) % 5 == blob && i % 3 == 1 && i % 2 == 0), goalB - curB));
        }

        while (corrections.Count > 0)
        {
            keepWaiting:
            var ix = corrections.IndexOf((int) BombInfo.GetTime() % 30);
            if (ix == -1)
            {
                yield return true;
                goto keepWaiting;
            }
            WhiteButtonSelectable.OnInteract();
            WhiteButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
            corrections.RemoveAt(ix);
        }

        WhiteButtonSelectable.OnInteract();
        yield return new WaitForSeconds(1f);
        WhiteButtonSelectable.OnInteractEnded();

        while (!_moduleSolved)
            yield return true;
    }
}
