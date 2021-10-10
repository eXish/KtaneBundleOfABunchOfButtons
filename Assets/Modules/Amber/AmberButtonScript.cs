using KModkit;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class AmberButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public TextMesh AmberText;
    public GameObject ButtonCap;

    private static int _moduleIdCounter = 1;
    private int[] _order = { 0, 1, 2, 3, 4, 5 };
    private int _moduleId, _lastTimerSeconds, _current, _typed;
    private int _toCycle = 4;
    private string _serialNumber, _input;
    private string _base32 = "012345689ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private bool[] _done = new bool[2];
    private bool _moduleSolved, _buttonHeld, _checkTap, _rotated, _isRotating, _typing;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;
        _serialNumber = BombInfo.GetSerialNumber();
        _order.Shuffle();
        Debug.Log(_order.Join(", "));
        AmberText.text = "";
        Module.OnActivate += delegate { AmberText.text = Convert.ToString(Convert.ToInt32(_base32.IndexOf(_serialNumber[_order[_current]]).ToString(), 10), 2).PadLeft(5, '0').ToCharArray().Select(x => x == '0' ? '?' : '¿').Join(""); };
    }

    private void Update()
    {
        var seconds = (int)BombInfo.GetTime() % 10;
        if (seconds != _lastTimerSeconds)
        {
            _lastTimerSeconds = seconds;
            _current = (_current + 1) % _toCycle;
            if (!_typing)
                AmberText.text = Convert.ToString(Convert.ToInt32(_base32.IndexOf(_serialNumber[_order[_current]]).ToString(), 10), 2).PadLeft(5, '0').ToCharArray().Select(x => x == '0' ? '?' : '¿').Join("");
            if (_buttonHeld)
                _checkTap = true;
        }
    }

    private bool ButtonPress()
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

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            _buttonHeld = false;
            if (!_checkTap)
            {
                if (!_isRotating)
                    StartCoroutine(RotateButton());
            }
            else
                Held();
        }
    }

    private void Held()
    {
        _typing = true;
        _input += _rotated ? "¿" : "?";
        AmberText.text = _input.PadRight(5, '-');
        _typed++;
        if (_typed == 5)
        {
            _typing = false;
            if (AmberText.text == Convert.ToString(Convert.ToInt32(_base32.IndexOf(_serialNumber[_order[4]]).ToString(), 10), 2).PadLeft(5, '0').ToCharArray().Select(x => x == '0' ? '?' : '¿').Join("") && !_done[0])
                _done[0] = true;
            else if (AmberText.text == Convert.ToString(Convert.ToInt32(_base32.IndexOf(_serialNumber[_order[5]]).ToString(), 10), 2).PadLeft(5, '0').ToCharArray().Select(x => x == '0' ? '?' : '¿').Join("") && !_done[1])
                _done[1] = true;
            else
                Module.HandleStrike();
            if (_done[0] && _done[1])
            {
                Module.HandlePass();
            }
            _typed = 0;
            _input = "";
        }
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

    private IEnumerator RotateButton()
    {
        _isRotating = true;
        Audio.PlaySoundAtTransform("AmberButtonUnscrew", ButtonCap.transform);
        var duration = 0.3f;
        var elapsed = 0f;
        _rotated = !_rotated;
        while (elapsed < duration)
        {
            ButtonCap.transform.localEulerAngles = new Vector3(0f, Easing.InOutQuad(elapsed, !_rotated ? 180 : 0, !_rotated ? 360 : 180, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localEulerAngles = new Vector3(0f, !_rotated ? 0 : 180, 0f);
        _isRotating = false;
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
