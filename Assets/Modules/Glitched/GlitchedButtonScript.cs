using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class GlitchedButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMBombInfo Bomb;
    public KMSelectable GlitchedButtonSelectable;
    public GameObject GlitchedButtonCap;
    public TextMesh Text;
    public MeshRenderer TextRenderer;
    public MaskShaderManager MaskShaderManager;
    public MeshRenderer Mask;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private string _cyclingBits;
    private int _solution;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        GlitchedButtonSelectable.OnInteract += GlitchedButtonPress;
        GlitchedButtonSelectable.OnInteractEnded += GlitchedButtonRelease;

        var fontTexture = TextRenderer.sharedMaterial.mainTexture;
        var mr = MaskShaderManager.MakeMaterials();
        TextRenderer.material = mr.Text;
        TextRenderer.material.mainTexture = fontTexture;
        Mask.sharedMaterial = mr.Mask;

        // RULE SEED STARTS HERE
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Glitched Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);

        const int bitLength = 12;
        var results = new List<int>();
        while (results.Count < 15)
        {
            var all = Enumerable.Range(0, 1 << bitLength)
                .Where(v =>
                {
                    for (var cycle = 1; cycle < bitLength; cycle++)
                    {
                        var cycled = ((v << cycle) & ((1 << bitLength) - 1)) | (v >> (bitLength - cycle));
                        var nb = countBits(cycled ^ v);
                        if (nb == 0 || nb == 2)
                            return false;
                    }
                    return true;
                })
                .ToList();
            results.Clear();
            while (all.Count > 0 && results.Count < 15)
            {
                var rndIx = rnd.Next(0, all.Count);
                var bits = all[rndIx];
                for (var cycle = 0; cycle < bitLength; cycle++)
                {
                    var compare = ((bits << cycle) & ((1 << bitLength) - 1)) | (bits >> (bitLength - cycle));
                    all.RemoveAll(v => { var nb = countBits(compare ^ v); return nb == 0 || nb == 2; });
                }
                results.Add(bits);
            }
        }
        // END RULE SEED


        var seqIx = Rnd.Range(0, results.Count);
        var seq = results[seqIx];
        Debug.LogFormat("[The Glitched Button #{0}] Selected bit sequence “{1}” (#{2}).", _moduleId, Convert.ToString(seq, 2).PadLeft(bitLength, '0'), seqIx);

        var flippedBit = Rnd.Range(0, bitLength);
        _cyclingBits = Convert.ToString(seq ^ (1 << flippedBit), 2).PadLeft(bitLength, '0');
        Debug.LogFormat("[The Glitched Button #{0}] Showing bit sequence “{1}” (flipped bit is #{2}).", _moduleId, _cyclingBits, 12 - flippedBit);

        _solution = (seqIx + 12 - flippedBit) % 15;
        Debug.LogFormat("[The Glitched Button #{0}] Tap on XX:{1:00} or XX:{2:00} or XX:{3:00} or XX:{4:00}.", _moduleId, _solution, _solution + 15, _solution + 30, _solution + 45);

        Text.text = _cyclingBits;
        StartCoroutine(CycleBits());
    }

    private void OnDestroy()
    {
        MaskShaderManager.Clear();
    }

    private IEnumerator CycleBits()
    {
        var isSolved = false;
        var solveStartTime = 0f;
        var fadeDuration = 4.7f;

        while (!isSolved || (Time.time - solveStartTime) < fadeDuration)
        {
            float time = Time.time;
            Vector3 start = Text.transform.localPosition;
            Vector3 end = Text.transform.localPosition + new Vector3(-.016f, 0f, 0f);
            while (time + 0.25f > Time.time)
            {
                Text.transform.localPosition = Vector3.Lerp(start, end, (Time.time - time) / 0.25f);
                yield return null;
            }
            Text.transform.localPosition = start;
            Text.text = Text.text.Substring(1) + Text.text[0];
            yield return null;

            if (_moduleSolved && !isSolved)
            {
                solveStartTime = Time.time;
                isSolved = true;
            }

            if (isSolved)
            {
                var v = 1 - ((Time.time - solveStartTime) / fadeDuration);
                Text.color = new Color(v, v, v, 1);
            }
        }
    }

    private int countBits(int v)
    {
        var bits = 0;
        while (v > 0)
        {
            bits++;
            v &= v - 1;
        }
        return bits;
    }

    private bool GlitchedButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        if (_moduleSolved)
            return false;

        if ((int) (Bomb.GetTime() % 15f) == _solution)
        {
            Debug.LogFormat("[The Glitched Button #{0}] Good job! Module solved.", _moduleId);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            _moduleSolved = true;
            Module.HandlePass();
        }
        else
        {
            Debug.LogFormat("[The Glitched Button #{0}] You pressed at XX:{1:00}. Strike!", _moduleId, (int) Bomb.GetTime() % 15);
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
        while (elapsed < duration)
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
        if (_moduleSolved)
            yield break;

        var m = Regex.Match(command, @"^\s*(?:press|tap|push|submit)?\s*(?:on|at)?\s*(\d\d?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        int i;
        if (!int.TryParse(m.Groups[1].Value, out i) || i < 0 || i >= 60)
            yield break;
        yield return null;
        while ((int) Bomb.GetTime() % 60 != i)
            yield return "trycancel";
        GlitchedButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        GlitchedButtonSelectable.OnInteractEnded();
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while ((int) Bomb.GetTime() % 15f != _solution)
            yield return true;
        GlitchedButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        GlitchedButtonSelectable.OnInteractEnded();
    }
}
