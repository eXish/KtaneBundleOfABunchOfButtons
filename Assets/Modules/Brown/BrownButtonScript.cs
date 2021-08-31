using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class BrownButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable BrownButtonSelectable;
    public GameObject BrownButtonCap;
    public MeshRenderer WideMazeScreen;
    public Camera WideMazeCamera;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        BrownButtonSelectable.OnInteract += RedButtonPress;
        BrownButtonSelectable.OnInteractEnded += RedButtonRelease;
        WideMazeCamera.targetTexture = WideMazeScreen.material.mainTexture as RenderTexture;
    }


    private bool RedButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            //code
        }
        return false;
    }

    private void RedButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            //code
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            BrownButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        BrownButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
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
