using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class YellowButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable YellowButtonSelectable;
    public GameObject YellowButtonCap;
    public TextMesh _screenTextLeft, _screenTextRight;
    public Material LedOn, LedOff;
    public MeshRenderer[] Leds;
    public Light[] Lights;
    public Transform LedParent;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private bool _holding;
    private int _holdWhen;
    private int _releaseWhen;
    private int _heldWhen;
    private int _denom;
    private int _numer;
    private bool _counterclockwise;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        YellowButtonSelectable.OnInteract += YellowButtonPress;
        YellowButtonSelectable.OnInteractEnded += YellowButtonRelease;

        foreach(Light l in Lights)
            l.range *= transform.lossyScale.x;

        tryAgain:
        _denom = Rnd.Range(2, 10);
        _numer = Rnd.Range(2, 10);
        if (gcd(_denom, _numer) != 1)
            goto tryAgain;
        _counterclockwise = Rnd.Range(0, 2) != 0;
        _holdWhen = _counterclockwise ? _denom : _numer;
        _releaseWhen = _counterclockwise ? _numer : _denom;

        Debug.LogFormat("[The Yellow Button #{0}] Going {3}. Hold on {1}, release on {2}.", _moduleId, _holdWhen, _releaseWhen, _counterclockwise ? "counter-clockwise" : "clockwise");

        StartCoroutine(Move());
    }

    private static int gcd(int a, int b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

    private IEnumerator Move()
    {
        var rotationPeriod = Rnd.Range(4f, 6f);
        var ledChangePeriod = rotationPeriod / _numer * _denom;
        var prevLedState = (int) (Time.time / ledChangePeriod);
        var latestRotation = 0f;

        while (!_moduleSolved)
        {
            yield return null;
            latestRotation = Time.time * 360 / rotationPeriod * (_counterclockwise ? -1 : 1);
            LedParent.localEulerAngles = new Vector3(0, latestRotation, 0);
            var ledState = (int) (Time.time / ledChangePeriod);
            if (ledState != prevLedState)
            {
                for(var i = 0; i < Leds.Length; i++)
                    SetLightState(i, Rnd.Range(0, 2) != 0);
                prevLedState = ledState;
            }
        }

        for(var i = 0; i < Leds.Length; i++)
            SetLightState(i, false);

        var duration = 3.5f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            LedParent.localEulerAngles = new Vector3(0, latestRotation + (_counterclockwise ? -1 : 1) * (elapsed - .5f * Mathf.Pow(elapsed, 2) / duration) * 360 / rotationPeriod, 0);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void SetLightState(int ix, bool on)
    {
        Leds[ix].sharedMaterial = on ? LedOn : LedOff;
        Lights[ix].gameObject.SetActive(on);
    }

    private bool YellowButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        if (!_moduleSolved)
        {
            _holding = true;
            _heldWhen = (int) Bomb.GetTime() % 10;
        }
        return false;
    }

    private void YellowButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);

        if (!_moduleSolved)
        {
            _holding = false;
            var releasedWhen = (int) Bomb.GetTime() % 10;

            if (_heldWhen != _holdWhen || releasedWhen != _releaseWhen)
            {
                Debug.LogFormat(@"[The Yellow Button #{0}] You held on {1} and released on {2}. Strike!", _moduleId, _heldWhen, releasedWhen);
                Module.HandleStrike();
            }
            else
            {
                Debug.LogFormat(@"[The Yellow Button #{0}] Module solved.", _moduleId);
                Module.HandlePass();
                _moduleSolved = true;
            }
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            YellowButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        YellowButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold 4 | !{0} release 7";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        Match m;
        int v;

        if (!_holding && (m = Regex.Match(command, @"^\s*hold\s+(\d)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success && int.TryParse(m.Groups[1].Value, out v))
        {
            yield return null;
            while ((int) Bomb.GetTime() % 10 != v)
                yield return null;
            YellowButtonSelectable.OnInteract();
            yield break;
        }

        if (_holding && (m = Regex.Match(command, @"^\s*release\s+(\d)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success && int.TryParse(m.Groups[1].Value, out v))
        {
            yield return null;
            while ((int) Bomb.GetTime() % 10 != v)
                yield return null;
            YellowButtonSelectable.OnInteractEnded();
            yield break;
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;

        while ((int) Bomb.GetTime() % 10 != _holdWhen)
            yield return true;
        YellowButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        while ((int) Bomb.GetTime() % 10 != _releaseWhen)
            yield return true;
        YellowButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
    }
}
