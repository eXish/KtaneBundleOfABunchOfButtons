using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using RNG = UnityEngine.Random;

public class GlitchedButtonScript : MonoBehaviour
{
    /*
    [UnityEditor.MenuItem("Glitched Button/Generate Data")]
    private static void DoStuff()
    {
        List<string> possibilities = new List<string>();
        foreach(char a in "01")
            foreach(char b in "01")
                foreach(char c in "01")
                    foreach(char d in "01")
                        foreach(char e in "01")
                            foreach(char f in "01")
                                foreach(char g in "01")
                                    foreach(char h in "01")
                                        possibilities.Add(a.ToString() + b + c + d + e + f + g + h);
        int best = 0;
        for(int iter = 0; iter < 100000; iter++)
        {
            List<string> chosenPossibilities = possibilities.Shuffle().Take(8).ToList();
            string total = chosenPossibilities.Aggregate("", (a, b) => a + b);
            List<string> possible = new List<string>();
            for(int i = 0; i < total.Length - 8; i++)
                possible.Add(total.Substring(i, 8));
            possible = possible.Where(test => possible.Where(s => CheckFuzzy(s, test)).Count() == 1).ToList();
            if(possible.Count <= best)
                continue;
            Debug.Log(total);
            Debug.Log(possible.Count);
            Debug.Log(possible.Join(" "));
            best = possible.Count;
        }
    }

    private static bool CheckFuzzy(string a, string b)
    {
        if(a.Length != b.Length)
            return false;
        int count = 0;
        for(int i = 0; i < a.Length; i++)
            if(a[i] == b[i])
                count++;
        return count >= a.Length - 1;
    }
    */

    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable GlitchedButtonSelectable;
    public GameObject GlitchedButtonCap;
    public TextMesh Text;
    public KMBombInfo Info;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    //private const string DATA = "1001110000000100110101110110001011011110101001111100110011111011";
    private static readonly string[] _dataSplit = "10011100 00111000 01110000 00000010 00000100 00100110 10011010 00110101 11010111 10101110 10111011 01110110 11011000 10110001 01100010 11000101 10001011 00010110 00101101 11011110 10111101 01111010 11110101 11101010 11010100 10101001 11111001 11110011 11100110 10011001".Split(' ');

    private string _chosenData, _modifiedData;
    private int _target;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        GlitchedButtonSelectable.OnInteract += GlitchedButtonPress;
        GlitchedButtonSelectable.OnInteractEnded += GlitchedButtonRelease;

        _chosenData = _dataSplit.PickRandom();
        int ix = RNG.Range(0, 8);
        char[] d = _chosenData.ToCharArray();
        d[ix] = d[ix] == '1' ? '0' : '1';
        _modifiedData = d.Aggregate("", (s, c) => s + c);

        _target = Array.IndexOf(_dataSplit, _chosenData);
        Text.text = _modifiedData;

        Debug.LogFormat("[The Glitched Button #{0}] The display is \"{1}\", modified from \"{2}\".", _moduleId, _modifiedData, _chosenData);
        Debug.LogFormat("[The Glitched Button #{0}] This is id {1}. Tap on XX:{1} or XX:{2}.", _moduleId, _target < 10 ? "0" + _target : _target.ToString(), _target + 30);
    }

    private bool GlitchedButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        if(_moduleSolved)
            return false;

        if((int)(Info.GetTime() % 30f) == _target)
        {
            Debug.LogFormat("[The Glitched Button #{0}] Good job! Module solved.", _moduleId);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            _moduleSolved = true;
            Module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[The Glitched Button #{0}] You pressed at XX:{1}. Strike!", _moduleId, (int)(Info.GetTime() % 60) < 10 ? "0" + (int)(Info.GetTime() % 60) : ((int)(Info.GetTime() % 60)).ToString());
            Module.HandleStrike();
        }

        return false;
    }

    private void GlitchedButtonRelease()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        StartCoroutine(AnimateButton(-0.05f, 0f));
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            GlitchedButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        GlitchedButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} tap on 56 [Submits at that time]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if(_moduleSolved)
            yield break;

        Match m;
        if((m = Regex.Match(command, @"^\s*(?:press|tap|push|submit)?\s*(?:on|at)?\s*(\d\d?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            int i;
            if(!int.TryParse(m.Groups[1].Value, out i))
                yield break;
            if(i > 59 || i < 0)
                yield break;
            yield return null;
            while((int)(Info.GetTime() % 60f) != i)
                yield return "trycancel";
            GlitchedButtonSelectable.OnInteract();
            GlitchedButtonSelectable.OnInteractEnded();
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while((int)(Info.GetTime() % 30f) != _target)
            yield return true;
        GlitchedButtonSelectable.OnInteract();
        GlitchedButtonSelectable.OnInteractEnded();
    }
}
