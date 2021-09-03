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
    public Material[] BandColors;
    public MeshRenderer[] Resistor1Bands, Resistor2Bands, Resistor3Bands;
    public TextMesh CapacitorText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private double _minTime, _maxTime;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        BlackButtonSelectable.OnInteract += BlackButtonPress;
        BlackButtonSelectable.OnInteractEnded += BlackButtonRelease;

        tryagain:
        int[] resistences = new int[] { Rnd.Range(0, 1000), Rnd.Range(0, 1000), Rnd.Range(0, 1000) };
        int[] exponents = new int[] { Rnd.Range(0, 3), Rnd.Range(0, 3), Rnd.Range(0, 3) };
        double resistence = 1 / (1 / (resistences[1] * Math.Pow(10, exponents[1])) + 1 / (resistences[2] * Math.Pow(10, exponents[2])) + 1 / (resistences[0] * Math.Pow(10, exponents[0])));

        float targetTime = Rnd.Range(10f, 35f);
        double capacitance = targetTime / resistence;
        if(capacitance >= 0.001d)
            goto tryagain;
        if(capacitance <= 0.000000001d)
            goto tryagain;

        if(capacitance >= 0.000001d)
        { // microfareds
            capacitance *= 1000000d;
            int c = (int)Math.Floor(capacitance);
            CapacitorText.text = c + " μF ±10%";
        }
        else
        { // nanofareds
            capacitance *= 1000000000d;
            int c = (int)Math.Floor(capacitance);
            CapacitorText.text = c + " nF ±10%";
        }
        Resistor1Bands[0].material = BandColors[resistences[0] / 100];
        Resistor2Bands[0].material = BandColors[resistences[1] / 100];
        Resistor3Bands[0].material = BandColors[resistences[2] / 100];

        Resistor1Bands[1].material = BandColors[(resistences[0] / 10) % 10];
        Resistor2Bands[1].material = BandColors[(resistences[1] / 10) % 10];
        Resistor3Bands[1].material = BandColors[(resistences[2] / 10) % 10];

        Resistor1Bands[2].material = BandColors[resistences[0] % 10];
        Resistor2Bands[2].material = BandColors[resistences[1] % 10];
        Resistor3Bands[2].material = BandColors[resistences[2] % 10];

        Resistor1Bands[3].material = BandColors[exponents[0]];
        Resistor2Bands[3].material = BandColors[exponents[1]];
        Resistor3Bands[3].material = BandColors[exponents[2]];

        Debug.LogFormat("[The Black Button #{0}] The resistors' values are (from top to bottom): {1}Ω {2}Ω {3}Ω", _moduleId, resistences[0] * Math.Pow(10, exponents[0]), resistences[1] * Math.Pow(10, exponents[1]), resistences[2] * Math.Pow(10, exponents[2]));
        Debug.LogFormat("[The Black Button #{0}] The capacitor's value is: ", _moduleId, CapacitorText.text.Substring(0, CapacitorText.text.Length - 5));

        double realCapacitence = int.Parse(CapacitorText.text.Substring(0, CapacitorText.text.Length - 8)) * (CapacitorText.text.Substring(CapacitorText.text.Length - 7, 1) == "μ" ? 0.000001d : 0.000000001d);
        double minResistence = 1 / (0.9d / (resistences[1] * Math.Pow(10, exponents[1])) + 0.9d / (resistences[2] * Math.Pow(10, exponents[2])) + 0.9d / (resistences[0] * Math.Pow(10, exponents[0])));
        double maxResistence = 1 / (1.1d / (resistences[1] * Math.Pow(10, exponents[1])) + 1.1d / (resistences[2] * Math.Pow(10, exponents[2])) + 1.1d / (resistences[0] * Math.Pow(10, exponents[0])));

        _minTime = minResistence * realCapacitence * 0.9d;
        _maxTime = maxResistence * realCapacitence * 1.1d;

        Debug.LogFormat("[The Black Button #{0}] Hold for between {1} and {2} seconds.", _moduleId, _minTime, _maxTime);
    }

    private bool BlackButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if(!_moduleSolved)
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
        while(elapsed < duration)
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
        if(!_moduleSolved)
            yield break;

        var m = Regex.Match(command, @"^\s*(?:(?:press|tap|click|hold|submit|make|do|go)\s+)?(\d)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if(!m.Success)
            yield break;

        var v = int.Parse(m.Groups[1].Value);
        while((int)BombInfo.GetTime() % 10 != v)
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
