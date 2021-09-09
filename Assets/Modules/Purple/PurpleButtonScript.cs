using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class PurpleButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public Material BulbOnMat;
    public Material BulbOffMat;
    public MeshRenderer Bulb;
    public Light BulbLight;
    public TextMesh DisplayText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _isLightOn;
    private bool _isPreInputModeActive;
    private bool _isInputModeActive;
    private int _cyclePosition;
    private int[] _cyclingNumbers;
    private bool[] _solutionStates;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        BulbLight.range *= transform.lossyScale.x;

        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Purple Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);
        for (var i = rnd.Next(0, 200); i >= 0; i--)
            rnd.Next(0, 2);
        var edgeworkValues = newArray(
            Bomb.GetIndicators().Count(),
            Bomb.GetOnIndicators().Count(),
            Bomb.GetOffIndicators().Count(),
            Bomb.GetIndicators().Count(ind => ind.Contains('A')),
            Bomb.GetIndicators().Count(ind => ind.Contains('N')),
            Bomb.GetIndicators().Count(ind => ind.Contains('R')),
            Bomb.GetIndicators().Count(ind => ind.Contains('S')),
            Bomb.GetBatteryCount(),
            Bomb.GetBatteryCount(Battery.D),
            Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4),
            Bomb.GetBatteryHolderCount(),
            Bomb.GetPortCount(),
            Bomb.GetPortPlateCount(),
            Bomb.CountUniquePorts(),
            Bomb.GetSerialNumber()[2] - '0',
            Bomb.GetSerialNumber()[5] - '0',
            Bomb.GetSerialNumberLetters().Count(),
            Bomb.GetSerialNumberNumbers().Count(),
            Bomb.GetSerialNumberLetters().Count(ltr => !"AEIOU".Contains(ltr)),
            Bomb.GetSerialNumber().Distinct().Count());
        rnd.ShuffleFisherYates(edgeworkValues);

        Debug.LogFormat("[The Purple Button #{0}] Edgework values: {1}", _moduleId, edgeworkValues.Join(", "));

        var dic = new Dictionary<string, List<int>>();  // indexes in subsequence
        var nonUnique = new HashSet<string>();

        foreach (var subseq in subsequences(edgeworkValues.Length, 6, 6))
        {
            var values = subseq.Select(ix => edgeworkValues[ix]).ToArray();

            var minCycle = values;
            var cyclesSeen = new HashSet<string> { minCycle.Join(", ") };
            for (var cycIx = 1; cycIx < values.Length; cycIx++)
            {
                var cycled = cycleArray(values, cycIx);
                for (var i = 0; i < minCycle.Length; i++)
                {
                    if (minCycle[i] < cycled[i])
                        break;
                    if (minCycle[i] > cycled[i])
                    {
                        minCycle = cycled;
                        break;
                    }
                }
                if (!cyclesSeen.Add(cycled.Join(", ")))
                    goto busted;
            }
            var key = minCycle.Join(", ");
            if (nonUnique.Contains(key))
                continue;
            if (dic.ContainsKey(key))
            {
                dic.Remove(key);
                nonUnique.Add(key);
                continue;
            }
            dic[key] = subseq;
            busted:;
        }
        var chooseFrom = dic.Values.ToArray();
        var chosen = chooseFrom[Rnd.Range(0, chooseFrom.Length)];
        _cyclingNumbers = chosen.Select(ix => edgeworkValues[ix]).ToArray();
        _solutionStates = chosen.Select(ix => ix % 2 != 0).ToArray();

        Debug.LogFormat("[The Purple Button #{0}] Cycling numbers: {1}", _moduleId, _cyclingNumbers.Join(", "));
        Debug.LogFormat("[The Purple Button #{0}] Desired states: {1}", _moduleId, _solutionStates.Select(b => b ? "on" : "off").Join(", "));

        StartCoroutine(CycleDigits());
    }

    private IEnumerator CycleDigits()
    {
        while (!_moduleSolved)
        {
            if (_isPreInputModeActive && _cyclePosition == _solutionStates.Length - 2)
            {
                Audio.PlaySoundAtTransform("PurpleButtonIntro", transform);
            }
            else if (_isPreInputModeActive && _cyclePosition == _solutionStates.Length - 1)
            {
                _isInputModeActive = true;
                _isPreInputModeActive = false;
                Audio.PlaySoundAtTransform("PurpleButtonCycle", transform);
            }
            else if (_isInputModeActive)
            {
                if (_isLightOn == _solutionStates[_cyclePosition] && _cyclePosition == _solutionStates.Length - 1)
                {
                    Debug.LogFormat("[The Purple Button #{0}] Module solved.", _moduleId);
                    Module.HandlePass();
                    _isInputModeActive = false;
                    _moduleSolved = true;
                    if (Bomb.GetSolvedModuleNames().Count < Bomb.GetSolvableModuleNames().Count)
                        Audio.PlaySoundAtTransform("PurpleButtonOutro", transform);
                }
                else if (_isInputModeActive && _isLightOn != _solutionStates[_cyclePosition])
                {
                    Debug.LogFormat("[The Purple Button #{0}] You entered “{1}” at position #{2}. Strike!", _moduleId, _isLightOn ? "on" : "off", _cyclePosition + 1);
                    Module.HandleStrike();
                    _isInputModeActive = false;
                    _isPreInputModeActive = false;
                }
                else
                    Audio.PlaySoundAtTransform("PurpleButtonCycle", transform);
            }

            ToggleBulb();
            _cyclePosition = (_cyclePosition + 1) % 6;
            DisplayText.text = _cyclingNumbers[_cyclePosition].ToString();
            yield return new WaitForSeconds(1.666f);
        }

        DisplayText.gameObject.SetActive(false);
        if (_isLightOn)
            ToggleBulb();
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_moduleSolved)
            return false;

        if (_isInputModeActive)
        {
            ToggleBulb();
        }
        else
        {
            if (_cyclePosition != _cyclingNumbers.Length - 2)
            {
                Debug.LogFormat("[The Purple Button #{0}] You attempted to start input at position #{1} (instead of {2}). Strike!", _moduleId, _cyclePosition + 1, _cyclingNumbers.Length - 1);
                Module.HandleStrike();
            }
            else
                _isPreInputModeActive = true;
        }

        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private void ToggleBulb()
    {
        _isLightOn = !_isLightOn;
        Bulb.sharedMaterial = _isLightOn ? BulbOnMat : BulbOffMat;
        BulbLight.gameObject.SetActive(_isLightOn);
    }

    private T[] newArray<T>(params T[] array) { return array; }

    private static IEnumerable<List<int>> subsequences(int range, int minLen, int maxLen)
    {
        if (minLen <= 0 && range == 0)
            yield return new List<int>();
        else if (range > 0 && maxLen > 0)
        {
            foreach (var list in subsequences(range - 1, minLen - 1, maxLen))
            {
                if (list.Count >= minLen)
                {
                    if (list.Count < maxLen)
                    {
                        var list2 = list.ToList();
                        list2.Add(range - 1);
                        yield return list2;
                    }
                    yield return list;
                }
                else if (list.Count < maxLen)
                {
                    list.Add(range - 1);
                    yield return list;
                }
            }
        }
    }

    private static T[] cycleArray<T>(T[] array, int amount)
    {
        if (amount == 0)
            return array;
        T[] result = new T[array.Length];
        Array.Copy(array, amount, result, 0, array.Length - amount);
        Array.Copy(array, 0, result, array.Length - amount, amount);
        return result;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} 0 2 3 on off off on on off [wait for this sequence of numbers and tap the button on the last one, then skip one digit, then toggle the light into the specified states]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;
        var pieces = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var digits = new List<int>();
        var i = 0;
        int val;
        for (; i < pieces.Length && int.TryParse(pieces[i], out val); i++)
            digits.Add(val);
        if (digits.Count == 0)
        {
            yield return "sendtochaterror You need to specify at least one number before the on/off states.";
            yield break;
        }
        var desiredStates = new List<bool>();
        for (; i < pieces.Length; i++)
        {
            if (pieces[i] == "on")
                desiredStates.Add(true);
            else if (pieces[i] == "off")
                desiredStates.Add(false);
            else
                yield break;
        }
        if (desiredStates.Count != 6)
        {
            yield return "sendtochaterror You need exactly 6 on/off states.";
            yield break;
        }

        var startIndex = Enumerable.Range(0, _cyclingNumbers.Length).IndexOf(cIx => Enumerable.Range(0, digits.Count).All(dIx => _cyclingNumbers[(cIx + dIx) % _cyclingNumbers.Length] == digits[dIx]));
        if (startIndex == -1)
        {
            yield return "sendtochaterror That sequence of numbers is not on the module.";
            yield break;
        }

        yield return null;
        yield return "strike";
        yield return "solve";

        while (_cyclePosition != (startIndex + digits.Count - 1) % _cyclingNumbers.Length)
            yield return null;

        ButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        ButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
        for (var cyclePosition = 0; cyclePosition < 6; cyclePosition++)
        {
            while (_cyclePosition != cyclePosition)
                yield return null;

            if (_isLightOn != desiredStates[cyclePosition])
            {
                ButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                ButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;

        while (_cyclePosition != _cyclingNumbers.Length - 2)
            yield return true;

        ButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        ButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
        for (var cyclePosition = 0; cyclePosition < 6; cyclePosition++)
        {
            while (_cyclePosition != cyclePosition)
                yield return null;

            if (_isLightOn != _solutionStates[cyclePosition])
            {
                ButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                ButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
        }

        while (!_moduleSolved)
            yield return true;
    }
}
