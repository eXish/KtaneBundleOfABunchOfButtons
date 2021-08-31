using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using KModkit;
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
    private bool _moduleSolved, inputting;
    private string[] CNames = { "RED", "GRN", "BLU", "CYN", "MGT", "YLW", "WHT", "BLK" };
    private string[] CBinary = { "1z0z0z", "0z1z0z", "0z0z1z", "0z1z1z", "1z0z1z", "1z1z0z", "1z1z1z", "0z0z0z" };
    private int[] CNums = { 0, 1, 3, 5, 4, 2, 6, 7 };
    private string[] Binary;
    private string[] Texts = new string[5];
    private int[] Colors = new int[5];
    private string Answer = "", input = "";
    private Coroutine _textFlash;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        string BinaryTemp = "";
        for (int i = 0; i < 4; i++)
        {
            Texts[i] = CNames[Rnd.Range(0, CNames.Length)];
            BinaryTemp += CBinary[Array.IndexOf(CNames, Texts[i])];
            Colors[i] = Rnd.Range(0, 7);
            BinaryTemp += CBinary[Array.IndexOf(CNums, Colors[i])];
        }
        Texts[4] = "GO";
        Colors[4] = 6;
        Debug.LogFormat("[The Pink Button #{0}] Flashing sequence:", _moduleId);
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[The Pink Button #{0}] Text: {1}, Color: {2}", _moduleId, Texts[i], CNames[Array.IndexOf(CNums, Colors[i])]);

        Binary = BinaryTemp.Split('z');
        Binary = Binary.Take(Binary.Length - 1).ToArray();
        Debug.LogFormat("[The Pink Button #{0}] Binary conversion: {1}.", _moduleId, Binary.Join(""));
        bool Open = false;
        Binary[0] = "1";
        for (int i = 0; i < 24; i++)
        {
            if (i > 2)
                if (Binary[i - 3] == Binary[i - 2] && Binary[i - 2] == Binary[i - 1])
                    Binary[i] = ((1 + int.Parse(Binary[i - 1])) % 2).ToString();
            switch (Binary[i])
            {
                case "0":
                    if (Answer[i - 1] != 'T')
                    {
                        Answer += "T";
                        break;
                    }
                    else goto case "1";

                case "1":
                    if (!Open)
                    {
                        Answer += "H";
                        Open = true;
                    }
                    else
                    {
                        Answer += "R";
                        Open = false;
                    }
                    break;
            }
        }
        Debug.LogFormat("[The Pink Button #{0}] Binary after modification: {1}.", _moduleId, Binary.Join(""));
        Debug.LogFormat("[The Pink Button #{0}] Input sequence: {1}.", _moduleId, Answer);
        PinkButtonSelectable.OnInteract += PinkButtonPress;
        PinkButtonSelectable.OnInteractEnded += PinkButtonRelease;
        _textFlash = StartCoroutine(TextFlash());
    }
    void AnswerChecker()
    {
        if (Answer == input)
        {
            Module.HandlePass();
            StopCoroutine(_textFlash);
            Texts[0] = "WOW";
            Texts[1] = "YOU";
            Texts[2] = "DID";
            Texts[3] = "IT!";
            Texts[4] = ":D";
            Colors[0] = 5;
            Colors[1] = 4;
            Colors[2] = 6;
            Colors[3] = 4;
            Colors[4] = 5;
            _moduleSolved = true;
            StartCoroutine(FlashSolve());

            Debug.LogFormat("[The Pink Button #{0}] Valid.", _moduleId);
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[The Pink Button #{0}] Inputted {1} instead of {2}. Strike.", _moduleId, input, Answer);
            input = "";
        }
    }
    private bool PinkButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            inputting = true;
            input += "H";
            if (input.Length == 24)
            {
                inputting = false;
                AnswerChecker();
            }
        }
        return false;
    }

    private void PinkButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved && inputting)
        {
            input += "R";
            if (input.Length == 24)
            {
                inputting = false;
                AnswerChecker();
            }
        }
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
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(.5f);
                PinkButtonText.text = Texts[i];
                StartCoroutine(ColorFade(Colors[i]));
            }
            if (inputting && !_moduleSolved)
            {
                input += "T";
                if (input[input.Length - 2] == 'T')
                {
                    Module.HandleStrike();
                    inputting = false;
                    input = "";
                    Debug.LogFormat("[The Pink Button #{0}] You stopped inputting mid-way. Did you fall asleep? Hope this strike wakes you up...", _moduleId);
                }
                if (input.Length == 24)
                {
                    inputting = false;
                    AnswerChecker();
                }
            }
        }
    }

    private IEnumerator FlashSolve()
    {
        for (int i = 0; i < 5; i++)
        {
            PinkButtonText.text = Texts[i];
            StartCoroutine(ColorFade(Colors[i]));
            yield return new WaitForSeconds(0.5f);
        }
    }
    public IEnumerator ColorFade(int Colors)
    {
        //RGYBMCW
        byte r = 0, g = 0, b = 0;
        if (Colors % 2 == 0) r = 255;
        if (Colors == 1 || Colors == 2 || Colors == 5 || Colors == 6) g = 255;
        if (Colors > 2) b = 255;
        while (g > 0 || r > 0 || b > 0)
        {
            if (r != 0) r -= 3;
            if (g != 0) g -= 3;
            if (b != 0) b -= 3;
            PinkButtonText.color = new Color32(r, g, b, 255);
            yield return new WaitForSeconds(0.005f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} HRT (Inputs a tap, then waits for a tick. Putting an input this short is a death wish and wastes time.)";
#pragma warning restore 414

    /* public IEnumerator ProcessTwitchCommand(string command)
     {
         Match m;
         if ((m = Regex.Match(command, @"^\s*([hrt]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
         {
             //copy the code in the solver :)
         }
     }*/

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (_moduleSolved)
            yield break;
        while (PinkButtonText.text != "GO")
        {
            yield return new WaitForSeconds(.05f);
        }
        yield return new WaitForSeconds(.05f);
        for (int i = 0; i < 24; i++)
        {
            switch (Answer[i])
            {
                case 'H': PinkButtonSelectable.OnInteract(); yield return new WaitForSeconds(.5f); break;
                case 'R': PinkButtonSelectable.OnInteractEnded(); yield return new WaitForSeconds(.5f); break;
                case 'T': while (input.Last() != 'T') { yield return new WaitForSeconds(.05f); } yield return new WaitForSeconds(.5f); break;
            }
        }
        PinkButtonSelectable.OnInteractEnded();
    }
}
