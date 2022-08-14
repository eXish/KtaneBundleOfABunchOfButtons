using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class PinkButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable PinkButtonSelectable;
    public GameObject PinkButtonCap;
    public TextMesh PinkButtonText;
    public Renderer ModuleBG;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private static readonly string[] _abbreviatedColorNames = { "BLK", "RED", "GRN", "YLW", "BLU", "MGT", "CYN", "WHT" };
    private static readonly string[] _colorNames = { "black", "red", "green", "yellow", "blue", "magenta", "cyan", "white" };
    private static readonly string[][] _solveTexts = { new[] { "WOW", "YOU", "DID", "IT!", ":D" }, new[] { "CON", "GRA", "TUL", "ATI", "ONS!" } };
    private string _solution;
    private int _solutionProgress;
    private readonly int[] _words = new int[4];
    private readonly int[] _colors = new int[4];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (var i = 0; i < _words.Length; i++)
        {
            _words[i] = Rnd.Range(0, 8);
            _colors[i] = Rnd.Range(1, 8);   // can’t have black as a color
        }
        _words[0] |= 1; // ensure that the sequence always starts with an ‘H’, not a ‘T’

        var binary = Enumerable.Range(0, _words.Length).Select(slot =>
            Enumerable.Range(0, 3).Select(bit => (_words[slot] & (1 << bit)) != 0 ? "1" : "0").Join("") +
            Enumerable.Range(0, 3).Select(bit => (_colors[slot] & (1 << bit)) != 0 ? "1" : "0").Join("")).Join("");

        var modifiedBinary = "";
        _solution = "";
        for (var i = 0; i < binary.Length; i++)
        {
            if (i >= 3 && modifiedBinary[i - 3] == '1' && modifiedBinary[i - 2] == '1' && modifiedBinary[i - 1] == '1' && binary[i] == '1')
                modifiedBinary += '0';
            else if (i >= 1 && modifiedBinary[i - 1] == '0' && binary[i] == '0')
                modifiedBinary += '1';
            else
                modifiedBinary += binary[i];

            _solution +=
                modifiedBinary[i] == '0' ? 'T' :
                modifiedBinary.Count(ch => ch == '1') % 2 != 0 ? 'H' : 'R';
        }

        Debug.LogFormat("[The Pink Button #{0}] Flashing sequence:", _moduleId);
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[The Pink Button #{0}] Text: {1} (meaning {2}), color: {3}", _moduleId, _abbreviatedColorNames[_words[i]], _colorNames[_words[i]], _colorNames[_colors[i]]);

        Debug.LogFormat("[The Pink Button #{0}] Binary: {1}.", _moduleId, binary);
        Debug.LogFormat("[The Pink Button #{0}] Binary after modification: {1}.", _moduleId, modifiedBinary);
        Debug.LogFormat("[The Pink Button #{0}] Input sequence: {1}.", _moduleId, _solution);

        PinkButtonSelectable.OnInteract += PinkButtonPress;
        PinkButtonSelectable.OnInteractEnded += PinkButtonRelease;
        StartCoroutine(TextFlash());
    }

    private void Input(char ch, string whatUserDid)
    {
        if (_solution[_solutionProgress] != ch)
        {
            Debug.LogFormat("[The Pink Button #{0}] You {2} at position {1} in the solution. Strike!", _moduleId, _solutionProgress + 1, whatUserDid);
            _solutionProgress = 0;
            Module.HandleStrike();
        }
        else
        {
            _solutionProgress++;
            if (_solutionProgress == _solution.Length)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Debug.LogFormat("[The Pink Button #{0}] Module solved.", _moduleId);
                _moduleSolved = true;

                // We’re delaying the Module.HandlePass() if the button is still pressed so that the TP handler can do some cleanup
                if (_solution.Count(c => c == 'H') == _solution.Count(c => c == 'R'))
                    Module.HandlePass();
            }
        }
    }

    private bool PinkButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
            Input('H', "held the button");
        return false;
    }

    private void PinkButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (_moduleSolved)  // Module.HandlePass() is delayed if the module is solved while the button is held so that the TP handler can release it
            Module.HandlePass();
        if (!_moduleSolved && _solutionProgress > 0)    // ignore a button release that happened immediately after a strike
            Input('R', "released the button");
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            PinkButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        PinkButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private IEnumerator TextFlash()
    {
        while (!_moduleSolved)
        {
            for (int i = 0; i < _words.Length; i++)
            {
                yield return new WaitForSeconds(.5f);
                if (_moduleSolved)
                    goto solved;
                PinkButtonText.text = _abbreviatedColorNames[_words[i]];
                StartCoroutine(ColorFade(_colors[i]));
            }

            yield return new WaitForSeconds(.5f);
            if (_moduleSolved)
                goto solved;
            PinkButtonText.text = "GO";
            StartCoroutine(ColorFade(7));   // white

            if (_solutionProgress > 0)
                Input('T', "waited for a “GO”");
        }

        solved:
        var solveText = _solveTexts[Rnd.Range(0, _solveTexts.Length)];
        var colors = new[] { 6, 5, 7, 5, 6 };
        for (int i = 0; i < solveText.Length; i++)
        {
            PinkButtonText.text = solveText[i];
            StartCoroutine(ColorFade(colors[i]));
            yield return new WaitForSeconds(.5f);
        }
    }

    public IEnumerator ColorFade(int color)
    {
        var duration = .4f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            PinkButtonText.color = new Color(
                ((color & 1) != 0 ? 1f : 0) * (1 - elapsed / duration),
                ((color & 2) != 0 ? 1f : 0) * (1 - elapsed / duration),
                ((color & 4) != 0 ? 1f : 0) * (1 - elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        PinkButtonText.text = "";
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} HRT [hold, release, tick]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        Match m;
        if (!(m = Regex.Match(command, @"^\s*([ HRT]+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            yield break;
        if (Regex.IsMatch(m.Groups[1].Value, @"H[^R]*H", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return "sendtochaterror You can’t hold when you’re already holding.";
            yield break;
        }
        if (Regex.IsMatch(m.Groups[1].Value, @"R[^H]*R", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return "sendtochaterror You can’t release when you’re not holding.";
            yield break;
        }
        if (Regex.IsMatch(m.Groups[1].Value, @"^\s*T", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return "sendtochaterror You can’t start with a T.";
            yield break;
        }
        yield return null;

        while (PinkButtonText.text != "GO")
            yield return null;

        var held = false;
        var abort = false;
        foreach (var ch in m.Groups[1].Value.ToUpperInvariant())
        {
            if (ch == ' ')
                continue;
            if (_solutionProgress == _solution.Length)
                break;
            if (ch != _solution[_solutionProgress])
            {
                yield return "multiple strikes";
                abort = true;
            }

            switch (ch)
            {
                case 'H':
                    held = true;
                    PinkButtonSelectable.OnInteract();
                    yield return new WaitForSeconds(.1f);
                    break;
                case 'R':
                    held = false;
                    PinkButtonSelectable.OnInteractEnded();
                    yield return new WaitForSeconds(.1f);
                    break;
                case 'T':
                    var p = _solutionProgress;
                    while (_solutionProgress == p)
                        yield return null;
                    break;
            }

            if (abort)
                break;
        }
        if (held)
            PinkButtonSelectable.OnInteractEnded();

        yield return "end multiple strikes";
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (PinkButtonText.text != "GO")
            yield return null;
        yield return new WaitForSeconds(0.1f);
        var lastHeld = false;
        while (!_moduleSolved)
        {
            switch (_solution[_solutionProgress])
            {
                case 'H': lastHeld = true; PinkButtonSelectable.OnInteract(); yield return new WaitForSeconds(.1f); break;
                case 'R': lastHeld = false; PinkButtonSelectable.OnInteractEnded(); yield return new WaitForSeconds(.1f); break;
            }
            yield return null;
        }
        if (lastHeld)
            PinkButtonSelectable.OnInteractEnded();
    }
}
