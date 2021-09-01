using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

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
    private bool _checkTap;
    private int _lastTimerSeconds;
    private int[] _blobColors = new int[5];
    private int[] _targetBlobColors = new int[5];
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
                (60 + 70 * ((float)(_blobColors[i] / 9))) / 255,
                (60 + 70 * ((float)(_blobColors[i] % 9) / 3)) / 255,
                (60 + 70 * ((float)(_blobColors[i] % 3))) / 255);
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
            _checkTap = false;
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
            if (!_checkTap)
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
        var seconds = (int)BombInfo.GetTime() % 30;
        if (seconds != _lastTimerSeconds)
        {
            _lastTimerSeconds = seconds;
            CycleThings(_lastTimerSeconds);
            if (_buttonHeld)
                _checkTap = true;
        }
    }

    private void CycleThings(int sec)
    {
        if (!_moduleSolved)
        {
            for (int i = 0; i < 3; i++)
            {
                ColorSegments[i].material = i == (sec % 3) ? OnColors[i] : OffColors[i];
            }
            if (!_isStriking)
            {
                for (int i = 0; i < 5; i++)
                {
                    LeftLeds[i].material = i == (sec % 5) ? OnLight : OffLight;
                    RightLeds[i].material = i == (sec % 5) ? OnLight : OffLight;
                }
                _currentColor = sec % 3;
                _currentBlob = 4 - ((sec + 4) % 5);
                _isAdding = sec % 2 == 0;
                if (_colorblindMode)
                    ColorblindText.text = CBNAMES[_blobColors[_currentBlob]];
            }
        }
    }

    private void AdjustColor(int channel, int blob, bool add)
    {
        string prevBlob = COLORNAMES[_blobColors[blob]];
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
            (60 + 70 * ((float)r)) / 255,
            (60 + 70 * ((float)g)) / 255,
            (60 + 70 * ((float)b)) / 255);
        //Debug.LogFormat("Changed from {0} to {1}", prevBlob, COLORNAMES[_blobColors[blob]]);
    }

    private IEnumerator CheckLogic(bool c)
    {
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
                    LeftLeds[i].material = OnColors[0];
                    RightLeds[i].material = OnColors[0];
                    Debug.LogFormat("[The White Button #{0}] The {1} blob should have been {2}, instead of {3}.", _moduleId, POSITIONS[i], _targetBlobColors[i], _blobColors[i]);
                }
                else
                {
                    LeftLeds[i].material = OnColors[2];
                    RightLeds[i].material = OnColors[2];
                }
            }
            Debug.LogFormat("[The White Button #{0}] Not all correct colors have been submitted. Strike.", _moduleId);
            _isStriking = true;
            yield return new WaitForSeconds(1.5f);
            _isStriking = false;
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 1 5 [hold on 1, release on 5] | !{0} tap";
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
