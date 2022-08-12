using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButtonLib
{
    public class AzureButtonPuzzle
    {
        // Cards are represented as ternary
        public int[] SetS;
        public int[] SetE;
        public int CardT;
        public int[] Grid;
        public string SolutionWord;
        public char ForbiddenLetter;
        public AzureButtonArrowInfo[] Arrows;

        private static readonly int[] _powersOf3 = { 1, 3, 9, 27 };
        private static readonly string[] _wordList = "ABLE,ACHE,ACID,ACNE,ACRE,AFRO,AGED,AIDE,AKIN,ALAS,ALLY,ALSO,AMEN,AMID,APEX,APPS,AQUA,ARAB,ARCH,AREA,ARID,ARMY,ASKS,ATOM,AUKS,AUNT,AURA,AWAY,AXES,AXIS,AXLE,BABY,BACK,BAIL,BAIT,BAKE,BALD,BALL,BAND,BANG,BANK,BARE,BARK,BARN,BASE,BASS,BATH,BATS,BEAD,BEAM,BEAN,BEAR,BEAT,BEEF,BEEN,BEER,BELL,BELT,BEND,BENT,BERG,BEST,BIAS,BIKE,BILE,BILL,BIND,BIRD,BITE,BLAH,BLED,BLEW,BLOC,BLOG,BLOT,BLOW,BLUE,BLUR,BOAR,BOAT,BODY,BOIL,BOLD,BOLT,BOMB,BOND,BONE,BONY,BOOK,BOOM,BOOT,BORE,BORN,BOSS,BOTH,BOUT,BOWL,BRED,BREW,BRIG,BRIT,BROW,BULB,BULK,BULL,BUMP,BURN,BURY,BUSH,BUST,BUSY,BUTT,BUZZ,CAFE,CAGE,CAKE,CALF,CALL,CALM,CAME,CAMP,CANE,CAPE,CAPO,CARD,CARE,CARP,CART,CASE,CASH,CAST,CAVE,CEIL,CELL,CENT,CHAI,CHAO,CHAP,CHAT,CHEF,CHIA,CHIN,CHIP,CHIS,CHOP,CIEL,CITY,CLAD,CLAM,CLAN,CLAP,CLAW,CLAY,CLIP,CLOG,CLUB,CLUE,COAL,COAT,COCK,COCO,CODE,COIL,COIN,COLD,COLE,COMB,COME,CONE,COOK,COOL,COPE,COPS,COPY,CORD,CORE,CORK,CORN,COST,COSY,COUP,COVE,COZY,CRAB,CREE,CREW,CRIB,CROP,CROW,CUBE,CULT,CURB,CURE,CURL,CUTE,DAFT,DAMP,DARE,DARK,DART,DASH,DATE,DAWN,DAYS,DEAD,DEAF,DEAL,DEAR,DEBT,DECK,DEED,DEEP,DEER,DENT,DENY,DESK,DIAL,DICE,DIET,DINE,DIRE,DIRT,DISC,DISH,DISK,DIVE,DOCK,DOLE,DOLL,DOME,DONE,DOOR,DORM,DORY,DOSE,DOVE,DOWN,DRAG,DRAW,DREW,DREY,DRIB,DRIP,DROP,DRUG,DRUM,DUAL,DUCK,DUEL,DUET,DULL,DULY,DUMB,DUMP,DUSK,DUST,DUTY,DYER,EACH,EARN,EARS,EASE,EAST,EASY,EATS,ECHO,EDGE,EDIT,ELSE,ENVY,EPIC,ERIC,EURO,EVEN,EVER,EVIL,EXAM,EXIT,EYED,EYES,FACE,FACT,FADE,FAIL,FAIR,FAKE,FALL,FAME,FANG,FARE,FARM,FAST,FATE,FAVE,FAWN,FEAR,FEAT,FEED,FEEL,FEET,FELL,FELT,FILE,FILL,FILM,FIND,FINE,FIRE,FIRM,FIRS,FISH,FIST,FIVE,FLAG,FLAP,FLAT,FLAW,FLAX,FLEA,FLED,FLEE,FLEW,FLEX,FLIP,FLOW,FLUX,FOAL,FOAM,FOIL,FOLD,FOLK,FOND,FONT,FOOD,FOOL,FOOT,FORA,FORD,FORK,FORM,FORT,FOUL,FOUR,FREE,FROG,FROM,FUEL,FULL,FUND,FURY,FUSE,FUSS,GAIN,GALA,GALL,GAME,GANG,GAOL,GASP,GATE,GAVE,GAZE,GEAR,GENE,GERM,GIFT,GILL,GILT,GIRL,GIVE,GLAD,GLEE,GLEN,GLIB,GLOW,GLUE,GOAL,GOAT,GOES,GOLD,GOLF,GONE,GONG,GOOD,GORY,GOSH,GOWN,GRAB,GRAM,GRAY,GREW,GREY,GRID,GRIM,GRIN,GRIP,GRIT,GROW,GUMP,GUST,GYRE,GYRO,HAHA,HAIL,HAIR,HALF,HALL,HALO,HALT,HAND,HANG,HARD,HARE,HARM,HATE,HAUL,HAVE,HAWK,HAZE,HEAD,HEAL,HEAP,HEAR,HEAT,HECK,HEEL,HEIR,HELD,HELL,HELP,HERB,HERD,HERE,HERO,HERS,HIDE,HIGH,HIKE,HILL,HINT,HIRE,HOLD,HOLE,HOLY,HOME,HOOD,HOOK,HOPE,HORN,HOSE,HOST,HOUR,HOWL,HUGE,HULL,HUNG,HUNT,HURT,HUSH,HYMN,HYPE,ICED,ICER,ICON,IDEA,IDLE,IDLY,IDOL,IDYL,INCH,INFO,INTO,IRON,ITCH,ITEM,JACK,JAIL,JARS,JAZZ,JINX,JOBS,JOIN,JOKE,JOSH,JUMP,JUNK,JURY,JUST,KEEN,KEEP,KELP,KEPT,KICK,KILL,KIND,KING,KISS,KITE,KIWI,KNEE,KNEW,KNIT,KNOB,KNOT,KNOW,LACE,LACK,LADY,LAID,LAIR,LAKE,LAMB,LAMP,LAND,LANE,LAST,LATE,LAVA,LAWN,LAZE,LAZY,LEAD,LEAF,LEAK,LEAN,LEAP,LEER,LEFT,LEND,LENS,LENT,LESS,LEST,LEVO,LEVY,LIAR,LICE,LIED,LIFE,LIFT,LIKE,LIMB,LIME,LINE,LINK,LINT,LION,LIPS,LIRE,LISP,LIST,LIVE,LOAD,LOAF,LOAN,LOCI,LOCK,LOCO,LOFT,LOGO,LONE,LONG,LOOK,LOOP,LOPS,LORD,LORE,LOSE,LOSS,LOST,LOTS,LOUD,LOUP,LOVE,LUCK,LUMP,LUNG,LURE,LUSH,LUST,MADE,MAID,MAIL,MAIN,MAKE,MALE,MALI,MALL,MALT,MANY,MARE,MARK,MASK,MASS,MAST,MATE,MATH,MATT,MAZE,MEAL,MEAN,MEAT,MEET,MELD,MELT,MEMO,MENU,MERE,MESH,MESS,MICE,MILD,MILE,MILK,MILL,MIME,MIND,MINE,MINI,MINT,MISS,MIST,MOAT,MOCK,MODE,MOLD,MOLE,MOLT,MONK,MOOD,MOON,MOOR,MORE,MOSS,MOST,MOTH,MOVE,MUCH,MULE,MUST,MUTE,MYTH,NAIL,NAME,NAVE,NEAR,NEAT,NECK,NEED,NEON,NERD,NEST,NEWS,NEWT,NEXT,NICE,NICK,NINE,NODE,NONE,NOOK,NOON,NORM,NOSE,NOTE,NOUN,NUMB,NUTS,OATH,OATS,OBEY,OBOE,ODDS,ODOR,OGRE,OILY,OINK,OKAY,OMEN,OMIT,ONCE,ONLY,ONTO,OOZE,OPAL,OPEN,ORAL,ORCA,ORES,ORGY,OUCH,OURS,OVAL,OVEN,OVER,OWLY,PACE,PACK,PACT,PAGE,PAID,PAIL,PAIN,PAIR,PALE,PALM,PANG,PANT,PAPA,PAPS,PARA,PARE,PARK,PART,PASS,PAST,PATH,PAVE,PAWN,PEAK,PEAL,PEAR,PEAT,PECK,PECS,PEEK,PEEL,PEER,PERK,PEST,PICA,PICK,PICS,PIER,PILE,PILL,PINE,PINK,PINT,PIPE,PITY,PLAN,PLAY,PLEA,PLOT,PLOW,PLOY,PLUG,PLUM,PLUS,POEM,POET,POKE,POLE,POLK,POLL,POLO,POND,PONY,POOL,POOR,PORK,PORT,POSE,POSH,POST,POUR,PRAY,PREY,PROP,PROS,PUCE,PULL,PUMP,PUNK,PUNT,PUPA,PUPS,PURE,PUSH,QUAY,QUID,QUIT,QUIZ,RACE,RACK,RAFT,RAGE,RAID,RAIL,RAIN,RAKE,RAMP,RANK,RARE,RASH,RATE,RAZE,READ,REAL,REAP,REAR,REEF,REEL,RELY,REND,RENT,REPS,REST,RHEA,RHOS,RICE,RICH,RIDE,RIFT,RILE,RIND,RING,RIOT,RIPE,RIPS,RISE,RISK,RITE,ROAD,ROAM,ROAR,ROBE,ROCK,RODE,ROLE,ROLL,ROOF,ROOM,ROOT,ROPE,ROSE,ROSY,ROVE,RUBY,RUDE,RUIN,RULE,RUNG,RUSH,RUST,SACK,SAFE,SAGA,SAID,SAIL,SAKE,SALE,SALT,SAME,SAND,SANE,SANG,SANK,SASS,SAVE,SCAN,SCAR,SCUM,SEAL,SEAM,SEAT,SEED,SEEK,SEEM,SEEN,SELF,SELL,SEND,SENT,SERF,SEXY,SHAH,SHED,SHIP,SHOE,SHOP,SHOT,SHOW,SHUT,SICK,SIDE,SIGH,SIGN,SILK,SING,SINK,SITE,SIZE,SKAS,SKIN,SKIP,SKUA,SLAB,SLAM,SLID,SLIM,SLIP,SLOP,SLOT,SLOW,SLUM,SMUG,SNAP,SNOW,SOAP,SOAR,SODA,SOFA,SOFT,SOIL,SOLD,SOLE,SOLO,SOME,SONG,SOON,SORE,SORT,SOUL,SOUP,SOUR,SPAN,SPEC,SPIN,SPOT,SPUN,SPUR,STAB,STAR,STAY,STEM,STEP,STIR,STOP,SUCH,SUIT,SUNG,SUNK,SURE,SUSS,SWAN,SWAP,SWIM,TACK,TACO,TAIL,TAKE,TALE,TALK,TALL,TAME,TANK,TAPE,TASK,TAUT,TAXI,TEAL,TEAM,TEAR,TELL,TEND,TENT,TERM,TERN,TEST,TEXT,THAI,THAN,THAT,THAW,THEE,THEM,THEN,THEY,THIN,THIS,THOU,THUD,THUS,TIDE,TIDY,TIED,TIER,TILE,TILL,TILT,TIME,TINY,TIRE,TOAD,TOIL,TOLD,TOLL,TOMB,TONE,TOOK,TOOL,TORE,TORN,TORT,TORY,TOSS,TOUR,TOWN,TRAM,TRAP,TRAY,TREE,TREY,TRIM,TRIO,TRIP,TRON,TROY,TRUE,TSAR,TUBE,TUCK,TUNA,TUNE,TURF,TURN,TWIN,TYPE,TYRE,UGLY,UNDO,UNIT,UNTO,UPON,URGE,USED,USER,USES,VAIN,VARY,VASE,VAST,VEER,VEIL,VEIN,VEND,VENT,VERB,VERY,VEST,VETO,VIAL,VICE,VIEW,VILE,VINE,VIOL,VISA,VIVO,VOID,VOLE,VOTE,WADE,WAGE,WAIT,WAKE,WALK,WALL,WAND,WANT,WARD,WARM,WARN,WARP,WARY,WASH,WAVE,WAVY,WAXY,WEAK,WEAR,WEED,WEEK,WELD,WELL,WENT,WERE,WEST,WHAM,WHAT,WHEN,WHIP,WHOM,WIDE,WIFE,WILD,WILL,WILY,WIND,WINE,WING,WIPE,WIRE,WISE,WISH,WITH,WOLF,WOMB,WOOD,WOOL,WORD,WORE,WORK,WORM,WORN,WRAP,WREN,WRIT,XYLO,YARD,YARN,YAWN,YEAH,YEAR,YELL,YOGA,YOUR,YOWL,ZAPS,ZEAL,ZERO,ZINC,ZONE,ZOOM".Split(',');

        public static AzureButtonPuzzle Generate(int seed)
        {
            var rnd = new Random(seed);

            tryAgain:

            // Start by generating random sets S and E and calculating C from it
            var setS = generateRandomSet(rnd);
            var setE = generateRandomSet(rnd);
            if (setS.Concat(setE).Distinct().Count() != 6)
                goto tryAgain;

            var possibleWrongTs = Enumerable.Range(0, setS.Length).SelectMany(sIx => Enumerable.Range(0, setE.Length).Select(eIx => otherCard(setS[sIx], setE[eIx])));
            var candidateTs = Enumerable.Range(0, 81).Except(setS.Concat(setE).Concat(possibleWrongTs)).ToArray();
            var cardT = candidateTs[rnd.Next(0, candidateTs.Length)];
            var grid = Ut.NewArray(16, ix => ix % 4 == 3 ? trit(cardT, ix / 4) : otherVal(trit(setS[ix % 4], ix / 4), trit(setE[ix % 4], ix / 4)));

            // Pick a random solution word
            var sol = _wordList[rnd.Next(0, _wordList.Length)];

            // For each letter in the solution word, find which arrows spell out that letter
            var allArrows = sol.Select((ltr, ltrIx) => AzureButtonArrowInfo.AllArrows.Where(ar => Enumerable.Range(0, AzureButtonArrowInfo.MaxArrowLength).All(arIx => grid[ar.Coordinates[arIx].AddWrap(ltrIx, ltrIx).Index] == ((ltr - 'A' + 1) / _powersOf3[2 - arIx]) % 3)).ToArray()).ToArray();
            var allLetterData = sol
                .SelectMany((ltr, solLtrIx) => allArrows[solLtrIx].Select(ar => (solLtrIx, arrowInfo: ar, ltrs: Enumerable.Range(1, 3).Select(disp => (char) (ar.Coordinates.Select(c => grid[c.AddWrap(solLtrIx + disp, solLtrIx + disp).Index]).Aggregate(0, (p, n) => p * 3 + n) + 'A' - 1)).JoinString())))
                .Where(tup => !tup.ltrs.Contains('@'))
                .ToArray();

            // Go through the possible taboo letters to find one that works
            foreach (var tabooLetter in Enumerable.Range(0, 26).Select(x => (char) ('A' + x)).Where(x => !sol.Contains(x)).ToArray().Shuffle(rnd))
            {
                var tabooFilled = new bool[3];
                var arrows = new AzureButtonArrowInfo[4];
                var letterData = allLetterData.ToList();

                while (arrows.Any(a => a == null))
                {
                    int candidateIx;

                    if (tabooFilled.Contains(false))
                    {
                        // Pick an arrow that produces the taboo letter on one of the wrong positions
                        var candidateIxs = letterData.SelectIndexWhere(tup => Enumerable.Range(0, tup.ltrs.Length).Any(ix => !tabooFilled[ix] && tup.ltrs[ix] == tabooLetter)).ToArray();
                        if (candidateIxs.Length == 0)
                            goto busted;
                        candidateIx = candidateIxs[rnd.Next(0, candidateIxs.Length)];
                    }
                    else
                    {
                        // We’ve already got our taboo letters — pick any remaining arrow
                        if (letterData.Count == 0)
                            goto busted;
                        candidateIx = rnd.Next(0, letterData.Count);
                    }

                    var (solLtrIx, arrowInfo, ltrs) = letterData[candidateIx];
                    for (var i = 0; i < ltrs.Length; i++)
                        if (ltrs[i] == tabooLetter)
                            tabooFilled[i] = true;
                    arrows[solLtrIx] = arrowInfo;
                    letterData.RemoveAll(tup => tup.solLtrIx == solLtrIx);
                }

                // Make sure that no other letter accidentally occurs exactly three times
                var otherWords = new List<string>();
                for (var disp = 0; disp < 4; disp++)
                    otherWords.Add(arrows.Select((ar, arIx) => (char) (ar.Coordinates.Select(c => grid[c.AddWrap(arIx + disp, arIx + disp).Index]).Aggregate(0, (p, n) => 3 * p + n) + 'A' - 1)).JoinString());
                for (var solLtrIx = 0; solLtrIx < sol.Length; solLtrIx++)
                    if (otherWords.Count(w => w.Contains(sol[solLtrIx])) == 3)
                        goto busted;

                return new AzureButtonPuzzle
                {
                    SetE = setE,
                    SetS = setS,
                    CardT = cardT,
                    Grid = grid,
                    SolutionWord = sol,
                    ForbiddenLetter = tabooLetter,
                    Arrows = arrows
                };

                busted:;
            }
            goto tryAgain;
        }

        private static int trit(int card, int characteristic) => (card / _powersOf3[3 - characteristic]) % 3;
        private static int otherVal(int v1, int v2) => (6 - v1 - v2) % 3;

        private static int otherCard(int c1, int c2)
        {
            var c = 0;
            for (var characteristic = 0; characteristic < 4; characteristic++)
                c += otherVal(trit(c1, characteristic), trit(c2, characteristic)) * _powersOf3[3 - characteristic];
            return c;
        }

        private static int[] generateRandomSet(Random rnd)
        {
            var a = rnd.Next(0, 81);
            var b = rnd.Next(0, 81);
            return new[] { a, b, otherCard(a, b) };
        }
    }
}
