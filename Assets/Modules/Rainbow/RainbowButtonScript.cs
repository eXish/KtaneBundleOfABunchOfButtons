using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

public class RainbowButtonScript : MonoBehaviour
{
    [SerializeField]
    private KMBombModule _module;
    [SerializeField]
    private KMBombInfo _info;
    [SerializeField]
    private KMAudio _audio;
    [SerializeField]
    private KMSelectable _buttonSelectable;
    [SerializeField]
    private GameObject _buttonCap, _arrowObject, _lightObject;
    [SerializeField]
    private TextMesh _stageText, _stagesLeftText;
    [SerializeField]
    private KMBossModule _boss;
    [SerializeField]
    private Animator _anim;
    [SerializeField]
    private AnimationCurve _lightCurve;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _rememberedSolves, _totalStages;
    private bool _moduleSolved, _stageDelay;
    private float _arrowRotation;
    private string[] _ignored;
    private State _state;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _buttonSelectable.OnInteract += ButtonPress;
        _buttonSelectable.OnInteractEnded += ButtonRelease;

        _ignored = _boss.GetIgnoredModules(_module).Union(new string[] { "The Rainbow Button" }).ToArray();
        StartCoroutine(MonitorSolves());
        _totalStages = _info.GetSolvableModuleNames().Count(s => !_ignored.Contains(s));
        UpdateTexts();
        _lightObject.GetComponentInChildren<Light>().intensity *= transform.lossyScale.x;
    }

    private IEnumerator MonitorSolves()
    {
        int counter = 0;
        while(true)
        {
            counter++;
            if(counter >= 10)
            {
                counter = 0;
                List<string> curSolves = _info.GetSolvedModuleNames();
                if(!_stageDelay && _rememberedSolves < curSolves.Count(s => !_ignored.Contains(s)))
                {
                    _stageDelay = true;
                    _rememberedSolves++;
                    OnOtherSolved();
                }
            }
            yield return null;
        }
    }

    private void OnOtherSolved()
    {
        UpdateTexts();
        _stageDelay = false;
    }

    private void UpdateTexts()
    {
        _stageText.text = _rememberedSolves.ToString();
        _stagesLeftText.text = (_totalStages - _rememberedSolves).ToString();
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if(!_moduleSolved)
        {
            switch(_state)
            {
                case State.Idle:
                    if(_arrowRotation >= 270f)
                    {
                        _anim.SetBool("Numbers", true);
                        StartCoroutine(DelayToState(State.Numbers));
                    }
                    break;
                case State.Numbers:
                    _anim.SetBool("Numbers", false);
                    StartCoroutine(DelayToState(State.Idle));
                    break;
            }
        }
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if(!_moduleSolved)
        {
            //code
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            _buttonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        _buttonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private void FixedUpdate()
    {
        if(_state != State.Idle)
            return;
        _arrowRotation += 60f * Time.fixedDeltaTime;
        _arrowRotation %= 360f;
        //_lightObject.transform.localEulerAngles = new Vector3(0f, (float)Math.Floor(_arrowRotation / 90f) * 90f + 45f, 0f);
        _lightObject.transform.localEulerAngles = new Vector3(0f, (float)Math.Floor(_arrowRotation / 90f) * 90f + _lightCurve.Evaluate(_arrowRotation / 90f) * 90f, 0f);
        _arrowObject.transform.localEulerAngles = new Vector3(0f, _arrowRotation, 0f);
    }

    private enum State
    {
        Idle,
        Numbers,
        Animating
    }

    private IEnumerator DelayToState(State to)
    {
        _lightObject.SetActive(false);
        _state = State.Animating;
        yield return new WaitForSeconds(1f);
        _state = to;
        if(to == State.Idle)
            _lightObject.SetActive(true);
    }

    /*
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} unfinished";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if(!_moduleSolved)
            yield break;
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        yield break;
    }
    */
}
