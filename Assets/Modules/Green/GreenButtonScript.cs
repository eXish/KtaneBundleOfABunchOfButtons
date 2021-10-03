using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using RNG = UnityEngine.Random;

public class GreenButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable GreenButtonSelectable;
    public GameObject GreenButtonCap;
    public TextMesh _screenTextLeft, _screenTextRight;

    private static int _moduleIdCounter = 1;
    private int _moduleId, _currentChar, _currentSet;
    private bool _moduleSolved, _playing;
    private string _displayedString, _targetWord;
    private bool[] _submission;

    private static readonly List<string> _words = new List<string>()
    {
        "ABOUT", "AFTER", "AMONG", "AGAIN", "ABOVE", "ALONG", "AWARD", "ALLOW", "ALONE", "AHEAD", "APPLY", "AWARE", "AVOID", "AGENT", "ASSET", "AGREE", "ADULT", "APART", "AUDIO", "ASIDE", "ARRAY", "ALIVE", "ARGUE", "APPLE", "ACUTE", "ADMIT", "ARENA", "ACTOR", "ALERT", "ALBUM", "ALTER", "ANGLE", "ALARM", "ADAPT", "ANGEL", "ANKLE", "ALIEN", "ARROW", "ALLEY", "AWAKE", "AMEND", "ARMOR", "ALIGN", "ALTAR", "ALLOY", "AMBER", "ATTIC", "AGILE", "AROMA", "APRON", "ACORN", "ADORE", "AMUSE", "ABYSS",
        "BOARD", "BEGAN", "BRING", "BUILT", "BLACK", "BASIC", "BELOW", "BUILD", "BEGIN", "BREAK", "BROWN", "BEACH", "BRAND", "BLOCK", "BEGUN", "BRIEF", "BROKE", "BOUND", "BOOST", "BUYER", "BAKER", "BLIND", "BREAD", "BENCH", "BURST", "BONUS", "BRICK", "BLEND", "BRUSH", "BLANK", "BUNCH", "BRAVE", "BLOWN", "BLAST", "BATCH", "BRASS", "BACON", "BAKED", "BLOOM", "BERRY", "BEARD", "BRAKE", "BOXER", "BURNT", "BADGE", "BLAND", "BLISS", "BUNNY", "BULKY", "BLUFF", "BLINK",
        "COULD", "CHIEF", "CAUSE", "CLASS", "CLOSE", "CLEAR", "CHILD", "COVER", "CROSS", "CARRY", "CLAIM", "CHECK", "CIVIL", "CHAIN", "COAST", "CLEAN", "CHAIR", "CYCLE", "CABLE", "COUNT", "CATCH", "CROWD", "CROWN", "CLOCK", "CHART", "CHEAP", "CRASH", "CHASE", "CURVE", "CLICK", "CRAFT", "CLIMB", "CRAZY", "CLOUD", "CARGO", "COLOR", "COMIC", "CLOTH", "CHAOS", "CANAL", "CLIFF", "CEASE", "CHARM", "CREEK", "CABIN", "CRANE", "CLASH", "CORAL", "CHEER", "CANDY", "CHILL", "CREST", "CHALK", "COUCH", "CRUST", "CHESS", "CHUNK", "CRAWL",
        "DAILY", "DRIVE", "DEPTH", "DRAWN", "DOUBT", "DREAM", "DRINK", "DANCE", "DELAY", "DOZEN", "DROVE", "DRESS", "DEBUT", "DEALT", "DRILL", "DRIED", "DAIRY", "DENSE", "DRAIN", "DIARY", "DERBY", "DRIFT", "DIGIT", "DECAY", "DEBIT", "DRANK", "DUSTY", "DODGE", "DISCO", "DAISY", "DOUGH", "DWARF", "DIZZY", "DINER", "DONUT",
        "EVERY", "EARLY", "EVENT", "EXTRA", "ENJOY", "ENTER", "EQUAL", "ENTRY", "EARTH", "EXIST", "ERROR", "EMPTY", "EXACT", "EAGER", "EAGLE", "ESSAY", "ELDER", "ELBOW", "EATEN", "EQUIP", "ERASE", "EVADE",
        "FIRST", "FOUND", "FIELD", "FINAL", "FORCE", "FRONT", "FOCUS", "FLOOR", "FIXED", "FIBER", "FRESH", "FIFTH", "FRAME", "FORUM", "FALSE", "FAULT", "FRUIT", "FUNNY", "FLASH", "FLUID", "FLOOD", "FENCE", "FANCY", "FROST", "FLOAT", "FLIES", "FLAME", "FORGE", "FAINT", "FLOUR", "FEAST", "FAIRY", "FAVOR", "FLUSH", "FLAIR", "FLARE", "FUZZY", "FROZE", "FLUTE", "FOYER", "FUDGE", "FLASK",
        "GROUP", "GOING", "GREAT", "GIVEN", "GREEN", "GUIDE", "GRAND", "GLASS", "GROWN", "GRADE", "GIANT", "GUEST", "GUESS", "GRASS", "GRAIN", "GRASP", "GRAPH", "GLORY", "GAUGE", "GHOST", "GRILL", "GRAMS", "GREET", "GLOVE", "GOOSE", "GRAPE", "GLIDE", "GRAVY", "GEESE", "GENIE",
        "HOUSE", "HUMAN", "HEART", "HOTEL", "HAPPY", "HEAVY", "HORSE", "HABIT", "HEDGE", "HONEY", "HURRY", "HANDY", "HONOR", "HATCH", "HOBBY", "HAIRY", "HASTE", "HINGE", "HUSKY", "HUMID", "HOUND", "HUMOR", "HIPPO", "HYENA",
        "ISSUE", "IMAGE", "IDEAL", "INDEX", "INPUT", "INNER", "IMPLY", "IRONY", "IVORY", "ICING", "IDIOM", "INTRO", "ITCHY", "INGOT", "IGLOO", "ITEMS", "IDEAS",
        "JOINT", "JUDGE", "JUICE", "JEWEL", "JOLLY", "JELLY", "JUMBO", "JUICY", "JOKER", "JUMPS",
        "KNOCK", "KITTY", "KAYAK", "KNEEL", "KARAT", "KNEAD", "KOALA", "KABOB", "KAZOO",
        "LOCAL", "LARGE", "LEVEL", "LATER", "LIGHT", "LOWER", "LEAVE", "LEARN", "LIVES", "LEAST", "LINKS", "LIMIT", "LUNCH", "LAYER", "LABEL", "LOGIC", "LUCKY", "LAUGH", "LASER", "LOYAL", "LOBBY", "LIVER", "LODGE", "LEMON", "LEVER", "LITER", "LEAPT", "LYRIC", "LUNAR", "LOUSY", "LEDGE", "LOGIN", "LEAKY", "LOOPY",
        "MIGHT", "MONEY", "MAJOR", "MARCH", "MONTH", "MEDIA", "MODEL", "MUSIC", "MATCH", "MAYBE", "MEANT", "MIXED", "METAL", "MOTOR", "MINOR", "MOUTH", "MOVIE", "MAGIC", "MOUNT", "MOUSE", "MINUS", "MAKER", "MERIT", "MEDAL", "METER", "MERGE", "MIDST", "MARSH", "MANOR", "MUMMY", "MAPLE", "MOIST", "MERRY", "MOTTO", "MUDDY", "MESSY", "MIMIC", "MUTED", "MIXER", "MOVER", "MOTEL", "MURKY", "MAGMA", "MISTY", "MANGO", "MELON", "MOOSE", "MORPH", "MEDIC",
        "NEVER", "NEEDS", "NORTH", "NIGHT", "NOTED", "NOVEL", "NOISE", "NURSE", "NINTH", "NOBLE", "NERVE", "NOISY", "NEEDY", "NUDGE", "NIFTY", "NINJA", "NACHO", "NAILS", "NEIGH",
        "OTHER", "OFTEN", "ORDER", "OFFER", "OCCUR", "OCEAN", "OUTER", "OPERA", "OLIVE", "ORBIT", "OUNCE", "ONION", "OASIS", "OTTER",
        "PLACE", "POINT", "POWER", "PRESS", "PARTY", "PRICE", "PAPER", "PHONE", "PLANT", "PRIME", "PRIOR", "PIECE", "PHASE", "PROVE", "PEACE", "PROUD", "PRINT", "PANEL", "PHOTO", "POUND", "PILOT", "PLATE", "PRIZE", "PRIDE", "PLAIN", "PAINT", "PITCH", "PLANE", "PIANO", "PATCH", "PANIC", "PAUSE", "PEARL", "PLAZA", "PIZZA", "PINCH", "PASTE", "POLAR", "PATIO", "PILES", "PEACH", "PORCH", "PIXEL", "POKER", "PERIL", "PUPPY", "PEDAL", "PIVOT", "PRISM", "PLANK", "PANDA",
        "QUITE", "QUICK", "QUIET", "QUEEN", "QUOTE", "QUEST", "QUERY", "QUEUE", "QUILT", "QUIRK", "QUAIL", "QUILL", "QUART", "QUARK", "QUACK",
        "RIGHT", "RANGE", "ROUND", "REACH", "READY", "RADIO", "ROYAL", "RAPID", "RAISE", "RIVER", "ROUTE", "RATIO", "ROUGH", "RIVAL", "REPLY", "RALLY", "REACT", "ROCKY", "RIGID", "RELAX", "REALM", "RADAR", "RELAY", "RISKY", "RENEW", "RANCH", "ROBOT", "RUSTY", "ROAST", "RUMOR", "ROGUE", "RAINY", "RAMPS", "RINSE", "REUSE", "RAVEN", "RECAP", "RHYME", "RHINO", "RELIC", "ROOMY", "REMIX",
        "STILL", "STATE", "SINCE", "SMALL", "STAFF", "SHARE", "SOUTH", "SHORT", "STOCK", "STUDY", "SPACE", "STORY", "STAGE", "SPEED", "SOUND", "SHOWN", "SPENT", "SPEND", "SERVE", "SPEAK", "SCALE", "STYLE", "STAND", "SHALL", "STORE", "SOILD", "SHEET", "STOOD", "SHAPE", "SUITE", "SCENE", "STONE", "STUFF", "SHIFT", "SCORE", "SPLIT", "STEEL", "SCOPE", "SPOKE", "SPORT", "SLEEP", "SMART", "SIGHT", "SIXTH", "SKILL", "STICK", "SMILE", "SOLVE", "SHOCK", "SWEET", "SUPER", "SUGAR", "STORM", "STUCK", "SHELF", "SHELL", "SPARE", "SHIRT", "STEAM", "SLIDE", "SWING", "SHORE", "SWEPT", "SOLAR", "SPELL", "SHAKE", "SHEEP", "SWIFT", "STAMP", "SPRAY", "SAUCE", "STACK",
        "THERE", "THEIR", "THESE", "THOSE", "TODAY", "THINK", "THIRD", "TOTAL", "TRADE", "THING", "TABLE", "TRACK", "TRIED", "TWICE", "TRAIN", "TRULY", "TRUTH", "TREND", "TRICK", "TOUGH", "TOWER", "THROW", "TEACH", "TASTE", "THICK", "TOPIC", "TIRED", "THREW", "TRUCK", "TRACE", "TRAIL", "TENTH", "TWIST", "TIGER", "THUMB", "TENSE", "TOKEN", "TOAST", "TOWEL", "TORCH", "TRASH", "TASTY", "TRAIT", "TIMER", "THORN",
        "UNDER", "UNTIL", "URBAN", "UPPER", "USUAL", "USAGE", "UPSET", "UNITY", "ULTRA", "UNITE", "UNLIT", "UDDER", "UNZIP",
        "VISIT", "VALUE", "VOICE", "VIDEO", "VITAL", "VALID", "VAGUE", "VIVID", "VOCAL", "VALVE", "VAPOR", "VAULT", "VIGOR", "VOWEL",
        "WHICH", "WOULD", "WHERE", "WORLD", "WHILE", "WATER", "WHOLE", "WHITE", "WORTH", "WRITE", "WRONG", "WATCH", "WROTE", "WASTE", "WORSE", "WORST", "WHEEL", "WIDTH", "WHEAT", "WIRES", "WIRED", "WRIST", "WEIRD", "WEIGH", "WAIST", "WAGON", "WIDEN", "WRECK", "WHALE", "WINDY", "WHISK", "WALTZ",
        "YOUNG", "YOUTH", "YIELD", "YACHT", "YEAST", "YODEL", "YELLS",
        "ZEBRA"
    };

    private static readonly Dictionary<char, int[]> _newYorkPoint = new Dictionary<char, int[]>
    {
        { 'A', new int[] { 1, 1, 0 } },
        { 'B', new int[] { 3, 1, 1, 0 } },
        { 'C', new int[] { 1, 1, 2, 0 } },
        { 'D', new int[] { 1, 3, 0 } },
        { 'E', new int[] { 1, 0 } },
        { 'F', new int[] { 1, 1, 1, 0 } },
        { 'G', new int[] { 2, 2, 3, 0 } },
        { 'H', new int[] { 2, 3, 3, 0 } },
        { 'I', new int[] { 3, 0 } },
        { 'J', new int[] { 1, 3, 1, 0 } },
        { 'K', new int[] { 1, 1, 3, 0 } },
        { 'L', new int[] { 3, 2, 0 } },
        { 'M', new int[] { 3, 1, 0 } },
        { 'N', new int[] { 2, 2, 0 } },
        { 'O', new int[] { 2, 1, 0 } },
        { 'P', new int[] { 1, 2, 2, 0 } },
        { 'Q', new int[] { 3, 2, 2, 0 } },
        { 'R', new int[] { 2, 3, 0 } },
        { 'S', new int[] { 1, 2, 0 } },
        { 'T', new int[] { 2, 0 } },
        { 'U', new int[] { 2, 2, 2, 0 } },
        { 'V', new int[] { 1, 2, 1, 0 } },
        { 'W', new int[] { 2, 2, 1, 0 } },
        { 'X', new int[] { 3, 2, 3, 0 } },
        { 'Y', new int[] { 2, 1, 2, 0 } },
        { 'Z', new int[] { 1, 3, 3, 0 } }
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        GreenButtonSelectable.OnInteract += GreenButtonPress;
        GreenButtonSelectable.OnInteractEnded += GreenButtonRelease;

        _targetWord = _displayedString = _words.PickRandom();
        while (_displayedString.Length < 7)
            _displayedString = _displayedString.Insert(RNG.Range(0, _displayedString.Length), ((char) ('A' + RNG.Range(0, 26))).ToString());

        Debug.LogFormat("[The Green Button #{0}] The letters that will play are: {1}", _moduleId, _displayedString);
        Debug.LogFormat("[The Green Button #{0}] A word that can be made from this is: {1}", _moduleId, _targetWord);
    }


    private bool GreenButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            if (_playing)
            {
                _submission[_currentChar] = true;
            }
            else
                StartCoroutine(PlayNYP());
        }
        return false;
    }

    private IEnumerator PlayNYP()
    {
        _playing = true;
        _submission = new bool[_displayedString.Length];
        _currentSet = 0;
        const float delay = 0.25f;
        yield return new WaitForSeconds(delay);
        for (_currentChar = 0; _currentChar < _displayedString.Length; _currentChar++)
        {
            char c = _displayedString[_currentChar];
            foreach (int n in _newYorkPoint[c])
            {
                _currentSet++;
                if (n >= 0)
                    Audio.PlaySoundAtTransform("GreenButtonOne", transform);
                _screenTextLeft.text = ".";
                yield return new WaitForSeconds(delay);
                if (n >= 1)
                    Audio.PlaySoundAtTransform("GreenButtonTwo", transform);
                _screenTextRight.text = ".";
                yield return new WaitForSeconds(delay);
                if (n >= 2)
                    Audio.PlaySoundAtTransform("GreenButtonThree", transform);
                _screenTextLeft.text = ":";
                yield return new WaitForSeconds(delay);
                if (n >= 3)
                    Audio.PlaySoundAtTransform("GreenButtonFour", transform);
                _screenTextRight.text = ":";
                yield return new WaitForSeconds(delay);
                _screenTextLeft.text = "";
                _screenTextRight.text = "";
            }
        }
        _playing = false;
        if (_submission.Any(b => b))
        {
            string word = "";
            for (int i = 0; i < _submission.Length; i++)
                if (_submission[i])
                    word += _displayedString[i];
            if (_words.Contains(word))
            {
                _moduleSolved = true;
                Debug.LogFormat("[The Green Button #{0}] You submitted \"{1}\", which which was a valid word. Good job!", _moduleId, word);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Module.HandlePass();
            }
            else
            {
                Debug.LogFormat("[The Green Button #{0}] You submitted \"{1}\", which is not a word. Strike!", _moduleId, word);
                Module.HandleStrike();
            }
        }
    }

    private void GreenButtonRelease()
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
            GreenButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        GreenButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} tap | !{0} submit 1 4 8 10 15 [submits those sets of 4]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        if (_playing)
        {
            yield return "sendtochaterror The module is already playing! Please wait...";
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(?:press|tap|push|submit|play)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            GreenButtonSelectable.OnInteract();
            GreenButtonSelectable.OnInteractEnded();
            yield return null;
            yield break;
        }

        string[] chunks = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (chunks.Length > 1 && Regex.IsMatch(chunks[0], @"^\s*(?:press|tap|push|submit|play)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            int[] answers = new int[chunks.Length - 1];
            for (int i = 1; i < chunks.Length; i++)
            {
                if (!int.TryParse(chunks[i].Trim(), out answers[i - 1]))
                    yield break;
                if (i != 1 && (answers[i - 1] <= 0 || answers[i - 1] > _displayedString.Sum(c => _newYorkPoint[c].Length) || answers[i - 1] <= answers[i - 2]))
                    yield break;
            }

            yield return null;
            yield return "strike";
            yield return "solve";

            GreenButtonSelectable.OnInteract();
            yield return new WaitForSeconds(.1f);
            GreenButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);

            foreach (int answer in answers)
            {
                while (_currentSet != answer)
                    yield return null;
                GreenButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                GreenButtonSelectable.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (_playing)
            yield return true;
        GreenButtonSelectable.OnInteract();
        GreenButtonSelectable.OnInteractEnded();
        while (_targetWord.Length > 0)
        {
            if (_currentChar >= _displayedString.Length)
            {
                yield return null;
                continue;
            }
            if (_targetWord[0] == _displayedString[_currentChar])
            {
                GreenButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                GreenButtonSelectable.OnInteractEnded();
                _targetWord = _targetWord.Substring(1);
                int ch = _currentChar;
                yield return new WaitWhile(() => _currentChar == ch);
            }
            yield return null;
        }
    }
}
