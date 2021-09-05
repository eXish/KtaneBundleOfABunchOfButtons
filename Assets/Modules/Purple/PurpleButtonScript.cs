using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RNG = UnityEngine.Random;

// Feel free to change this, the rules are temporary
public class PurpleButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable PurpleButtonSelectable;
    public GameObject PurpleButtonCap;
    public Material BulbOnMat, BulbOffMat;
    public MeshRenderer Bulb;
    public Light BulbLight;
    public KMBombInfo Info;

    private int _id;
    private static int _counter = 1;

    private bool _blinkOn;
    private List<float> _delays = new List<float>();
    private List<int> _required = new List<int>();
    private List<int> _entered = new List<int>();
    private List<Event> _events = new List<Event>();
    private bool _isSolved, _isMouseDown;
    private readonly List<List<Event>> _gestures = new List<List<Event>>
    {
        new List<Event>
        {
            Event.Tick, Event.MouseDown, Event.Tick, Event.MouseUp, Event.Tick
        },
        new List<Event>
        {
            Event.Tick, Event.MouseDown, Event.Tick, Event.Tick, Event.MouseUp, Event.Tick
        },
        new List<Event>
        {
            Event.Tick, Event.MouseDown, Event.MouseUp, Event.Tick, Event.MouseDown, Event.MouseUp, Event.Tick
        },
        new List<Event>
        {
            Event.Tick, Event.MouseDown, Event.Tick, Event.MouseUp, Event.MouseDown, Event.Tick, Event.MouseUp, Event.Tick
        }
    };

    private void Start()
    {
        _id = _counter++;

        _blinkOn = false;
        SetBulbActive(false);
        BulbLight.range *= transform.lossyScale.x;

        PurpleButtonSelectable.OnInteract += ButtonPress;
        PurpleButtonSelectable.OnInteractEnded += ButtonRelease;

        GenerateStage();

        _blinkOn = true;
        StartCoroutine(Flash());
    }

    private void GenerateStage()
    {
        if (_required.Count >= 3)
        {
            Debug.LogFormat("[The Purple Button #{0}] Good job! Module solved.", _id);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Module.HandlePass();
            _isSolved = true;
            _blinkOn = false;
            return;
        }
        int c = RNG.Range(0, 4);
        _entered.Clear();
        _required.Add(c);
        _delays.AddRange(GetSequence(c));
        Debug.LogFormat("[The Purple Button #{0}] Next flashes are: {1}", _id, c);
    }

    private IEnumerable<float> GetSequence(int v)
    {
        switch (v)
        {
            case 0:
                return new[] { 1f, 0.6f };
            case 1:
                return new[] { 0.3f, 0.3f, 1f, 0.6f };
            case 2:
                return new[] { 1f, 0.3f, 0.3f, 0.6f };
            case 3:
                return new[] { 1f, 0.3f, 1f, 0.6f };
        }
        throw new ArgumentException();
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (_isSolved)
            return false;

        _blinkOn = false;

        if (!_isMouseDown)
        {
            _events.Add(Event.MouseDown);
            checkEvents();
        }
        _isMouseDown = true;
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (_isSolved)
            return;

        if (_isMouseDown && _events.Count(e => e == Event.MouseDown) > _events.Count(e => e == Event.MouseUp))
        {
            _events.Add(Event.MouseUp);
            checkEvents();
        }
        _isMouseDown = false;
    }

    private void checkEvents()
    {
        while (_events.Count >= 2 && _events[0] == Event.Tick && (_events[1] == Event.Tick || _events[1] == Event.MouseUp))
            _events.RemoveAt(1);

        var input = _gestures.IndexOf(list => list.SequenceEqual(_events));
        if (input != -1)
        {
            process(input);
            return;
        }

        if (_events.Count(e => e == Event.MouseUp) >= _events.Count(e => e == Event.MouseDown))
        {
            var validPrefix = _gestures.IndexOf(list => list.Take(_events.Count).SequenceEqual(_events));
            if (validPrefix == -1)
            {
                Debug.LogFormat("[The Purple Button #{0}] You entered {1}, which is not a valid pattern.", _id, _events.Join(", "));
                Module.HandleStrike();
                _events.Clear();
                _events.Add(Event.Tick);
            }
        }
    }

    private void process(int input)
    {
        _entered.Add(input);
        if (_required[_entered.Count - 1] == input)
        {
            if (_required.Count <= _entered.Count)
                GenerateStage();
        }
        else
        {
            Debug.LogFormat("[The Purple Button #{0}] You entered {1}, which wasn't correct. Strike!", _id, input);
            Module.HandleStrike();
        }
        _events.Clear();
        _blinkOn = true;
    }

    private IEnumerator Flash()
    {
        while (true)
        {
            if (!_blinkOn)
            {
                yield return null;
                continue;
            }
            yield return new WaitForSeconds(1f);
            bool on = false;
            foreach (float f in _delays)
            {
                SetBulbActive(on ^= true);
                if (!_blinkOn)
                {
                    SetBulbActive(false);
                    break;
                }
                yield return new WaitForSeconds(f);
            }
        }
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            PurpleButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        PurpleButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

    private void SetBulbActive(bool on)
    {
        Bulb.sharedMaterial = on ? BulbOnMat : BulbOffMat;
        BulbLight.gameObject.SetActive(on);
    }

    private enum Event
    {
        MouseUp,
        MouseDown,
        Tick
    }

    private float _lastTime;

    private void Update()
    {
        if (!_isSolved)
        {
            int time = (int) Info.GetTime();
            if (time != _lastTime)
            {
                _events.Add(Event.Tick);
                checkEvents();
                _lastTime = time;
            }
        }
    }
}
