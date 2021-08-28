using System;
using System.Collections;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class RedButtonScript : MonoBehaviour
{

    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable RedButtonSelectable;
    public GameObject RedButtonCap;
    public TextMesh _buttonText;
    public TextMesh[] _topText, _bottomText, _lightsText;
    public Color[] _textColors;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _buttonSymbol, _lastTimerSeconds;
    private int[] _pairParts = new int[6];
    private bool _moduleSolved, _buttonHeld, _correctAnswer, _checkTap;
    private bool[] _topLogic = new bool[10], _bottomLogic = new bool[10], _heldLogic = new bool[10], _solutionLogic = new bool[10];

    private Coroutine _animateButton;
    private string _logicGates = "∧∨⊻|↓↔→←", topTextDisplay, bottomTextDisplay, solution = "";
    private string[] _logicGatesWords = { "AND", "OR", "XOR", "NAND", "NOR", "XNOR", "LEFT IMPLIES RIGHT", "RIGHT IMPLIES LEFT" };


    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        RedButtonSelectable.OnInteract += delegate ()
        {
            RedButtonPress();
            return false;
        };
        RedButtonSelectable.OnInteractEnded += delegate ()
        {
            RedButtonRelease();
        };
        Generate();
    }


    private void RedButtonPress()
    {
        _animateButton = StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            _checkTap = false;
            _buttonHeld = true;
        }
    }

    private void RedButtonRelease()
    {
        _animateButton = StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            _buttonHeld = false;
            if (!_checkTap)
                CheckAnswer();
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            RedButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
    private void Generate()
    {
        _buttonSymbol = Rnd.Range(0, 8);
        _buttonText.text = _logicGates.Substring(_buttonSymbol, 1);
        Debug.LogFormat("[The Red Button #{0}] The logic gate written on the button is {1}", _moduleId, _logicGatesWords[_buttonSymbol]);


        for (int i = 0; i < 10; i++)
        {
            int rndTop = Rnd.Range(0, 2);
            int rndBottom = Rnd.Range(0, 2);
            int rndColor = Rnd.Range(0, 2);

            _topText[i].text = rndTop == 0 ? "0" : "1";
            _topLogic[i] = rndTop == 0 ? false : true;
            _topText[i].color = _textColors[rndColor];
            topTextDisplay += rndTop == 0 ? "0" : "1";

            _bottomText[i].text = rndBottom == 0 ? "0" : "1";
            _bottomLogic[i] = rndBottom == 0 ? false : true;
            _bottomText[i].color = _textColors[rndColor];
            bottomTextDisplay += rndBottom == 0 ? "0" : "1";

            _lightsText[i].color = _textColors[2];

            _heldLogic[i] = rndColor == 0 ? true : false;
        }
        Debug.LogFormat("[The Red Button #{0}] The top display is {1}", _moduleId, topTextDisplay);
        Debug.LogFormat("[The Red Button #{0}] The bottom display is {1}", _moduleId, bottomTextDisplay);

        for (int i = 0; i < 10; i++)
        {
            if (_buttonSymbol == 0) // AND
                if (_topLogic[i] && _bottomLogic[i])
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 1) // OR
                if (_topLogic[i] || _bottomLogic[i])
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 2) // XOR
                if (_topLogic[i] != _bottomLogic[i])
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 3) // NAND
                if (!(_topLogic[i] && _bottomLogic[i]))
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 4) // NOR
                if (!(_topLogic[i] || _bottomLogic[i]))
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 5) // XNOR
                if (_topLogic[i] == _bottomLogic[i])
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 6) // LEFT IMPLIES RIGHT or TOP IMPLIES BOTTOM
                if (!(_topLogic[i] && !_bottomLogic[i]))
                    _solutionLogic[i] = true;
            if (_buttonSymbol == 7) // RIGHT IMPLIES LEFT or BOTTOM IMPLIES TOP
                if (!(!_topLogic[i] && _bottomLogic[i]))
                    _solutionLogic[i] = true;
            if (_solutionLogic[i])
                solution += "G";
            if (!_solutionLogic[i])
                solution += "R";
        }
        Debug.LogFormat("[The Red Button #{0}] The correct answer is: {1}", _moduleId, solution);
    }

    private void Update()
    {
        var seconds = (int)BombInfo.GetTime() % 10;
        if (seconds != _lastTimerSeconds)
        {
            _lastTimerSeconds = seconds;
            SetLights(seconds);
            if (_buttonHeld)
            {
                ToggleLogic(seconds);
                _checkTap = true;
            }
        }
    }

    private void ToggleLogic(int i)
    {
        if (_heldLogic[i])
        {
            _heldLogic[i] = false;
            _topText[i].color = _textColors[1];
            _bottomText[i].color = _textColors[1];
        }
        else
        {
            _heldLogic[i] = true;
            _topText[i].color = _textColors[0];
            _bottomText[i].color = _textColors[0];
        }
    }

    private void SetLights(int sec)
    {
        for (int i = 0; i < 10; i++)
        {
            if (i != sec)
                _lightsText[i].color = _textColors[2];
            if (i == sec)
                _lightsText[i].color = _textColors[3];
        }
    }

    private void CheckAnswer()
    {
        string submission = "";
        _correctAnswer = true;
        for (int i = 0; i < 10; i++)
        {
            if (_heldLogic[i] != _solutionLogic[i])
                _correctAnswer = false;
            if (_heldLogic[i])
                submission += "G";
            if (!_heldLogic[i])
                submission += "R";
        }
        if (_correctAnswer)
        {
            Module.HandlePass();
            _moduleSolved = true;
            Debug.LogFormat("[The Red Button #{0}] You submitted {1}. Module solved.", _moduleId, submission);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[The Red Button #{0}] You submitted {1}, but the correct answer is {2}. Strike.", _moduleId, submission, solution);
        }
    }
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 1 5 [Hold on 1, release on 5] | !{0} tap";
#pragma warning restore 0414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        string[] split = command.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 1 && split[0] == "tap")
        {
            yield return null;
            var current = (int)BombInfo.GetTime() % 10;
            while ((int)BombInfo.GetTime() % 10 == current)
                yield return null;
            yield return new[] { RedButtonSelectable };
            yield break;
        }

        if (split.Length == 3 && split[0] == "hold")
        {
            int holdTime, releaseTime;
            if (!int.TryParse(split[1], out holdTime) || holdTime < 0 || holdTime > 9)
                yield break;
            if (!int.TryParse(split[2], out releaseTime) || releaseTime < 0 || releaseTime > 9)
                yield break;
            yield return null;
            while ((int)BombInfo.GetTime() % 10 != holdTime)
                yield return "trycancel";
            RedButtonSelectable.OnInteract();
            while ((int)BombInfo.GetTime() % 10 != releaseTime)
                yield return null;
            RedButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
