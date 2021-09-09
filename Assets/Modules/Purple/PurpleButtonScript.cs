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
        }
        var chooseFrom = dic.Values.ToArray();
        var chosen = chooseFrom[Rnd.Range(0, chooseFrom.Length)];
        _cyclingNumbers = chosen.Select(ix => edgeworkValues[ix]).ToArray();
        _solutionStates = chosen.Select(ix => ix % 2 != 0).ToArray();

        Debug.LogFormat("[The Purple Button #{0}] Cycling numbers: {1}", _moduleId, _cyclingNumbers.Join(", "));
        Debug.LogFormat("[The Purple Button #{0}] Desired states: {1}", _moduleId, _solutionStates.Select(b => b ? "on" : "off").Join(", "));

        StartCoroutine(CycleLight());
    }

    private IEnumerator CycleLight()
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
                }
                else
                    Audio.PlaySoundAtTransform("PurpleButtonCycle", transform);
            }

            ToggleBulb();
            _cyclePosition = (_cyclePosition + 1) % 6;
            DisplayText.text = _cyclingNumbers[_cyclePosition].ToString();
            yield return new WaitForSeconds(1.666f);
        }
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
}
