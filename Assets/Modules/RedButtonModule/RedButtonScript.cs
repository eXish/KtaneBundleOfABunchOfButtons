using System;
using System.Collections;
using System.Linq;
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
    private bool _moduleSolved, _buttonHeld, _checkTap;
    private readonly bool[] _topValues = new bool[10];
    private readonly bool[] _bottomValues = new bool[10];
    private readonly bool[] _currentValues = new bool[10];
    private readonly bool[] _solutionValues = new bool[10];
    private readonly string _logicGates = "∧∨⊻|↓↔→←";
    private string _topTextDisplay;
    private string _bottomTextDisplay;
    private string _solution = "";
    private readonly string[] _logicGatesWords = { "AND", "OR", "XOR", "NAND", "NOR", "XNOR", "LEFT IMPLIES RIGHT", "RIGHT IMPLIES LEFT" };


    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        RedButtonSelectable.OnInteract += RedButtonPress;
        RedButtonSelectable.OnInteractEnded += RedButtonRelease;

        _buttonSymbol = Rnd.Range(0, 8);
        _buttonText.text = _logicGates.Substring(_buttonSymbol, 1);
        Debug.LogFormat("[The Red Button #{0}] The logic gate written on the button is {1}", _moduleId, _logicGatesWords[_buttonSymbol]);


        for (int i = 0; i < 10; i++)
        {
            int rndTop = Rnd.Range(0, 2);
            int rndBottom = Rnd.Range(0, 2);
            int rndColor = Rnd.Range(0, 2);

            _topText[i].text = rndTop == 0 ? "0" : "1";
            _topValues[i] = rndTop == 0 ? false : true;
            _topText[i].color = _textColors[rndColor];
            _topTextDisplay += rndTop == 0 ? "0" : "1";

            _bottomText[i].text = rndBottom == 0 ? "0" : "1";
            _bottomValues[i] = rndBottom == 0 ? false : true;
            _bottomText[i].color = _textColors[rndColor];
            _bottomTextDisplay += rndBottom == 0 ? "0" : "1";

            _lightsText[i].color = _textColors[2];

            _currentValues[i] = rndColor == 0 ? true : false;
        }
        Debug.LogFormat("[The Red Button #{0}] The top display is {1}", _moduleId, _topTextDisplay);
        Debug.LogFormat("[The Red Button #{0}] The bottom display is {1}", _moduleId, _bottomTextDisplay);

        for (int i = 0; i < 10; i++)
        {
            if (_buttonSymbol == 0) // AND
                if (_topValues[i] && _bottomValues[i])
                    _solutionValues[i] = true;
            if (_buttonSymbol == 1) // OR
                if (_topValues[i] || _bottomValues[i])
                    _solutionValues[i] = true;
            if (_buttonSymbol == 2) // XOR
                if (_topValues[i] != _bottomValues[i])
                    _solutionValues[i] = true;
            if (_buttonSymbol == 3) // NAND
                if (!(_topValues[i] && _bottomValues[i]))
                    _solutionValues[i] = true;
            if (_buttonSymbol == 4) // NOR
                if (!(_topValues[i] || _bottomValues[i]))
                    _solutionValues[i] = true;
            if (_buttonSymbol == 5) // XNOR
                if (_topValues[i] == _bottomValues[i])
                    _solutionValues[i] = true;
            if (_buttonSymbol == 6) // LEFT IMPLIES RIGHT or TOP IMPLIES BOTTOM
                if (!(_topValues[i] && !_bottomValues[i]))
                    _solutionValues[i] = true;
            if (_buttonSymbol == 7) // RIGHT IMPLIES LEFT or BOTTOM IMPLIES TOP
                if (!(!_topValues[i] && _bottomValues[i]))
                    _solutionValues[i] = true;
            if (_solutionValues[i])
                _solution += "G";
            if (!_solutionValues[i])
                _solution += "R";
        }
        Debug.LogFormat("[The Red Button #{0}] The correct answer is: {1}", _moduleId, _solution);
    }


    private bool RedButtonPress()
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

    private void RedButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
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
    private void Update()
    {
        var seconds = (int) BombInfo.GetTime() % 10;
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
        if (_currentValues[i])
        {
            _currentValues[i] = false;
            _topText[i].color = _textColors[1];
            _bottomText[i].color = _textColors[1];
        }
        else
        {
            _currentValues[i] = true;
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
        var isAnswerCorrect = true;
        for (int i = 0; i < 10; i++)
        {
            if (_currentValues[i] != _solutionValues[i])
                isAnswerCorrect = false;
            if (_currentValues[i])
                submission += "G";
            if (!_currentValues[i])
                submission += "R";
        }
        if (isAnswerCorrect)
        {
            Module.HandlePass();
            _moduleSolved = true;
            Debug.LogFormat("[The Red Button #{0}] You submitted {1}. Module solved.", _moduleId, submission);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[The Red Button #{0}] You submitted {1}, but the correct answer is {2}. Strike.", _moduleId, submission, _solution);
        }
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 1 5 [hold on 1, release on 5] | !{0} tap";
    private bool ZenModeActive; // set by TP via Reflection
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        string[] split = command.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 1 && split[0] == "tap")
        {
            yield return null;
            var current = (int) BombInfo.GetTime() % 10;
            while ((int) BombInfo.GetTime() % 10 == current)
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
            while ((int) BombInfo.GetTime() % 10 != holdTime)
                yield return "trycancel";
            RedButtonSelectable.OnInteract();
            while ((int) BombInfo.GetTime() % 10 != releaseTime)
                yield return null;
            RedButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleSolved)
        {
            var cur = (int) BombInfo.GetTime() % 10;
            var next = (cur + (ZenModeActive ? 1 : 9)) % 10;
            if (_currentValues[next] != _solutionValues[next])
            {
                RedButtonSelectable.OnInteract();
                while ((int) BombInfo.GetTime() % 10 == cur)
                    yield return null;
                RedButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }

            if (Enumerable.Range(0, 10).All(ix => _currentValues[ix] == _solutionValues[ix]))
            {
                RedButtonSelectable.OnInteract();
                RedButtonSelectable.OnInteractEnded();
            }

            yield return true;
        }
    }
}
