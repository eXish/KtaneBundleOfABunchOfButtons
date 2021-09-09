using System;
using System.Collections;
using System.Linq;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class MaroonButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public GameObject ButtonObj;
    public GameObject ModuleBackground;
    public GameObject[] HighestSpheres, UpperSpheres, MiddleSpheres, LowerSpheres;
    public GameObject[] HighestSphereParents, UpperSphereParents, MiddleSphereParents, LowerSphereParents;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private int _buttonAction;
    private int[] _sphereRotations = new int[4];
    private bool _spheresAreRotating;
    private bool _isAnimating;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;
        for (int i = 0; i < _sphereRotations.Length; i++)
        {
            _sphereRotations[i] = i % 2 == 0 ? Rnd.Range(3, 7) : Rnd.Range(-6, -2);
            Debug.LogFormat("{0}", _sphereRotations[i]);
        }
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            //code
        }
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            if (!_isAnimating)
                StartCoroutine(FirstButtonAnimations());
        }
    }

    private IEnumerator FirstButtonAnimations()
    {
        if (_buttonAction == 3)
            StartCoroutine(SquishButton());
        var duration = 0.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (_buttonAction == 0)
            {
                ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0.4f, 0.3f, duration), Easing.InOutQuad(elapsed, 0.4f, 0.3f, duration), Easing.InOutQuad(elapsed, 0.4f, 0.3f, duration));
                ButtonObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0.046f, 0.039f, duration), 0f);
                ModuleBackground.GetComponent<MeshRenderer>().material.color = Color32.Lerp(new Color32(255, 255, 255, 255), new Color32(245, 150, 180, 255), elapsed / duration);
            }
            if (_buttonAction == 1)
            {
                ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0.3f, 0.2f, duration), Easing.InOutQuad(elapsed, 0.3f, 0.2f, duration), Easing.InOutQuad(elapsed, 0.3f, 0.2f, duration));
                ButtonObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0.039f, 0.031f, duration), 0f);
                ModuleBackground.GetComponent<MeshRenderer>().material.color = Color32.Lerp(new Color32(245, 150, 180, 255), new Color32(225, 100, 140, 255), elapsed / duration);
            }
            if (_buttonAction == 2)
            {
                ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0.2f, 0.1f, duration), Easing.InOutQuad(elapsed, 0.2f, 0.1f, duration), Easing.InOutQuad(elapsed, 0.2f, 0.1f, duration));
                ButtonObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0.031f, 0.023f, duration), 0f);
                ModuleBackground.GetComponent<MeshRenderer>().material.color = Color32.Lerp(new Color32(225, 100, 140, 255), new Color32(180, 60, 100, 255), elapsed / duration);
            }
            if (_buttonAction == 3)
                ModuleBackground.GetComponent<MeshRenderer>().material.color = Color32.Lerp(new Color32(180, 60, 100, 255), new Color32(140, 30, 70, 255), elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        _buttonAction++;
    }

    private IEnumerator SquishButton()
    {
        _isAnimating = true;
        var durationZero = 0.3f;
        var elapsedZero = 0f;
        while (elapsedZero < durationZero)
        {
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsedZero, 0.1f, 0.05f, durationZero), Easing.InOutQuad(elapsedZero, 0.1f, 0.05f, durationZero), Easing.InOutQuad(elapsedZero, 0.1f, 0.05f, durationZero));
            yield return null;
            elapsedZero += Time.deltaTime;
        }
        var durationFirst = 0.3f;
        var elapsedFirst = 0f;
        while (elapsedFirst < durationFirst)
        {
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsedFirst, 0.05f, 0.4f, durationFirst), Easing.InOutQuad(elapsedFirst, 0.05f, 0.4f, durationFirst), Easing.InOutQuad(elapsedFirst, 0.05f, 0.1f, durationFirst));
            ButtonObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsedFirst, 0.023f, 0.046f, durationFirst), 0f);
            yield return null;
            elapsedFirst += Time.deltaTime;
        }
        ButtonObj.transform.localPosition = new Vector3(0f, 0.046f, 0f);
        var durationSecond = 0.3f;
        var elapsedSecond = 0f;
        while (elapsedSecond < durationSecond)
        {
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsedSecond, 0.4f, 0.1f, durationSecond), 0.4f, Easing.InOutQuad(elapsedSecond, 0.1f, 0.4f, durationSecond));
            yield return null;
            elapsedSecond += Time.deltaTime;
        }
        var durationThird = 0.3f;
        var elapsedThird = 0f;
        while (elapsedThird < durationThird)
        {
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsedThird, 0.1f, 0.5f, durationThird), 0.4f, Easing.InOutQuad(elapsedThird, 0.4f, 0.5f, durationThird));
            yield return null;
            elapsedThird += Time.deltaTime;
        }
        var durationFourth = 0.5f;
        var elapsedFourth = 0f;
        while (elapsedFourth < durationFourth)
        {
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsedFourth, 0.5f, 0f, durationFourth), Easing.InOutQuad(elapsedFourth, 0.4f, 0f, durationFourth), Easing.InOutQuad(elapsedFourth, 0.5f, 0f, durationFourth));
            yield return null;
            elapsedFourth += Time.deltaTime;
        }
        _isAnimating = false;
        StartCoroutine(MoveSpheres());
    }

    private IEnumerator MoveSpheres()
    {
        var duration = 0.5f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < UpperSpheres.Length; i++)
            {
                HighestSpheres[i].transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration));
                UpperSpheres[i].transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration));
                MiddleSpheres[i].transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration));
                LowerSpheres[i].transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration), Easing.InOutQuad(elapsed, 0f, 0.01f, duration));
                HighestSpheres[i].transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0f, 0.1f, duration), Easing.InOutQuad(elapsed, 0f, 0.09f, duration));
                UpperSpheres[i].transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0f, 0.08f, duration), Easing.InOutQuad(elapsed, 0f, 0.07f, duration));
                MiddleSpheres[i].transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0f, 0.06f, duration), Easing.InOutQuad(elapsed, 0f, 0.05f, duration));
                LowerSpheres[i].transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0f, 0.04f, duration), Easing.InOutQuad(elapsed, 0f, 0.03f, duration));
            }
            ButtonObj.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0f, 0.1f, duration), Easing.InOutQuad(elapsed, 0f, 0.1f, duration), Easing.InOutQuad(elapsed, 0f, 0.1f, duration));
            ButtonObj.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, 0.046f, 0.023f, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        _spheresAreRotating = true;
        StartCoroutine(SpinSphereParents());
    }

    private IEnumerator SpinSphereParents()
    {
        while (_spheresAreRotating)
        {
            var duration = 4f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                for (int i = 0; i < HighestSphereParents.Length; i++)
                {
                    HighestSphereParents[i].transform.localEulerAngles = new Vector3(0f, Easing.InOutQuad(elapsed, 0f + (i * 45), _sphereRotations[0] * 45 + (i * 45), duration), 0f);
                    UpperSphereParents[i].transform.localEulerAngles = new Vector3(0f, Easing.InOutQuad(elapsed, 0f + (i * 45), _sphereRotations[1] * 45 + (i * 45), duration), 0f);
                    MiddleSphereParents[i].transform.localEulerAngles = new Vector3(0f, Easing.InOutQuad(elapsed, 0f + (i * 45), _sphereRotations[2] * 45 + (i * 45), duration), 0f);
                    LowerSphereParents[i].transform.localEulerAngles = new Vector3(0f, Easing.InOutQuad(elapsed, 0f + (i * 45), _sphereRotations[3] * 45 + (i * 45), duration), 0f);
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
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
