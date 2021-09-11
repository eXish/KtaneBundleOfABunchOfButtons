using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class JadeButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public TextMesh ButtonText;
    public Transform Arm;
    public Transform Segment;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved, _isSpinning, _isClockwiseSpin, _isCompleted;
    private float CurrentAngle = 0f, TargetAngle;
    private float _speed = 4f;
    private int _currentStage;
    private Coroutine _spinArm;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;
        Debug.LogFormat("[The Jade Button #{0}] Let's play a game!", _moduleId);
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            if (!_isSpinning)
            {
                _isSpinning = true;
                _currentStage = -1;
                _speed = 4f;
                StartCoroutine(Startup());
            }
            else
                CheckPress();
        }
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private void CheckPress()
    {
        if (Math.Abs(TargetAngle - CurrentAngle) < 12)
        {
            StopCoroutine(_spinArm);
            _spinArm = StartCoroutine(SpinArm());
        }
        else
        {
            _isSpinning = false;
            StopCoroutine(_spinArm);
            if (_isCompleted)
            {
                ButtonText.text = "✓";
                Module.HandlePass();
                _moduleSolved = true;
                Debug.LogFormat("[The Jade Button #{0}] You completed {1} stages. Module solved.", _moduleId, _currentStage);
            }
            else
            {
                ButtonText.text = "X";
                Module.HandleStrike();
                Debug.LogFormat("[The Jade Button #{0}] You completed {1} stages before pressing the button too early. Strike.", _moduleId, _currentStage);
            }
        }
    }

    private IEnumerator Startup()
    {
        for (int i = 3; i > 0; i--)
        {
            ButtonText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        ButtonText.text = "0";
        _spinArm = StartCoroutine(SpinArm());
    }

    private IEnumerator SpinArm()
    {
        _currentStage++;
        ButtonText.text = _currentStage.ToString();
        if (_currentStage == 10)
            _isCompleted = true;
        _isClockwiseSpin = !_isClockwiseSpin;
        if (_currentStage < 15)
            _speed -= .15f;
        else if (_currentStage < 25)
            _speed -= .1f;
        else
            _speed -= .02f;
        if (_isClockwiseSpin)
            TargetAngle = Rnd.Range(CurrentAngle + 90, CurrentAngle + 180);
        else
            TargetAngle = Rnd.Range(CurrentAngle - 90, CurrentAngle - 180);
        Segment.localEulerAngles = new Vector3(0f, TargetAngle, 0f);
        var duration = _speed;
        var elapsed = 0f;
        var y = CurrentAngle;
        while (elapsed < duration)
        {
            if (_isClockwiseSpin)
                CurrentAngle = Mathf.Lerp(y, y + 360f, elapsed / duration);
            else
                CurrentAngle = Mathf.Lerp(y, y - 360f, elapsed / duration);
            Arm.transform.localEulerAngles = new Vector3(0f, CurrentAngle, 0f);
            yield return null;
            elapsed += Time.deltaTime;
            if (_isClockwiseSpin)
            {
                if (CurrentAngle - TargetAngle > 12)
                    TooLong();
            }
            else
            {
                if (TargetAngle - CurrentAngle > 12)
                    TooLong();
            }
        }
    }

    private void TooLong()
    {
        StopCoroutine(_spinArm);
        _isSpinning = false;
        if (_isCompleted)
        {
            ButtonText.text = "✓";
            Module.HandlePass();
            _moduleSolved = true;
            Debug.LogFormat("[The Jade Button #{0}] You completed {1} stages. Module solved.", _moduleId, _currentStage);
        }
        else
        {
            Module.HandleStrike();
            ButtonText.text = "X";
            Debug.LogFormat("[The Jade Button #{0}] You completed {1} stages before pressing the button too late. Strike.", _moduleId, _currentStage);
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
}