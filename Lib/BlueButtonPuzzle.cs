using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButtonLib
{
    public class BlueButtonPuzzle
    {
        // PUBLIC
        public PolyominoPlacement[] PolyominoSequence { get; private set; }
        public int[] PolyominoSequenceColors { get; private set; }
        public int[] PolyominoGrid { get; private set; }
        public int[] ColorStageColors { get; private set; }
        public int[] EquationOffsets { get; private set; }
        public string Word { get; private set; }
        public int[] Suits { get; private set; }
        public int[] Jumps { get; private set; }

        public const int GridWidth = 5;
        public const int GridHeight = 4;

        public static BlueButtonPuzzle GeneratePuzzle(int seed)
        {
            var rnd = new Random(seed);

            tryAgain:
            var word = _words[rnd.Next(0, _words.Length)];
            var result = GenerateRandomPolyominoSolution(rnd, word);
            if (result == null)
                goto tryAgain;

            var (generatedGrid, generatedPolys, generatedJumps) = result.Value;
            generatedPolys.Shuffle(rnd);

            // Without loss of generality, assume the first polyomino is a given
            var givenPolyominoPlacement = generatedPolys[0];
            var givenGrid = new int?[GridWidth * GridHeight];
            foreach (var cell in givenPolyominoPlacement.Polyomino.Cells)
                givenGrid[givenPolyominoPlacement.Place.AddWrap(cell).Index] = 0;
            var possiblePlacements = GetAllPolyominoPlacements()
                .Where(pl =>
                    pl.Polyomino.Cells.All(c => givenGrid[pl.Place.AddWrap(c).Index] == null) &&
                    generatedPolys.Skip(1).Any(tup => tup.Polyomino == pl.Polyomino))
                .ToArray();

            IEnumerable<(int[] solution, PolyominoPlacement[] polys)> GenerateSolutions(List<(Polyomino one, Polyomino two)> noAllowTouch)
            {
                var grid = givenGrid.ToArray();
                var placements = possiblePlacements.ToList();
                foreach (var (one, two) in noAllowTouch)
                    if (one == givenPolyominoPlacement.Polyomino)
                        placements.RemoveAll(pl => pl.Polyomino == two && pl.Touches(givenPolyominoPlacement));
                    else if (two == givenPolyominoPlacement.Polyomino)
                        placements.RemoveAll(pl => pl.Polyomino == one && pl.Touches(givenPolyominoPlacement));

                return SolvePolyominoPuzzle(grid, 1, placements, noAllowTouch);
            }

            var notAllowedToTouch = new List<(Polyomino one, Polyomino two)>();
            while (true)
            {
                var solutions = GenerateSolutions(notAllowedToTouch).Take(2).ToArray();

                // Puzzle unique!
                if (solutions.Length == 1)
                    break;

                // Find a wrong solution
                var (_, wrongAllPolys) = solutions.First(s => s.polys.Any(sPl => !generatedPolys.Contains(sPl)));
                // Find all wrong polyominoes in this wrong solution
                var wrongPolys = wrongAllPolys.Where(sPl => !generatedPolys.Contains(sPl)).ToArray();
                // Find all polyominoes that touch a wrong polyomino in the wrong solution, but do not touch the corresponding correct polyomino in the correct solution
                var touchingPolys = wrongPolys.SelectMany(wPl => wrongAllPolys
                      .Where(owPl => owPl.Touches(wPl) && !generatedPolys.First(gPl => gPl.Polyomino == wPl.Polyomino).Touches(generatedPolys.First(gPl => gPl.Polyomino == owPl.Polyomino)))
                      .Select(owPl => (one: wPl.Polyomino, two: owPl.Polyomino)))
                      .ToArray();
                if (touchingPolys.Length == 0)  // We cannot disambiguate this puzzle with a no-touch constraint
                    goto tryAgain;

                // Prefer one that isn’t already constrained
                var prefIx = touchingPolys.IndexOf(tup1 =>
                    !notAllowedToTouch.Any(tup2 => tup2.one == tup1.one || tup2.two == tup1.one) &&
                    !notAllowedToTouch.Any(tup2 => tup2.one == tup1.two || tup2.two == tup1.two));
                notAllowedToTouch.Add(touchingPolys[prefIx == -1 ? 0 : prefIx]);
            }

            // Assign the polyominoes colors
            var colors = new int[generatedPolys.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var availableColors = Enumerable.Range(0, 6).ToList();
                var already = generatedPolys.Take(i).IndexOf(prev => notAllowedToTouch.Any(tup => (tup.one == prev.Polyomino && tup.two == generatedPolys[i].Polyomino) || (tup.two == prev.Polyomino && tup.one == generatedPolys[i].Polyomino)));
                if (already != -1)
                    availableColors = new List<int> { colors[already] };
                for (var j = 0; j < i; j++)
                    if (generatedPolys[j].Touches(generatedPolys[i]))
                        availableColors.Remove(colors[j]);
                if (availableColors.Count == 0)
                    goto tryAgain;
                colors[i] = availableColors[rnd.Next(0, availableColors.Count)];
            }

            var polyColors = Enumerable.Range(0, generatedPolys.Length).Select(ix => (poly: generatedPolys[ix], color: colors[ix])).ToArray();

            var colorIterations = 0;
            retryColors:
            colorIterations++;
            if (colorIterations > 10)
                goto tryAgain;

            polyColors.Shuffle(rnd);

            var keyPoly = polyColors.First(pc => pc.poly.Polyomino.Cells.Any(c => pc.poly.Place.AddWrap(c).Index == 0));
            var firstKeyColorIx = (polyColors.IndexOf(keyPoly) + polyColors.Length - 1) % polyColors.Length;

            var eqPolyIx = keyPoly.poly.Polyomino.Cells.IndexOf(c => keyPoly.poly.Place.AddWrap(c).Index == 0);
            var suitsTargetPermutation = Enumerable.Range(0, 4).ToArray().Shuffle(rnd);
            while (suitsTargetPermutation.IndexOf(3) == eqPolyIx)
                suitsTargetPermutation.Shuffle(rnd);
            var suitPartialPermutationColor = new[] { "012", "021", "102", "120", "201", "210" }.IndexOf(suitsTargetPermutation.Where(suit => suit != 3).JoinString());

            var eqDiamondsIx = suitsTargetPermutation.IndexOf(3);

            var colorStageColors = new List<int>
            {
                polyColors[firstKeyColorIx].color,
                polyColors[(firstKeyColorIx + 1) % polyColors.Length].color,
                polyColors[(firstKeyColorIx + 2) % polyColors.Length].color,
                suitPartialPermutationColor
            };
            var numColorsAv = Enumerable.Range(6, 4).Where(n => n != eqDiamondsIx + 1 && n != eqPolyIx + 1).ToArray();
            var numColors = numColorsAv[rnd.Next(0, numColorsAv.Length)];
            while (colorStageColors.Count < numColors)
            {
                var colorsAv = Enumerable.Range(0, 6).ToList();
                if (colorStageColors[colorStageColors.Count - 2] == colorStageColors[0] && colorStageColors[colorStageColors.Count - 1] == colorStageColors[1])
                    colorsAv.Remove(colorStageColors[2]);
                colorStageColors.Add(colorsAv[rnd.Next(0, colorsAv.Count)]);
            }

            for (var i = 1; i < colorStageColors.Count; i++)
                for (var j = 0; j < polyColors.Length; j++)
                    if (colorStageColors[i] == polyColors[j].color &&
                        colorStageColors[(i + 1) % colorStageColors.Count] == polyColors[(j + 1) % polyColors.Length].color &&
                        colorStageColors[(i + 2) % colorStageColors.Count] == polyColors[(j + 2) % polyColors.Length].color)
                        goto retryColors;

            var eqExtraCandidates = Enumerable.Range(1, 9).Except(new[] { colorStageColors.Count, eqDiamondsIx + 1, eqPolyIx + 1 }).ToArray();

            return new BlueButtonPuzzle
            {
                PolyominoSequence = polyColors.Select(tup => tup.poly).ToArray(),
                PolyominoSequenceColors = polyColors.Select(tup => tup.color).ToArray(),
                PolyominoGrid = generatedGrid,
                ColorStageColors = colorStageColors.ToArray(),
                EquationOffsets = new[] { colorStageColors.Count, eqDiamondsIx + 1, eqPolyIx + 1, eqExtraCandidates[rnd.Next(0, eqExtraCandidates.Length)] },
                Suits = suitsTargetPermutation,
                Jumps = Enumerable.Range(0, 4).Select(pos => (generatedJumps >> (2 * pos)) & 0x03).ToArray(),
                Word = word
            };
        }

        // PRIVATE
        private static readonly string[] _words = new[] { "ABIDE", "ABORT", "ABOUT", "ABOVE", "ABYSS", "ACIDS", "ACORN", "ACRES", "ACTED", "ACTOR", "ACUTE", "ADDER", "ADDLE", "ADIEU", "ADIOS", "ADMIN", "ADMIT", "ADOPT", "ADORE", "ADORN", "ADULT", "AFFIX", "AFTER", "AGILE", "AGING", "AGORA", "AGREE", "AHEAD", "AIDED", "AIMED", "AIOLI", "AIRED", "AISLE", "ALARM", "ALBUM", "ALIAS", "ALIBI", "ALIEN", "ALIGN", "ALIKE", "ALIVE", "ALLAY", "ALLEN", "ALLOT", "ALLOY", "ALOFT", "ALONE", "ALONG", "ALOOF", "ALOUD", "ALPHA", "ALTAR", "ALTER", "AMASS", "AMAZE", "AMBLE", "AMINO", "AMISH", "AMISS", "AMONG", "AMPLE", "AMUSE", "ANGLE", "ANGLO", "ANGRY", "ANGST", "ANIME", "ANION", "ANISE", "ANKLE", "ANNEX", "ANNOY", "ANNUL", "ANTIC", "ANVIL", "AORTA", "APNEA", "APPLE", "APRON", "AREAS", "ARENA", "ARGUE", "ARISE", "ARMED", "ARMOR", "AROSE", "ASHEN", "ASHES", "ASIAN", "ASIDE", "ASSET", "ASTER", "ASTIR", "ATOLL", "ATOMS", "ATONE", "ATTIC", "AUDIO", "AUDIT", "AUGUR", "AUNTY", "AVAIL", "AVIAN", "AVOID", "AWAIT", "AWAKE", "AWARE", "AWASH", "AXIAL", "AXIOM", "AXION", "AZTEC", "BIBLE", "BIDET", "BIGHT", "BILGE", "BILLS", "BINGE", "BINGO", "BIOME", "BIRCH", "BIRDS", "BIRTH", "BISON", "BITER", "BLADE", "BLAME", "BLAND", "BLARE", "BLAZE", "BLEAT", "BLEED", "BLEEP", "BLIMP", "BLIND", "BLING", "BLINK", "BLISS", "BLITZ", "BLOND", "BLOOM", "BLOOP", "BLUES", "BLUNT", "BLUSH", "BOGGY", "BOGUS", "BOLTS", "BONDS", "BONED", "BONER", "BONES", "BONNY", "BONUS", "BOOST", "BOOTH", "BOOTS", "BORAX", "BORED", "BORER", "BORNE", "BORON", "BOTCH", "BOUGH", "BOULE", "BRACE", "BRAID", "BRAIN", "BRAKE", "BRAND", "BRASH", "BRASS", "BRAVE", "BRAWL", "BRAWN", "BRAZE", "BREAD", "BREAK", "BREAM", "BREED", "BRIAR", "BRIBE", "BRICK", "BRIDE", "BRIEF", "BRIER", "BRINE", "BRING", "BRINK", "BRINY", "BRISK", "BROIL", "BRONX", "BROOM", "BROTH", "BRUNT", "BRUSH", "BRUTE", "BUCKS", "BUDDY", "BUDGE", "BUGGY", "BUILD", "BUILT", "BULBS", "BULGE", "BULLS", "BUMPY", "BUNCH", "BUNNY", "BUSES", "BUZZY", "BYLAW", "BYWAY", "CABBY", "CABIN", "CABLE", "CACHE", "CAIRN", "CAKES", "CALLS", "CALVE", "CALYX", "CAMPS", "CAMPY", "CANAL", "CANED", "CANNY", "CANON", "CARDS", "CARVE", "CASED", "CASES", "CASTE", "CATCH", "CAULK", "CAUSE", "CAVES", "CEASE", "CEDED", "CELLS", "CENTS", "CHAFE", "CHAFF", "CHAIN", "CHAIR", "CHALK", "CHAMP", "CHANT", "CHAOS", "CHAPS", "CHARM", "CHART", "CHARY", "CHASE", "CHASM", "CHEAP", "CHEAT", "CHECK", "CHEMO", "CHESS", "CHEST", "CHICK", "CHIDE", "CHILD", "CHILI", "CHILL", "CHIME", "CHINA", "CHIPS", "CHORD", "CHORE", "CHOSE", "CHUCK", "CHUNK", "CHUTE", "CINCH", "CITED", "CITES", "CIVET", "CIVIC", "CIVIL", "CLADE", "CLAIM", "CLANK", "CLASH", "CLASS", "CLAWS", "CLEAN", "CLEAR", "CLEAT", "CLICK", "CLIFF", "CLIMB", "CLING", "CLONE", "CLOSE", "CLOTH", "CLOUD", "CLOUT", "CLOVE", "CLUBS", "CLUCK", "CLUES", "CLUNG", "CLUNK", "COINS", "COLIC", "COLON", "COLOR", "COMAL", "COMES", "COMIC", "COMMA", "CONCH", "CONIC", "CORAL", "CORGI", "CORNY", "CORPS", "COSTS", "COTTA", "COUCH", "COUGH", "COULD", "COUNT", "COVEN", "COYLY", "CRACK", "CRANE", "CRANK", "CRASH", "CRASS", "CRATE", "CRAVE", "CREAK", "CREAM", "CREED", "CREWS", "CRIED", "CRIES", "CRIME", "CRONE", "CROPS", "CROSS", "CRUDE", "CRUEL", "CRUSH", "CUBBY", "CUBIT", "CUMIN", "CUTIE", "CYCLE", "CYNIC", "CZECH", "DACHA", "DAILY", "DALLY", "DANCE", "DATED", "DATES", "DATUM", "DEALS", "DEALT", "DEATH", "DEBIT", "DEBTS", "DEBUG", "DEBUT", "DECAF", "DECAL", "DECOR", "DECOY", "DEEDS", "DEIST", "DELFT", "DELVE", "DEMUR", "DENIM", "DENSE", "DESKS", "DETER", "DETOX", "DEUCE", "DEVIL", "DICED", "DIETS", "DIGIT", "DIMLY", "DINAR", "DINER", "DINGY", "DIRTY", "DISCO", "DISCS", "DISKS", "DITCH", "DITTY", "DITZY", "DIVAN", "DIVED", "DIVER", "DIVOT", "DIVVY", "DOGGY", "DOGMA", "DOING", "DOLLS", "DOMED", "DONOR", "DONUT", "DOORS", "DORIC", "DOSED", "DOSES", "DOTTY", "DOUGH", "DOUSE", "DRAFT", "DRAIN", "DRAMA", "DRANK", "DREAM", "DRESS", "DRIED", "DRIER", "DRIFT", "DRILL", "DRILY", "DRINK", "DRIVE", "DROLL", "DRONE", "DROPS", "DROVE", "DRUGS", "DRUMS", "DRUNK", "DRYER", "DUCAT", "DUCKS", "DUMMY", "DUNCE", "DUNES", "DUTCH", "DUVET", "DWARF", "DWELL", "DYING", "EAGLE", "EARED", "EARLY", "EARTH", "EASED", "EASEL", "EATEN", "ECLAT", "EDEMA", "EDICT", "EDIFY", "EGRET", "EIDER", "EIGHT", "ELATE", "ELDER", "ELECT", "ELIDE", "ELITE", "ELUDE", "EMCEE", "EMOTE", "ENACT", "ENEMA", "ENNUI", "ENSUE", "ENTER", "ENVOY", "ETHIC", "ETHOS", "ETUDE", "EVADE", "EVICT", "EXACT", "EXALT", "EXAMS", "EXILE", "EXIST", "EXTRA", "EXUDE", "FAILS", "FAINT", "FAIRY", "FAITH", "FALLS", "FALSE", "FAMED", "FANCY", "FATAL", "FATED", "FATTY", "FATWA", "FAULT", "FAVOR", "FECAL", "FEINT", "FETAL", "FIBRE", "FIFTH", "FIFTY", "FIGHT", "FILCH", "FILED", "FILET", "FILLE", "FILLS", "FILLY", "FILMS", "FILMY", "FILTH", "FINAL", "FINDS", "FINED", "FINNY", "FIRED", "FIRMS", "FIRST", "FISTS", "FLAIL", "FLAIR", "FLATS", "FLEET", "FLING", "FLIRT", "FLOOR", "FLORA", "FLOUT", "FLUID", "FLUNG", "FLYBY", "FOGGY", "FOIST", "FOLIC", "FOLIO", "FOLLY", "FONTS", "FORAY", "FORGO", "FORMS", "FORTE", "FORTH", "FORTY", "FORUM", "FOUNT", "FOVEA", "FRAIL", "FRAUD", "FREED", "FRIED", "FRILL", "FRISK", "FRONT", "FRUIT", "FUELS", "FULLY", "FUNNY", "FUSED", "FUTON", "FUZZY", "GHOUL", "GIANT", "GIDDY", "GIFTS", "GIMPY", "GIRLS", "GIRLY", "GIRTH", "GIVEN", "GIZMO", "GLAND", "GLEAM", "GLEAN", "GLIAL", "GLINT", "GLOOM", "GLORY", "GLUED", "GLUON", "GOING", "GOLLY", "GOOFY", "GOOPY", "GRAFT", "GRAIN", "GRAND", "GRANT", "GRASS", "GRATE", "GRAVE", "GRAVY", "GREAT", "GREED", "GREEN", "GREET", "GRILL", "GRIME", "GRIMY", "GRIND", "GRIPS", "GROIN", "GROOM", "GROSS", "GROUT", "GRUEL", "GRUMP", "GRUNT", "GUANO", "GUARD", "GUAVA", "GUEST", "GUILD", "GUILT", "GUISE", "GULLS", "GULLY", "GUMMY", "GUNKY", "GUNNY", "GUSHY", "GUSTY", "GUTSY", "GYRUS", "HABIT", "HAIKU", "HAIRS", "HAIRY", "HALAL", "HALVE", "HAMMY", "HANDS", "HANDY", "HANGS", "HARDY", "HAREM", "HARPY", "HARSH", "HASTE", "HASTY", "HATCH", "HATED", "HATES", "HAUNT", "HAVEN", "HAZEL", "HEADS", "HEADY", "HEARD", "HEARS", "HEART", "HEATH", "HEAVE", "HEAVY", "HEELS", "HEIRS", "HEIST", "HELIX", "HELLO", "HENRY", "HILLS", "HILLY", "HINDI", "HINDU", "HINTS", "HIRED", "HITCH", "HOBBY", "HOIST", "HOLLY", "HOMED", "HONOR", "HORNS", "HORSE", "HOSEL", "HOTLY", "HOUND", "HUBBY", "HUGGY", "HULLO", "HUMAN", "HUMID", "HUMOR", "ICHOR", "ICILY", "ICING", "ICONS", "IDEAL", "IDEAS", "IDIOM", "IDIOT", "IDLED", "IDYLL", "IGLOO", "ILIAC", "ILIUM", "IMAGO", "IMBUE", "IMPLY", "INANE", "INCAN", "INCUS", "INDEX", "INDIA", "INDIE", "INFRA", "INGOT", "INLAY", "INLET", "INPUT", "INSET", "INTRO", "INUIT", "IONIC", "IRISH", "IRONY", "ISLET", "ISSUE", "ITEMS", "IVORY", "JOINS", "JOINT", "JOULE", "JUMBO", "JUNTA", "KANJI", "KARAT", "KARMA", "KIDDO", "KILLS", "KINDA", "KINDS", "KINGS", "KITTY", "KNAVE", "KNEES", "KNELT", "KNIFE", "KNOBS", "KNOLL", "KNOTS", "KUDOS", "KUDZU", "LABOR", "LACED", "LACKS", "LADLE", "LAITY", "LAMBS", "LAMPS", "LANDS", "LANES", "LAPIN", "LAPSE", "LARGE", "LARVA", "LASER", "LASSO", "LATCH", "LATER", "LATHE", "LATIN", "LATTE", "LAUGH", "LAWNS", "LAYER", "LAYUP", "LEACH", "LEADS", "LEAFY", "LEANT", "LEAPT", "LEARN", "LEASE", "LEASH", "LEAVE", "LEDGE", "LEECH", "LEGGY", "LEMMA", "LEMON", "LEMUR", "LIANA", "LIDAR", "LIEGE", "LIFTS", "LIGHT", "LIKEN", "LIKES", "LILAC", "LIMBO", "LIMBS", "LIMIT", "LINED", "LINEN", "LINER", "LINES", "LINGO", "LINKS", "LIONS", "LIPID", "LISTS", "LITER", "LITRE", "LIVED", "LIVEN", "LIVER", "LIVES", "LIVID", "LLAMA", "LOBBY", "LOFTY", "LOGIC", "LOGON", "LOLLY", "LONER", "LOONY", "LOOPS", "LOOPY", "LOOSE", "LORDS", "LORRY", "LOSER", "LOSES", "LOTTO", "LOTUS", "LOUSE", "LOVED", "LOVER", "LOVES", "LOYAL", "LUCID", "LUCRE", "LUMEN", "LUMPS", "LUMPY", "LUNAR", "LUNCH", "LUNGE", "LUNGS", "LUSTY", "LYING", "LYMPH", "LYNCH", "LYRIC", "MADAM", "MADLY", "MAGIC", "MAGMA", "MAINS", "MAJOR", "MALAY", "MALTA", "MAMBO", "MANGO", "MANGY", "MANIA", "MANIC", "MANLY", "MANOR", "MASKS", "MATCH", "MATED", "MATHS", "MATTE", "MAVEN", "MAXIM", "MAYAN", "MAYOR", "MEALS", "MEANS", "MEANT", "MEATY", "MEDAL", "MEDIA", "MEDIC", "MEETS", "MELON", "MESON", "METAL", "MICRO", "MIDST", "MIGHT", "MILES", "MILLS", "MIMIC", "MINCE", "MINDS", "MINED", "MINES", "MINOR", "MINTY", "MINUS", "MIRED", "MIRTH", "MISTY", "MITRE", "MOGUL", "MOIST", "MOLAR", "MOLDY", "MONTH", "MOONY", "MOORS", "MOOSE", "MORAL", "MORAY", "MORPH", "MOTEL", "MOTIF", "MOTOR", "MOTTO", "MOULD", "MOUND", "MOUNT", "MOUSE", "MOUTH", "MOVED", "MOVIE", "MUCUS", "MUDDY", "MUGGY", "MULCH", "MULTI", "MUMMY", "MUNCH", "MUSED", "MUSIC", "MUSTY", "MUTED", "MUZZY", "MYTHS", "NADIR", "NAILS", "NAIVE", "NAMED", "NAMES", "NANNY", "NARCO", "NASAL", "NATAL", "NATTY", "NAVAL", "NAVEL", "NEATH", "NECKS", "NEEDS", "NEEDY", "NEIGH", "NESTS", "NEWLY", "NEXUS", "NICER", "NICHE", "NIECE", "NIFTY", "NIGHT", "NIGRA", "NINTH", "NITRO", "NOBLE", "NOBLY", "NOISE", "NOMAD", "NOMES", "NONCE", "NOOSE", "NORMS", "NORSE", "NORTH", "NOSES", "NOTCH", "NOTED", "NOTES", "NOVEL", "NUDGE", "NUTTY", "NYLON", "NYMPH", "OFFER", "OFTEN", "OILED", "OLDER", "OLDIE", "OLIVE", "OMANI", "ONION", "ONSET", "OOMPH", "OPINE", "OPIUM", "OPTIC", "ORBIT", "ORDER", "OTHER", "OTTER", "OUGHT", "OUNCE", "OUTDO", "OUTER", "OVOID", "OVULE", "OXBOW", "OXIDE", "PHASE", "PHONE", "PHOTO", "PIANO", "PIECE", "PIGGY", "PILAF", "PILED", "PILLS", "PILOT", "PINCH", "PINTS", "PISTE", "PITCH", "PIVOT", "PIXEL", "PIXIE", "PLACE", "PLAIN", "PLAIT", "PLANE", "PLANS", "PLANT", "PLATE", "PLAYS", "PLEAS", "PLEAT", "PLOTS", "PLUMB", "PLUME", "PLUMP", "POINT", "POLAR", "POLIO", "POLLS", "POLYP", "PONDS", "POOLS", "PORCH", "PORTS", "POSED", "POSIT", "POSTS", "POUCH", "PREEN", "PRICE", "PRICY", "PRIDE", "PRIMA", "PRIME", "PRIMP", "PRINT", "PRION", "PRIOR", "PRISE", "PRISM", "PRIVY", "PRIZE", "PROMO", "PRONE", "PRONG", "PROOF", "PROSE", "PROUD", "PROVE", "PRUDE", "PRUNE", "PUDGY", "PULLS", "PUNCH", "PUNIC", "PYLON", "QUAIL", "QUALM", "QUASI", "QUEEN", "QUELL", "QUILL", "QUILT", "QUINT", "RABBI", "RADAR", "RADIO", "RAGGY", "RAIDS", "RAILS", "RAINY", "RAISE", "RALLY", "RANCH", "RANGE", "RANGY", "RATED", "RATIO", "RATTY", "RAZOR", "REACH", "REACT", "READS", "REALM", "REARM", "RECAP", "RECON", "RECTO", "REDLY", "REEDY", "REHAB", "REINS", "RELIC", "REMIT", "RENTS", "RESTS", "RETRO", "RHINO", "RIDGE", "RIFLE", "RIGHT", "RIGID", "RIGOR", "RILED", "RINGS", "RINSE", "RIOTS", "RISEN", "RISES", "RISKS", "RITZY", "RIVAL", "RIVEN", "RIVET", "ROBOT", "ROILY", "ROLLS", "ROMAN", "ROOFS", "ROOMS", "ROOTS", "ROSIN", "ROTOR", "ROUGE", "ROUGH", "ROUTE", "ROYAL", "RUDDY", "RUGBY", "RUINS", "RULED", "RUMBA", "RUMMY", "RUMOR", "RUNIC", "RUNNY", "RUNTY", "SABLE", "SADLY", "SAFER", "SAGGY", "SAILS", "SAINT", "SALAD", "SALES", "SALLY", "SALON", "SALSA", "SALTS", "SALTY", "SALVE", "SAMBA", "SANDS", "SANDY", "SATED", "SATIN", "SATYR", "SAUCE", "SAUCY", "SAUDI", "SAUNA", "SAVED", "SAVER", "SAVOR", "SAVVY", "SAXON", "SCALD", "SCALE", "SCALP", "SCALY", "SCAMP", "SCANT", "SCAPE", "SCARE", "SCARF", "SCARP", "SCARS", "SCARY", "SCENE", "SCENT", "SCHMO", "SCOFF", "SCOLD", "SCONE", "SCOOP", "SCOOT", "SCOPE", "SCORE", "SCORN", "SCOTS", "SCOUR", "SCOUT", "SCRAM", "SCRAP", "SCREE", "SCREW", "SCRIM", "SCRIP", "SCRUB", "SCRUM", "SCUBA", "SCULL", "SEALS", "SEAMS", "SEAMY", "SEATS", "SEDAN", "SEEDS", "SEEDY", "SEEMS", "SEGUE", "SEIZE", "SELLS", "SENDS", "SENSE", "SETUP", "SEXES", "SHADE", "SHADY", "SHAFT", "SHAKE", "SHALE", "SHALL", "SHAME", "SHANK", "SHAPE", "SHARD", "SHARE", "SHARP", "SHAVE", "SHAWL", "SHEAF", "SHEAR", "SHEEN", "SHEET", "SHELF", "SHELL", "SHIFT", "SHILL", "SHINE", "SHINY", "SHIPS", "SHIRE", "SHIRT", "SHONE", "SHOOT", "SHOPS", "SHORE", "SHORN", "SHORT", "SHOTS", "SHOUT", "SHOVE", "SHUNT", "SHUSH", "SIDES", "SIDLE", "SIEGE", "SIGHT", "SIGIL", "SIGNS", "SILLY", "SILTY", "SINCE", "SINEW", "SINGE", "SINGS", "SINUS", "SITAR", "SITES", "SIXTH", "SIXTY", "SIZED", "SIZES", "SKALD", "SKANK", "SKATE", "SKEIN", "SKIER", "SKIES", "SKIFF", "SKILL", "SKIMP", "SKINS", "SKIRT", "SKULL", "SLABS", "SLAIN", "SLAKE", "SLANG", "SLANT", "SLASH", "SLATE", "SLEEP", "SLEET", "SLICE", "SLIDE", "SLIME", "SLIMY", "SLING", "SLINK", "SLOPE", "SLOSH", "SLOTH", "SLOTS", "SLUMP", "SLUSH", "SLYLY", "SMALL", "SMART", "SMASH", "SMEAR", "SMELL", "SMELT", "SMILE", "SMITE", "SNAIL", "SNAKE", "SNARE", "SNARL", "SNEER", "SNIDE", "SNIFF", "SNIPE", "SNOOP", "SNORE", "SNORT", "SNOUT", "SOFTY", "SOGGY", "SOILS", "SOLAR", "SOLID", "SOLVE", "SONAR", "SONGS", "SONIC", "SOOTH", "SORRY", "SORTS", "SOUGH", "SOULS", "SOUTH", "STAFF", "STAGE", "STAIN", "STAIR", "STAKE", "STALE", "STALL", "STAMP", "STAND", "START", "STASH", "STATE", "STEAD", "STEAL", "STEAM", "STEEL", "STEEP", "STEER", "STEMS", "STENO", "STIFF", "STILE", "STILL", "STILT", "STING", "STINK", "STINT", "STOIC", "STOLE", "STOMP", "STONE", "STONY", "STOOL", "STOOP", "STOPS", "STORE", "STORK", "STORM", "STORY", "STOUT", "STOVE", "STRAP", "STRAW", "STREP", "STREW", "STRIP", "STRUM", "STRUT", "STUCK", "STUDY", "STUMP", "STUNT", "SUAVE", "SUEDE", "SUITE", "SUITS", "SULLY", "SUNNY", "SUNUP", "SUSHI", "SWALE", "SWAMI", "SWAMP", "SWANK", "SWANS", "SWARD", "SWARM", "SWASH", "SWATH", "SWAZI", "SWEAR", "SWEAT", "SWELL", "SWIFT", "SWILL", "SWINE", "SWING", "SWIPE", "SWIRL", "SWISH", "SWISS", "SWOON", "SWOOP", "SWORD", "SWORE", "SWORN", "SWUNG", "TACIT", "TAFFY", "TAILS", "TAINT", "TAKEN", "TALLY", "TALON", "TAMED", "TAMIL", "TANGO", "TANGY", "TARDY", "TAROT", "TARRY", "TASKS", "TATTY", "TAUNT", "TAWNY", "TAXES", "TAXON", "TEACH", "TEAMS", "TEARS", "TEARY", "TEASE", "TECHY", "TEDDY", "TEENS", "TEENY", "TEETH", "TELLS", "TELLY", "TENOR", "TENSE", "TENTH", "TENTS", "TEXAS", "THANK", "THEIR", "THEME", "THESE", "THETA", "THIGH", "THINE", "THING", "THINK", "THIRD", "THONG", "THORN", "THOSE", "THUMB", "TIARA", "TIBIA", "TIDAL", "TIGHT", "TILDE", "TILED", "TILES", "TILTH", "TIMED", "TIMES", "TIMID", "TINES", "TINNY", "TIPSY", "TIRED", "TITLE", "TOMMY", "TONAL", "TONED", "TONGS", "TONIC", "TONNE", "TOOLS", "TOONS", "TOOTH", "TOPIC", "TOPSY", "TOQUE", "TORCH", "TORSO", "TORTE", "TORUS", "TOTAL", "TOTEM", "TOUCH", "TOXIC", "TOXIN", "TRACT", "TRAIL", "TRAIN", "TRAIT", "TRAMS", "TRAWL", "TREAD", "TREAT", "TRIAD", "TRIAL", "TRIED", "TRIKE", "TRILL", "TRITE", "TROLL", "TROOP", "TROUT", "TRUCE", "TRUCK", "TRULY", "TRUST", "TRUTH", "TUBBY", "TULIP", "TUMMY", "TUNED", "TUNIC", "TUTEE", "TUTOR", "TWANG", "TWEAK", "TWINS", "TWIRL", "TWIST", "TYING", "UDDER", "ULCER", "ULNAR", "ULTRA", "UMBRA", "UNCAP", "UNCLE", "UNCUT", "UNDER", "UNDUE", "UNFED", "UNFIT", "UNHIP", "UNIFY", "UNION", "UNITE", "UNITS", "UNITY", "UNLIT", "UNMET", "UNSAY", "UNTIE", "UNTIL", "UNZIP", "USAGE", "USHER", "USING", "USUAL", "UTTER", "UVULA", "VAGUE", "VALET", "VALID", "VALOR", "VALUE", "VAPOR", "VAULT", "VAUNT", "VEDIC", "VEINS", "VEINY", "VENAL", "VENOM", "VICAR", "VIEWS", "VIGIL", "VIGOR", "VILLA", "VINES", "VINYL", "VIRAL", "VIRUS", "VISIT", "VISOR", "VITAL", "VIVID", "VIXEN", "VOGUE", "VOTED", "VOUCH", "VROOM", "WAGON", "WAIST", "WAITS", "WAIVE", "WALKS", "WALLS", "WALTZ", "WANTS", "WARDS", "WARES", "WARNS", "WASTE", "WATCH", "WAVED", "WAVES", "WAXEN", "WEARS", "WEARY", "WEAVE", "WEBBY", "WELLS", "WETLY", "WHALE", "WHEAT", "WHEEL", "WHICH", "WHILE", "WHINE", "WHITE", "WHORL", "WHOSE", "WIDTH", "WIELD", "WILLS", "WIMPY", "WINCE", "WINCH", "WINDS", "WINES", "WINGS", "WITCH", "WITTY", "WIVES", "WOMAN", "WORDS", "WORKS", "WORLD", "WORMS", "WORMY", "WORSE", "WORST", "WORTH", "WOULD", "WOUND", "XENON", "YARDS", "YAWNS", "YEARN", "YEARS", "YOUNG", "YOUTH", "YUCCA", "YUMMY", "ZILCH", "ZINGY", "ZONAL", "ZONES" };

        private static IEnumerable<(int[] solution, PolyominoPlacement[] polys)> SolvePolyominoPuzzle(
            int?[] sofar,
            int pieceIx,
            List<PolyominoPlacement> possiblePlacements,
            IEnumerable<(Polyomino one, Polyomino two)> notAllowedToTouch = null,
            List<PolyominoPlacement> polysSofar = null)
        {
            polysSofar ??= new List<PolyominoPlacement>();
            Coord? bestCell = null;
            int[] bestPlacementIxs = null;

            foreach (var tCell in Coord.Cells(GridWidth, GridHeight))
            {
                if (sofar[tCell.Index] != null)
                    continue;
                var tPossiblePlacementIxs = possiblePlacements.SelectIndexWhere(pl => pl.Polyomino.Has((tCell.X - pl.Place.X + GridWidth) % GridWidth, (tCell.Y - pl.Place.Y + GridHeight) % GridHeight)).ToArray();
                if (tPossiblePlacementIxs.Length == 0)
                    yield break;
                if (bestPlacementIxs == null || tPossiblePlacementIxs.Length < bestPlacementIxs.Length)
                {
                    bestCell = tCell;
                    bestPlacementIxs = tPossiblePlacementIxs;
                }
                if (tPossiblePlacementIxs.Length == 1)
                    goto shortcut;
            }

            if (bestPlacementIxs == null)
            {
                yield return (sofar.Select(i => i.Value).ToArray(), polysSofar.ToArray());
                yield break;
            }

            shortcut:
            var cell = bestCell.Value;

            foreach (var placementIx in bestPlacementIxs.Reverse())
            {
                var placement = possiblePlacements[placementIx];
                var (poly, place) = placement;
                possiblePlacements.RemoveAt(placementIx);

                foreach (var c in poly.Cells)
                    sofar[place.AddWrap(c).Index] = pieceIx;
                polysSofar.Add(new PolyominoPlacement(poly, place));

                var newPlacements = possiblePlacements
                    .Where(p => p.Polyomino != poly && p.Polyomino.Cells.All(c => sofar[p.Place.AddWrap(c).Index] == null))
                    .ToList();
                if (notAllowedToTouch != null)
                {
                    foreach (var (one, two) in notAllowedToTouch)
                        if (one == poly)
                            newPlacements.RemoveAll(pl => pl.Polyomino == two && pl.Touches(placement));
                        else if (two == poly)
                            newPlacements.RemoveAll(pl => pl.Polyomino == one && pl.Touches(placement));
                }

                foreach (var solution in SolvePolyominoPuzzle(sofar, pieceIx + 1, newPlacements, notAllowedToTouch, polysSofar))
                    yield return solution;

                polysSofar.RemoveAt(polysSofar.Count - 1);
                foreach (var c in poly.Cells)
                    sofar[place.AddWrap(c).Index] = null;
            }
        }

        private static Polyomino[] GetAllPolyominoes()
        {
            var basePolyominoes = Ut.NewArray(
                // domino
                "##",

                // triominoes
                "###",
                "##,#",

                // tetrominoes
                "####",     // I
                "##,##",    // O
                "###,#",    // L
                "##,.##",   // S
                "###,.#",   // T

                // pentominoes
                ".##,##,.#",    // F
                "#####",        // I
                "####,#",       // L
                "##,.###",      // N
                "##,###",       // P
                "###,.#,.#",    // T
                "###,#.#",      // U
                "###,#,#",      // V
                ".##,##,#",     // W
                ".#,###,.#",    // X
                "####,.#",      // Y
                "##,.#,.##"     // Z
            );

            return basePolyominoes
                .Select(p => new Polyomino(p))
                .SelectMany(p => new[] { p, p.RotateClockwise(), p.RotateClockwise().RotateClockwise(), p.RotateClockwise().RotateClockwise().RotateClockwise() })
                .SelectMany(p => new[] { p, p.Reflect() })
                .Distinct()
                .Where(poly => !poly.Cells.Any(c => c.X >= GridWidth || c.Y >= GridHeight)
                    && !poly.Cells.Any(c =>
                        (!poly.Has(c.X + 1, c.Y) && poly.Has((c.X + 1) % GridWidth, c.Y)) ||
                        (!poly.Has(c.X - 1, c.Y) && poly.Has((c.X + GridWidth - 1) % GridWidth, c.Y)) ||
                        (!poly.Has(c.X, c.Y + 1) && poly.Has(c.X, (c.Y + 1) % GridHeight)) ||
                        (!poly.Has(c.X, c.Y - 1) && poly.Has(c.X, (c.Y + GridHeight - 1) % GridHeight))))
                .ToArray();
        }

        private static List<PolyominoPlacement> GetAllPolyominoPlacements() =>
            (from poly in GetAllPolyominoes() from place in Enumerable.Range(0, GridWidth * GridHeight) select new PolyominoPlacement(poly, new Coord(GridWidth, GridHeight, place))).ToList();

        private static readonly Dictionary<char, int> _polyominoAlphabet = new Dictionary<char, int>
        {
            ['E'] = 0b11010,
            ['S'] = 0b11011,
            ['A'] = 0b11110,
            ['R'] = 0b01110,
            ['O'] = 0b01011,
            ['T'] = 0b01101,
            ['L'] = 0b00111,
            ['I'] = 0b11101,
            ['N'] = 0b10111,
            ['D'] = 0b10110,
            ['C'] = 0b11100,
            ['U'] = 0b10011,
            ['H'] = 0b11001,
            ['P'] = 0b01010,
            ['M'] = 0b10101,
            ['G'] = 0b01001,
            ['Y'] = 0b00110,
            ['B'] = 0b00011,
            ['F'] = 0b01100,
            ['W'] = 0b11000,
            ['K'] = 0b10010,
            ['V'] = 0b00101,
            ['X'] = 0b10001,
            ['Z'] = 0b10100,
            ['J'] = 0b00010,
            ['Q'] = 0b01000
        };

        private static (int[] solution, PolyominoPlacement[] polys, int jumps)? GenerateRandomPolyominoSolution(Random rnd, string word)
        {
            var allPlacements = GetAllPolyominoPlacements().Shuffle(rnd);
            var allJumps = Enumerable.Range(0, 256).ToArray().Shuffle(rnd);     // 4⁴ = 256
            foreach (var jumps in allJumps)
            {
                var letterPositions = Enumerable.Range(0, word.Length).Select(i => new Coord(GridWidth, GridHeight, i).AddYWrap((jumps >> (2 * i)) & 0x03)).ToArray();
                var placements = allPlacements.Where(tup => letterPositions.All((cell, ix) =>
                {
                    var (poly, place) = tup;
                    var dx = (cell.X - place.X + GridWidth) % GridWidth;
                    var dy = (cell.Y - place.Y + GridHeight) % GridHeight;
                    if (!poly.Has(dx, dy))
                        return true;
                    var encoding = _polyominoAlphabet[word[ix]];
                    return
                        poly.Has(dx, dy - 1) != ((encoding & 8) != 0) &&
                        poly.Has(dx + 1, dy) != ((encoding & 4) != 0) &&
                        poly.Has(dx, dy + 1) != ((encoding & 2) != 0) &&
                        poly.Has(dx - 1, dy) != ((encoding & 1) != 0) &&
                        (poly.Cells.Count() == 5) == ((encoding & 16) != 0);
                })).ToList();

                var solutionTup = SolvePolyominoPuzzle(new int?[GridWidth * GridHeight], 0, placements).FirstOrNull();
                if (solutionTup != null)
                    return (solutionTup.Value.solution, solutionTup.Value.polys, jumps);
            }
            return null;
        }
    }
}
