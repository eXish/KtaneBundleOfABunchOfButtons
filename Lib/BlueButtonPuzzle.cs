using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace BlueButtonLib
{
    public class BlueButtonPuzzle
    {
        // PUBLIC
        public PolyominoPlacement[] Polyominoes { get; private set; }
        public int[] PolyominoColors { get; private set; }
        public int[] ColorStageColors { get; private set; }
        public int[] EquationOffsets { get; private set; }
        public string Word { get; private set; }
        public int[] Suits { get; private set; }

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
            var givenGrid = new int?[_gw * _gh];
            foreach (var cell in givenPolyominoPlacement.Polyomino.Cells)
                givenGrid[givenPolyominoPlacement.Place.AddWrap(cell).Index] = 1;
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

                return SolvePolyominoPuzzle(grid, 2, placements, noAllowTouch);
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
            var eqColorExtraCandidates = Enumerable.Range(0, 6).Except(new[] { eqPolyIx, eqDiamondsIx }).ToArray();

            var colorStageColors = new List<int>
            {
                polyColors[firstKeyColorIx].color,
                polyColors[(firstKeyColorIx + 1) % polyColors.Length].color,
                polyColors[(firstKeyColorIx + 2) % polyColors.Length].color,
                suitPartialPermutationColor
            };
            var numColorsAv = Enumerable.Range(4, 7).Where(n => n != eqDiamondsIx && n != eqPolyIx).ToArray();
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

            return new BlueButtonPuzzle
            {
                Polyominoes = polyColors.Select(tup => tup.poly).ToArray(),
                PolyominoColors = polyColors.Select(tup => tup.color).ToArray(),
                ColorStageColors = colorStageColors.ToArray(),
                EquationOffsets = new[] { colorStageColors.Count, eqDiamondsIx, eqPolyIx },
                Suits = suitsTargetPermutation,
                Word = word
            };
        }

        // PRIVATE
        const int _gw = 6;
        const int _gh = 4;

        private static readonly string[] _words = new[] { "ABORT", "ABOUT", "ABYSS", "ACIDS", "ACORN", "ACRES", "ACTED", "ACTOR", "ACUTE", "ADDER", "ADDLE", "ADIEU", "ADIOS", "ADMIN", "ADMIT", "ADOPT", "ADORE", "ADORN", "ADULT", "AFFIX", "AFTER", "AGILE", "AGING", "AGORA", "AGREE", "AHEAD", "AIDED", "AIMED", "AIOLI", "AIRED", "AISLE", "ALARM", "ALBUM", "ALIAS", "ALIBI", "ALIEN", "ALIGN", "ALIKE", "ALIVE", "ALLAY", "ALLEN", "ALLOT", "ALLOY", "ALOFT", "ALONG", "ALOOF", "ALOUD", "ALPHA", "ALTAR", "ALTER", "AMASS", "AMINO", "AMISH", "AMISS", "AMUSE", "ANGLO", "ANGRY", "ANGST", "ANIME", "ANION", "ANISE", "ANNEX", "ANNOY", "ANNUL", "ANTIC", "ANVIL", "AORTA", "APRON", "AREAS", "ARENA", "ARGUE", "ARISE", "ARMED", "ARMOR", "AROSE", "ASHEN", "ASHES", "ASIAN", "ASIDE", "ASSET", "ASTER", "ASTIR", "ATOLL", "ATOMS", "ATTIC", "AUDIO", "AUDIT", "AUGUR", "AUNTY", "AVAIL", "AVIAN", "AWAIT", "AWARE", "AWASH", "AXIAL", "AXION", "AZTEC", "BILGE", "BILLS", "BINGE", "BINGO", "BIRDS", "BIRTH", "BISON", "BITER", "BLIMP", "BLIND", "BLING", "BLINK", "BLISS", "BLITZ", "BLOOM", "BLOOP", "BLUES", "BLUES", "BLUNT", "BLUSH", "BOGUS", "BOGUS", "BOLTS", "BONUS", "BOOST", "BOOTH", "BOOTS", "BORAX", "BORED", "BORER", "BORNE", "BORON", "BOUGH", "BOULE", "BRACE", "BRAID", "BRAIN", "BRAKE", "BRAND", "BRASH", "BRASS", "BRAVE", "BRAWL", "BRAWN", "BRAZE", "BREAD", "BREAK", "BREAM", "BREED", "BRIAR", "BRIBE", "BRICK", "BRIDE", "BRIEF", "BRIER", "BRINE", "BRING", "BRINK", "BRINY", "BRISK", "BROIL", "BRONX", "BROOM", "BROTH", "BRUNT", "BRUSH", "BRUTE", "BUCKS", "BUDDY", "BUDGE", "BUGGY", "BUILD", "BUILT", "BULBS", "BULGE", "BULLS", "BUNNY", "BUSES", "BUZZY", "BYLAW", "CABBY", "CABIN", "CACHE", "CAIRN", "CAKES", "CALLS", "CALVE", "CALYX", "CAMPS", "CAMPY", "CANAL", "CANED", "CANNY", "CANON", "CARDS", "CARVE", "CASED", "CASES", "CASTE", "CATCH", "CAUSE", "CAVES", "CEASE", "CEDED", "CELLS", "CENTS", "CHAFE", "CHAFF", "CHAIN", "CHAIR", "CHALK", "CHAMP", "CHANT", "CHAOS", "CHAPS", "CHARM", "CHART", "CHARY", "CHASE", "CHASM", "CHEAP", "CHEAT", "CHECK", "CHEMO", "CHESS", "CHEST", "CHIDE", "CHILD", "CHILI", "CHILL", "CHIME", "CHINA", "CHINA", "CHORD", "CHORE", "CHOSE", "CHUTE", "CINCH", "CITED", "CITES", "CIVET", "CIVIC", "CIVIL", "CLADE", "CLAIM", "CLANK", "CLASH", "CLASS", "CLAWS", "CLEAN", "CLEAR", "CLEAT", "CLICK", "CLIFF", "CLIMB", "CLING", "CLOSE", "CLOTH", "CLOUD", "CLOUT", "CLUBS", "CLUCK", "CLUES", "CLUNG", "COINS", "COLIC", "COLON", "COLOR", "COMIC", "CONIC", "CORAL", "CORGI", "CORNY", "CORPS", "COSTS", "COTTA", "COUCH", "COUGH", "COUNT", "COYLY", "CRANE", "CRANK", "CRASH", "CRASS", "CRATE", "CRAVE", "CREAK", "CREAM", "CREED", "CREWS", "CRIED", "CRIES", "CRIME", "CROPS", "CROSS", "CRUDE", "CRUEL", "CRUSH", "CUBBY", "CUBIT", "CUMIN", "CUTIE", "CYCLE", "CYNIC", "CZECH", "DACHA", "DAILY", "DALLY", "DANCE", "DATED", "DATES", "DATUM", "DEALS", "DEALT", "DEATH", "DEBIT", "DEBTS", "DEBUT", "DECAF", "DECAL", "DECOR", "DECOY", "DEEDS", "DEIST", "DELVE", "DEMUR", "DENIM", "DENSE", "DESKS", "DETER", "DETOX", "DEUCE", "DEVIL", "DICED", "DIETS", "DIGIT", "DIMLY", "DINAR", "DINER", "DINGY", "DIRTY", "DISCO", "DISCS", "DISKS", "DITCH", "DITTY", "DITZY", "DIVAN", "DIVED", "DIVER", "DIVOT", "DIVVY", "DOGGY", "DOING", "DOLLS", "DONOR", "DONUT", "DOORS", "DORIC", "DOSED", "DOSES", "DOTTY", "DOUGH", "DOUSE", "DRAFT", "DRAIN", "DRAMA", "DRANK", "DREAM", "DRESS", "DRIED", "DRIER", "DRIFT", "DRILL", "DRILY", "DRINK", "DRIVE", "DROLL", "DROPS", "DRUGS", "DRUMS", "DRYER", "DUCAT", "DUCKS", "DUMMY", "DUNCE", "DUNES", "DUTCH", "DUVET", "DWARF", "DWELL", "DYING", "EARED", "EARLY", "EARTH", "EASED", "EASEL", "EATEN", "ECLAT", "EDEMA", "EDICT", "EDIFY", "EGRET", "EIDER", "EIGHT", "ELATE", "ELDER", "ELECT", "ELIDE", "ELITE", "ELUDE", "ELVES", "EMCEE", "ENACT", "ENNUI", "ENSUE", "ENTER", "ENVOY", "ETHOS", "ETUDE", "EVENT", "EVICT", "EXACT", "EXALT", "EXAMS", "EXILE", "EXIST", "EXUDE", "EXULT", "FAILS", "FAINT", "FAIRY", "FAITH", "FALLS", "FALSE", "FAMED", "FANCY", "FATAL", "FATED", "FATTY", "FATWA", "FAULT", "FAVOR", "FEAST", "FECAL", "FEINT", "FETAL", "FIBRE", "FIFTH", "FIFTY", "FIGHT", "FILCH", "FILED", "FILET", "FILLE", "FILLS", "FILLY", "FILMS", "FILMY", "FILTH", "FINAL", "FINDS", "FINED", "FINNY", "FIRED", "FIRST", "FISTS", "FLAIL", "FLAIR", "FLASK", "FLATS", "FLEET", "FLING", "FLIRT", "FLOOR", "FLORA", "FLOUT", "FLUNG", "FLYBY", "FOGGY", "FOIST", "FOLIC", "FOLIO", "FOLLY", "FONTS", "FORAY", "FORGO", "FORMS", "FORTE", "FORTH", "FORTY", "FORUM", "FOUNT", "FRAIL", "FRAUD", "FREED", "FRIED", "FRILL", "FRISK", "FRONT", "FRUIT", "FUELS", "FULLY", "FUNNY", "FUSED", "FUTON", "FUZZY", "GHOUL", "GIMPY", "GIRLS", "GIRLY", "GIRTH", "GIVEN", "GLIAL", "GLINT", "GLOOM", "GLORY", "GLUED", "GLUON", "GOING", "GOLLY", "GOOFY", "GOOPY", "GRAFT", "GRAIN", "GRAND", "GRANT", "GRASS", "GRATE", "GRAVE", "GRAVY", "GREAT", "GREED", "GREEN", "GREET", "GRILL", "GRIME", "GRIMY", "GRIND", "GRIPS", "GROIN", "GROOM", "GROSS", "GROUT", "GRUEL", "GRUNT", "GUEST", "GUILD", "GUILT", "GUISE", "GULLS", "GULLY", "GUNNY", "GUTSY", "GYRUS", "HABIT", "HAIKU", "HAIRS", "HAIRY", "HALAL", "HALVE", "HAMMY", "HANDS", "HANDY", "HANGS", "HARDY", "HAREM", "HARPY", "HARSH", "HASTE", "HATCH", "HATED", "HATES", "HAUNT", "HAVEN", "HAZEL", "HEADS", "HEADY", "HEARD", "HEARS", "HEART", "HEATH", "HEAVE", "HEAVY", "HEELS", "HEIST", "HELIX", "HELLO", "HILLS", "HILLY", "HINDI", "HINDU", "HINTS", "HITCH", "HOBBY", "HOIST", "HOLLY", "HONOR", "HORNS", "HORSE", "HOSEL", "HOTLY", "HULLO", "HUMOR", "ICHOR", "ICILY", "ICING", "ICONS", "IDEAL", "IDEAS", "IDIOM", "IDIOT", "IDLED", "IDYLL", "IGLOO", "ILIAC", "ILIUM", "IMAGO", "IMBUE", "INANE", "INCAN", "INCUS", "INDEX", "INDIA", "INDIE", "INFRA", "INGOT", "INLAY", "INLET", "INPUT", "INSET", "INTRO", "INUIT", "IONIC", "IRISH", "IRONY", "ISLET", "ISSUE", "ITEMS", "IVORY", "JOINS", "JOINT", "JUMBO", "JUNTA", "KANJI", "KARAT", "KARMA", "KILLS", "KINDA", "KINDS", "KINGS", "KITTY", "KNAVE", "KNIFE", "KNOBS", "KNOLL", "KNOTS", "KUDOS", "LABOR", "LACED", "LADLE", "LAITY", "LAMBS", "LAMPS", "LANDS", "LANES", "LAPIN", "LAPSE", "LARGE", "LARVA", "LASER", "LASSO", "LASTS", "LATCH", "LATER", "LATHE", "LATIN", "LATTE", "LAUGH", "LAWNS", "LAYER", "LAYUP", "LEACH", "LEADS", "LEAFY", "LEANT", "LEAPT", "LEARN", "LEASE", "LEASH", "LEAST", "LEAVE", "LEDGE", "LEECH", "LEGGY", "LEMMA", "LEMON", "LEMUR", "LEVEE", "LEVER", "LIANA", "LIDAR", "LIEGE", "LIFTS", "LIGHT", "LIKEN", "LIKES", "LILAC", "LIMBO", "LIMBS", "LIMIT", "LINED", "LINEN", "LINER", "LINES", "LINGO", "LINKS", "LIONS", "LISTS", "LITER", "LITRE", "LIVED", "LIVEN", "LIVER", "LIVES", "LIVID", "LLAMA", "LOBBY", "LOFTY", "LOGON", "LOLLY", "LOONY", "LOOPS", "LOOPY", "LOOSE", "LORDS", "LORRY", "LOSER", "LOSES", "LOTTO", "LOTUS", "LOUSE", "LOYAL", "LUCID", "LUCRE", "LUMEN", "LUNAR", "LUNCH", "LUNGE", "LUNGS", "LYING", "LYMPH", "LYNCH", "LYRIC", "MADAM", "MADLY", "MAINS", "MAJOR", "MALAY", "MALTA", "MAMBO", "MANGO", "MANGY", "MANIA", "MANIC", "MANLY", "MANOR", "MASKS", "MATCH", "MATED", "MATHS", "MATTE", "MAVEN", "MAYOR", "MEALS", "MEANS", "MEANT", "MEATY", "MEDAL", "MEDIA", "MEDIC", "MEETS", "MELON", "METAL", "MEZZO", "MICRO", "MIDST", "MIGHT", "MILES", "MILLS", "MIMIC", "MINCE", "MINDS", "MINED", "MINES", "MINOR", "MINTY", "MINUS", "MIRED", "MIRTH", "MITRE", "MOGUL", "MOIST", "MONTH", "MOONY", "MOORS", "MOOSE", "MORAL", "MORAY", "MORPH", "MOTIF", "MOTOR", "MOTTO", "MOUNT", "MOUSE", "MOUTH", "MOVIE", "MUCUS", "MUDDY", "MUGGY", "MULCH", "MULTI", "MUSED", "MUSIC", "MUTED", "MUZZY", "NADIR", "NAILS", "NAIVE", "NAMED", "NAMES", "NANNY", "NARCO", "NASAL", "NATAL", "NATTY", "NAVAL", "NAVEL", "NEATH", "NECKS", "NEEDS", "NEEDY", "NEIGH", "NESTS", "NEVER", "NEXUS", "NICER", "NICHE", "NIECE", "NIFTY", "NIGHT", "NIGRA", "NINTH", "NITRO", "NOISE", "NOOSE", "NORMS", "NORSE", "NORTH", "NOSES", "NUDGE", "NUTTY", "NYLON", "NYMPH", "OFFER", "OFTEN", "OILED", "OLIVE", "ONION", "ONSET", "OOMPH", "OPINE", "OPIUM", "OPTIC", "ORBIT", "ORDER", "OTHER", "OTTER", "OUGHT", "OUNCE", "OUTDO", "OUTER", "OUTER", "OVULE", "PHASE", "PHOTO", "PIGGY", "PILAF", "PILED", "PILLS", "PILOT", "PINCH", "PINTS", "PISTE", "PITCH", "PIVOT", "PIXEL", "PLOTS", "PLUMB", "PLUME", "POINT", "POLIO", "POLLS", "POOLS", "PORCH", "PORTS", "POSED", "POSIT", "POSTS", "POUCH", "PREEN", "PRICE", "PRICY", "PRIDE", "PRIMA", "PRIME", "PRIMP", "PRINT", "PRION", "PRIOR", "PRISE", "PRISM", "PRIVY", "PRIZE", "PROMO", "PRONG", "PROOF", "PROSE", "PROUD", "PRUDE", "PRUNE", "PUDGY", "PULLS", "PUNCH", "PUNIC", "PYLON", "QUEEN", "QUELL", "QUILL", "QUILT", "QUINT", "RABBI", "RADAR", "RADIO", "RAGGY", "RAIDS", "RAILS", "RAINY", "RAISE", "RALLY", "RANCH", "RANGE", "RANGY", "RATED", "RATIO", "RATTY", "RAZOR", "REACH", "REACT", "READS", "REALM", "REARM", "RECAP", "RECON", "RECTO", "REDLY", "REEDY", "REHAB", "REINS", "RELIC", "REMIT", "RENTS", "RESTS", "RHINO", "RIDGE", "RIFLE", "RIGHT", "RIGOR", "RILED", "RINGS", "RINSE", "RIOTS", "RISEN", "RISES", "RISKS", "RITZY", "RIVAL", "RIVEN", "RIVET", "ROBOT", "ROILY", "ROLLS", "ROOFS", "ROOMS", "ROOTS", "ROSIN", "ROTOR", "ROUGE", "ROUGH", "ROUTE", "ROYAL", "RUDDY", "RUGBY", "RUINS", "RULED", "RUMBA", "RUMMY", "RUMOR", "RUNIC", "RUNNY", "RUNTY", "SADLY", "SAGGY", "SAILS", "SAINT", "SALAD", "SALES", "SALLY", "SALON", "SALSA", "SALTS", "SALTY", "SALVE", "SAMBA", "SANDS", "SANDY", "SATED", "SATIN", "SATYR", "SAUCE", "SAUCY", "SAUDI", "SAUNA", "SAVED", "SAVER", "SAVES", "SAVOR", "SAVVY", "SAXON", "SCALD", "SCALE", "SCALP", "SCALY", "SCAMP", "SCANT", "SCAPE", "SCARE", "SCARF", "SCARP", "SCARS", "SCARY", "SCENE", "SCENT", "SCHMO", "SCOFF", "SCOOP", "SCOOT", "SCOPE", "SCORE", "SCORN", "SCOTS", "SCOUR", "SCOUT", "SCRAM", "SCRAP", "SCREE", "SCREW", "SCRIM", "SCRIP", "SCRUB", "SCRUM", "SCUBA", "SCULL", "SEALS", "SEAMS", "SEAMY", "SEATS", "SEDAN", "SEEDS", "SEEDY", "SEEMS", "SEGUE", "SELLS", "SENDS", "SENSE", "SETUP", "SEVEN", "SEVER", "SEXES", "SHADE", "SHADY", "SHAFT", "SHAKE", "SHALE", "SHALL", "SHAME", "SHANK", "SHAPE", "SHARD", "SHARE", "SHARP", "SHAVE", "SHAWL", "SHEAF", "SHEAR", "SHEEN", "SHEET", "SHELL", "SHILL", "SHINE", "SHINY", "SHOOT", "SHOPS", "SHORE", "SHORN", "SHORT", "SHOTS", "SHOUT", "SHUNT", "SHUSH", "SIDES", "SIDLE", "SIGHT", "SIGIL", "SIGNS", "SILLY", "SILTY", "SINCE", "SINEW", "SINGE", "SINGS", "SINUS", "SITAR", "SITES", "SIXTH", "SIZED", "SIZES", "SKALD", "SKANK", "SKATE", "SKEIN", "SKILL", "SKIMP", "SKINS", "SKULL", "SLEEP", "SLEET", "SLICE", "SLIDE", "SLIME", "SLIMY", "SLING", "SLINK", "SLOPE", "SLOSH", "SLOTH", "SLOTS", "SLUSH", "SMELL", "SMELT", "SMILE", "SMITE", "SNAIL", "SNAKE", "SNARE", "SNARL", "SNEER", "SNIDE", "SNIFF", "SNIPE", "SNOOP", "SNORE", "SNORT", "SNOUT", "SOFTY", "SOGGY", "SOILS", "SOLID", "SOLVE", "SONGS", "SONIC", "SOOTH", "SORRY", "SORTS", "SOUGH", "SOULS", "SOUTH", "STEAD", "STEAL", "STEAM", "STEEL", "STEEP", "STEER", "STEMS", "STENO", "STILE", "STILL", "STILT", "STING", "STINK", "STINT", "STOMP", "STONY", "STOOL", "STOOP", "STOPS", "STORE", "STORK", "STORM", "STORY", "STOUT", "STUDY", "STUNT", "SUAVE", "SUEDE", "SUITE", "SUITS", "SULLY", "SUNNY", "SUNUP", "SUSHI", "SWALE", "SWAMI", "SWAMP", "SWANK", "SWANS", "SWARD", "SWARM", "SWASH", "SWATH", "SWAZI", "SWEAR", "SWEAT", "SWELL", "SWILL", "SWINE", "SWING", "SWISH", "SWISS", "SWOON", "SWOOP", "SWORD", "SWORE", "SWORN", "SWUNG", "TACIT", "TACKY", "TAFFY", "TAILS", "TAINT", "TAKEN", "TALKY", "TALLY", "TALON", "TAMED", "TAMIL", "TANGO", "TANGY", "TARDY", "TAROT", "TARRY", "TASKS", "TATTY", "TAUNT", "TAXES", "TAXIS", "TAXON", "TEACH", "TEAMS", "TEARS", "TEARY", "TEASE", "TECHY", "TEDDY", "TEENS", "TEETH", "TELLS", "TELLY", "TENOR", "TENSE", "TENTH", "TENTS", "TEXAS", "THANK", "THEME", "THESE", "THETA", "THIGH", "THINE", "THING", "THINK", "THONG", "THORN", "THOSE", "THUMB", "TIARA", "TIDAL", "TIGHT", "TILDE", "TILED", "TILES", "TILTH", "TIMED", "TIMES", "TIMID", "TINES", "TINNY", "TIRED", "TITLE", "TOMMY", "TONGS", "TONIC", "TONNE", "TOOLS", "TOONS", "TOOTH", "TOQUE", "TORCH", "TORSO", "TORTE", "TORUS", "TOUCH", "TOXIN", "TRACT", "TRAIL", "TRAIN", "TRAIT", "TRAMS", "TRAWL", "TREAD", "TREAT", "TRIAD", "TRIAL", "TRIED", "TRIKE", "TRILL", "TRITE", "TROLL", "TROOP", "TROUT", "TRUCE", "TRUCK", "TRUST", "TRUTH", "TUBBY", "TULIP", "TUNIC", "TUTEE", "TUTOR", "TWANG", "TWEAK", "TWINS", "TWIST", "TYING", "UDDER", "ULCER", "ULNAR", "ULTRA", "UMBRA", "UNCLE", "UNCUT", "UNIFY", "UNION", "UNITE", "UNITS", "UNITY", "UNLIT", "UNMET", "UNTIE", "UNTIL", "USAGE", "USHER", "USING", "USUAL", "UTTER", "UVULA", "VAGUE", "VALET", "VALID", "VALOR", "VALUE", "VALVE", "VAPOR", "VAULT", "VAUNT", "VEDIC", "VEINS", "VEINY", "VENAL", "VENOM", "VENUE", "VICAR", "VIEWS", "VIGIL", "VIGOR", "VILLA", "VINES", "VINYL", "VIRUS", "VISIT", "VISOR", "VITAL", "VIVID", "VIXEN", "VOGUE", "VOUCH", "VROOM", "WAGON", "WAIST", "WAITS", "WAIVE", "WALKS", "WALLS", "WALTZ", "WANTS", "WARDS", "WARES", "WARNS", "WASTE", "WATCH", "WAVED", "WAVES", "WAXEN", "WEARS", "WEARY", "WEAVE", "WEBBY", "WELLS", "WETLY", "WHALE", "WHEAT", "WHEEL", "WHILE", "WHINE", "WHITE", "WHORL", "WHOSE", "WILLS", "WIMPY", "WINCE", "WINCH", "WINDS", "WINES", "WINGS", "WITCH", "WITTY", "WIVES", "WORDS", "WORKS", "WORLD", "WORMS", "WORMY", "WORSE", "WORST", "WORTH", "XENON", "YARDS", "YAWNS", "YEARN", "YEARS", "YEAST", "YOUNG", "YOUTH", "YUMMY", "ZILCH", "ZINGY" };

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

            foreach (var tCell in Coord.Cells(_gw, _gh))
            {
                if (sofar[tCell.Index] != null)
                    continue;
                var tPossiblePlacementIxs = possiblePlacements.SelectIndexWhere(pl => pl.Polyomino.Has((tCell.X - pl.Place.X + _gw) % _gw, (tCell.Y - pl.Place.Y + _gh) % _gh)).ToArray();
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
                .Where(poly => !poly.Cells.Any(c => c.X >= _gw || c.Y >= _gh)
                    && !poly.Cells.Any(c =>
                        (!poly.Has(c.X + 1, c.Y) && poly.Has((c.X + 1) % _gw, c.Y)) ||
                        (!poly.Has(c.X - 1, c.Y) && poly.Has((c.X + _gw - 1) % _gw, c.Y)) ||
                        (!poly.Has(c.X, c.Y + 1) && poly.Has(c.X, (c.Y + 1) % _gh)) ||
                        (!poly.Has(c.X, c.Y - 1) && poly.Has(c.X, (c.Y + _gh - 1) % _gh))))
                .ToArray();
        }

        private static List<PolyominoPlacement> GetAllPolyominoPlacements() =>
            (from poly in GetAllPolyominoes() from place in Enumerable.Range(0, _gw * _gh) select new PolyominoPlacement(poly, new Coord(_gw, _gh, place))).ToList();

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
            var allJumps = Enumerable.Range(0, 1 << 4).ToArray().Shuffle(rnd);
            foreach (var jumps in allJumps)
            {
                var letterPositions = Enumerable.Range(0, word.Length).Select(i => new Coord(_gw, _gh, i).AddYWrap((jumps & (1 << i)) != 0 ? 2 : 0)).ToArray();
                var placements = allPlacements.Where(tup => letterPositions.All((cell, ix) =>
                {
                    var (poly, place) = tup;
                    var dx = (cell.X - place.X + _gw) % _gw;
                    var dy = (cell.Y - place.Y + _gh) % _gh;
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

                var solutionTup = SolvePolyominoPuzzle(new int?[_gw * _gh], 1, placements).FirstOrNull();
                if (solutionTup != null)
                    return (solutionTup.Value.solution, solutionTup.Value.polys, jumps);
            }
            return null;
        }
    }
}
