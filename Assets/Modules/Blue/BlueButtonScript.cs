using System.Collections;
using BlueButtonLib;
using UnityEngine;

public class BlueButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;

    // Puzzle
    private PolyominoPlacement[] _polyominoes;
    private int[] _polyominoColors;
    private int[] _colorStageColors;
    private int[] _equationOffsets;
    private string _word;

    // Internals
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += BlueButtonPress;
        ButtonSelectable.OnInteractEnded += BlueButtonRelease;
    }

    private void GeneratePuzzle()
    {
        var puzzle = BlueButtonPuzzle.GeneratePuzzle();
        _polyominoes = puzzle.Polyominoes;
        _polyominoColors = puzzle.PolyominoColors;
        _colorStageColors = puzzle.ColorStageColors;
        _equationOffsets = puzzle.EquationOffsets;
        _word = puzzle.Word;
    }

    private bool BlueButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_moduleSolved)
            return false;
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
