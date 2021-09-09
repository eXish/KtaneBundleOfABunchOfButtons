using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class NavyButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public GameObject RealButton;
    public GameObject FakeButton;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private int[] _realPositions = new int[5];
    private int _duplicateIndex;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;
        FakeButton.SetActive(false);
        for (int i = 0; i < _realPositions.Length; i++)
        {
            _realPositions[i] = Rnd.Range(0, (int)Math.Pow(i, 2));
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
            //code
        }
    }

    private IEnumerator DuplicateButtons()
    {
        var elapsed = 0.3f;
        var duration = 0f;
        while (elapsed < duration)
        {
            RealButton.transform.localScale = new Vector3(Easing.InOutQuad(elapsed, 0.4f, 0f, duration), Easing.InOutQuad(elapsed, 0.4f, 0f, duration), Easing.InOutQuad(elapsed, 0.4f, 0f, duration));
            for (int i = 0; i < (int)Math.Pow(_duplicateIndex, 2); i++)
            {

            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        _duplicateIndex++;
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
