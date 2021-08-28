using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class OrangeButtonScript : MonoBehaviour
{
    [SerializeField]
    private KMBombModule Module;
    [SerializeField]
    private KMBombInfo BombInfo;
    [SerializeField]
    private KMAudio Audio;
    [SerializeField]
    private KMRuleSeedable RuleSeedable;
    [SerializeField]
    private KMSelectable OrangeButtonSelectable;
    [SerializeField]
    private GameObject OrangeButtonCap;
    [SerializeField]
    private MeshRenderer[] LedLights;
    [SerializeField]
    private Material[] LedMats;
    [SerializeField]
    private TextMesh _orangeButtonText;
    [SerializeField]
    private TextMesh[] _mainText;
    [SerializeField]
    private Color[] _textColors;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _lastTimerSeconds, _lightIndex, _tapIx, _buttonNum;
    private int[] _lightCycle = { 0, 1, 2, 3 }, _screenTextIxs = new int[3], _snDigits = new int[6], _snPairs = new int[6], _solutions = new int[3], _pairPos = { 0, 1, 2, 3, 4, 5 };
    private List<int> letterTable = new List<int>();
    private string LETTERS = "ABCDEFGHI";
    private string[] POSITIONS = { "first", "second", "third", "fourth", "fifth", "sixth" };
    private bool _moduleSolved, _flashingStrike, _buttonHeld, _checkTap, _isStriking;
    private Coroutine _animateButton;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        // START RULE SEED
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Orange Button #{0}] Using rule seed {1}", _moduleId, rnd.Seed);
        rnd.Next(0, 2);
        rnd.Next(0, 2);
        for (int ix = 0; ix < 9; ix++)
        {
            int[] letIx = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            rnd.ShuffleFisherYates(letIx);
            for (int jx = 0; jx < 9; jx++)
            {
                letterTable.Add(letIx[jx]);
            }
        }
        rnd.ShuffleFisherYates(_pairPos);
        // END RULE SEED

        OrangeButtonSelectable.OnInteract += delegate ()
        {
            OrangeButtonPress();
            return false;
        };
        OrangeButtonSelectable.OnInteractEnded += delegate ()
        {
            OrangeButtonRelease();
        };

        for (int i = 0; i < _screenTextIxs.Length; i++)
            _screenTextIxs[i] = Rnd.Range(0, 9);

        string SerialNumber = BombInfo.GetSerialNumber();
        for (int i = 0; i < _snDigits.Length; i++)
            _snDigits[i] = (SerialNumber[i] >= '0' && SerialNumber[i] <= '9' ? SerialNumber[i] - '0' : SerialNumber[i] - 'A' + 10) % 9;

        _buttonNum = Rnd.Range(0, 9);
        _orangeButtonText.text = _buttonNum.ToString();
        SetText();

        _lightCycle.Shuffle();
        SetLights();

        FindSolution();
    }

    private void OrangeButtonPress()
    {
        _animateButton = StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            _checkTap = false;
            _buttonHeld = true;
        }
    }
    private void OrangeButtonRelease()
    {
        _animateButton = StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved && !_isStriking)
        {
            _buttonHeld = false;
            if (_checkTap)
                CheckAnswer();
            else
            {
                SetLights();
                ButtonTap(_lastTimerSeconds);
            }
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            OrangeButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void SetLights()
    {
        _lightIndex++;
        for (int i = 0; i < _lightCycle.Length; i++)
        {
            if (i != _lightCycle[(_lightIndex + 1) % 4])
                LedLights[i].material = LedMats[0];
            else
                LedLights[i].material = LedMats[1];
        }

    }

    private void Update()
    {
        var seconds = (int)BombInfo.GetTime() % 3;
        if (seconds != _lastTimerSeconds)
        {
            _lastTimerSeconds = seconds;
            if (!_flashingStrike)
                SetTextColors(seconds);
            if (_buttonHeld)
                _checkTap = true;
        }
    }

    private void SetText()
    {
        for (int i = 0; i < _mainText.Length; i++)
            _mainText[i].text = LETTERS.Substring(_screenTextIxs[i], 1);
    }
    private void SetTextColors(int sec)
    {
        if (!_moduleSolved && !_isStriking)
        {
            for (int i = 0; i < _mainText.Length; i++)
            {
                if (i == sec)
                    _mainText[i].color = _textColors[3];
                else
                    _mainText[i].color = _textColors[2];
            }
        }
    }

    private void ButtonTap(int ix)
    {
        _screenTextIxs[ix] = (_screenTextIxs[ix] + _lightCycle[_lightIndex % 4] + 1) % 9;
        SetText();
    }

    private void FindSolution()
    {
        for (int i = 0; i < _snPairs.Length; i++)
            _snPairs[i] = _snDigits[_pairPos[i]];
        for (int i = 0; i < _solutions.Length; i++)
            _solutions[i] = letterTable[((_snPairs[i * 2] + _buttonNum) % 9) + (_snPairs[i * 2 + 1] * 9)];
        Debug.LogFormat("[The Orange Button #{0}] The solution is {1}, {2}, {3}.", _moduleId, LETTERS[_solutions[0]], LETTERS[_solutions[1]], LETTERS[_solutions[2]]);
    }

    private void CheckAnswer()
    {
        bool isSolve = true;
        for (int i = 0; i < _screenTextIxs.Length; i++)
            if (_solutions[i] != _screenTextIxs[i])
                isSolve = false;
        if (isSolve)
        {
            Module.HandlePass();
            _moduleSolved = true;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            for (int i = 0; i < _screenTextIxs.Length; i++)
                _mainText[i].color = _textColors[1];
            for (int i = 0; i < LedLights.Length; i++)
                LedLights[i].material = LedMats[1];
            Debug.LogFormat("[The Orange Button #{0}] Submitted {1}, {2}, {3}. Module solved.", _moduleId, LETTERS[_screenTextIxs[0]], LETTERS[_screenTextIxs[1]], LETTERS[_screenTextIxs[2]]);
        }
        else
            StartCoroutine(Strike());
    }

    private IEnumerator Strike()
    {
        Debug.LogFormat("[The Orange Button #{0}] Submitted {1}, {2}, {3}. Strike.", _moduleId, LETTERS[_screenTextIxs[0]], LETTERS[_screenTextIxs[1]], LETTERS[_screenTextIxs[2]]);
        Module.HandleStrike();
        _isStriking = true;
        for (int i = 0; i < _screenTextIxs.Length; i++)
        {
            if (_screenTextIxs[i] != _solutions[i])
                _mainText[i].color = _textColors[0];
            else
                _mainText[i].color = _textColors[1];
        }
        yield return new WaitForSeconds(1.2f);
        _isStriking = false;
    }
}
