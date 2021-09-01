using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class BlackButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable BlackButtonSelectable;
    public GameObject ButtonCap;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        BlackButtonSelectable.OnInteract += BlackButtonPress;
        BlackButtonSelectable.OnInteractEnded += BlackButtonRelease;
    }

    private bool BlackButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            //code
        }
        return false;
    }

    private void BlackButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
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
    private readonly string TwitchHelpMessage = "!{0} press 1 [press when the last digit of the timer is a 1]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (!_moduleSolved)
            yield break;

        var m = Regex.Match(command, @"^\s*(?:(?:press|tap|click|hold|submit|make|do|go)\s+)?(\d)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        var v = int.Parse(m.Groups[1].Value);
        while ((int) BombInfo.GetTime() % 10 != v)
            yield return null;
        BlackButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        BlackButtonSelectable.OnInteractEnded();
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
}
