using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BlueButton;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class GrayButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public KMSelectable BlueButtonSelectable;
    public GameObject BlueButtonCap;
    public MeshRenderer BlueButtonSymbol;
    public TextMesh BlueButtonText;
    public Material[] Symbols;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private int _solution;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        // START RULE SEED
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[The Blue Button #{0}] Using rule seed: {1}.", _moduleId, rnd.Seed);
        var mazes = Enumerable.Range(0, 9).Select(mazeId => MazeLayout.Generate(10, 10, rnd)).ToArray();
        // END RULE SEED

        var sn = BombInfo.GetSerialNumber();
        var startPos = convert(sn[2]) % 10 + 10 * (convert(sn[5]) % 10);

        tryAgain:
        var symbol = Rnd.Range(0, 9);
        var maze = mazes[symbol];
        var goalPos = Rnd.Range(0, 100);

        // Find the distance from ‘startPos’ to ‘goalPos’
        var q = new Queue<int>();
        var dist = new Dictionary<int, int>();
        q.Enqueue(startPos);
        dist[startPos] = 0;
        while (q.Count > 0)
        {
            var cell = q.Dequeue();
            if (cell == goalPos)
                break;
            for (var dir = 0; dir < 4; dir++)
            {
                if (!maze.CanGo(cell, dir))
                    continue;
                var newCell = maze.Move(cell, dir);
                if (dist.ContainsKey(newCell))
                    continue;
                dist[newCell] = dist[cell] + 1;
                q.Enqueue(newCell);
            }
        }

        _solution = dist[goalPos];
        if (_solution > 59)
            goto tryAgain;

        Debug.LogFormat(@"[The Blue Button #{0}] To disarm, tap at xx:{1:00} or 00:{2:00}.", _moduleId, _solution, _solution / 10);

        BlueButtonSymbol.sharedMaterial = Symbols[symbol];
        BlueButtonText.text = string.Format("{0}, {1}", goalPos % 10, goalPos / 10);
        BlueButtonSelectable.OnInteract += BlueButtonPress;
        BlueButtonSelectable.OnInteractEnded += BlueButtonRelease;
    }

    private static int convert(char ch)
    {
        return ch >= '0' && ch <= '9' ? ch - '0' : ch - 'A' + 10;
    }

    private bool BlueButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            if ((int) BombInfo.GetTime() % 60 == _solution || (int) BombInfo.GetTime() == _solution / 10)
            {
                Debug.LogFormat(@"[The Blue Button #{0}] Module solved.", _moduleId);
                _moduleSolved = true;
                Module.HandlePass();
            }
            else
            {
                Debug.LogFormat(@"[The Blue Button #{0}] Button tapped at time {1}. Strike!", _moduleId, BombInfo.GetFormattedTime());
                Module.HandleStrike();
            }
        }
        return false;
    }

    private void BlueButtonRelease()
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
            BlueButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        BlueButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} tap 21 [tap button when timer is xx:21]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        int sec;
        if ((m = Regex.Match(command, @"^\s*(?:press|tap|push|submit|click)*\s+(\d\d?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success
                && int.TryParse(m.Groups[1].Value, out sec) && sec >= 0 && sec <= 59)
        {
            yield return null;
            while ((int) BombInfo.GetTime() % 60 != sec)
                yield return "trycancel";
            BlueButtonSelectable.OnInteract();
            yield return new WaitForSeconds(.1f);
            BlueButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;
        while ((int) BombInfo.GetTime() % 60 != _solution && (int) BombInfo.GetTime() != _solution / 10)
            yield return true;
        BlueButtonSelectable.OnInteract();
        yield return new WaitForSeconds(.1f);
        BlueButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
    }
}
