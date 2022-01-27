using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButtonLib
{
    public class CeruleanButtonPuzzle
    {
        private CeruleanButtonPuzzle(BlueLatinSquare latinSquare, EndViewConstraint[] constraints, Cube leftCube, Cube rightCube, string answer)
        {
            LatinSquare = latinSquare;
            Constraints = constraints;
            LeftCube = leftCube;
            RightCube = rightCube;
            Answer = answer;
        }

        public BlueLatinSquare LatinSquare { get; }
        public EndViewConstraint[] Constraints { get; }
        public Cube LeftCube { get; }
        public Cube RightCube { get; }
        public string Answer { get; }
        public int Size => _sz;

        private const int _sz = 4;

        private static readonly string[] _words = "DEED|DEEP|DEER|DENT|DENY|DIET|DINE|DIRE|DIRT|DISH|DOLE|DOLL|DOME|DONE|DOOR|DORM|DORY|DOSE|DREY|DRIP|DROP|DRUG|DRUM|DUEL|DUET|DULL|DULY|DUMP|DUST|DUTY|DYER|EDGE|EDIT|ELSE|EURO|EYED|EYES|FEED|FEEL|FEET|FELL|FELT|FILE|FILL|FILM|FIND|FINE|FIRE|FIRM|FIRS|FISH|FIST|FLED|FLEE|FLIP|FOIL|FOLD|FOND|FONT|FOOD|FOOL|FOOT|FORD|FORM|FORT|FOUL|FOUR|FREE|FROG|FROM|FUEL|FULL|FUND|FURY|FUSE|FUSS|GENE|GERM|GIFT|GILL|GILT|GIRL|GLEE|GLEN|GLUE|GOES|GOLD|GOLF|GONE|GONG|GOOD|GORY|GOSH|GREY|GRIM|GRIN|GRIP|GRIT|GUMP|GUST|GYRE|GYRO|HEEL|HEIR|HELD|HELL|HELP|HERD|HERE|HERO|HERS|HIDE|HIGH|HILL|HINT|HIRE|HOLD|HOLE|HOLY|HOME|HOOD|HOPE|HORN|HOSE|HOST|HOUR|HUGE|HULL|HUNG|HUNT|HURT|HUSH|HYMN|HYPE|IDLE|IDLY|IDOL|IDYL|INFO|INTO|IRON|ITEM|LEER|LEFT|LEND|LENS|LENT|LESS|LEST|LIED|LIFE|LIFT|LIME|LINE|LINT|LION|LIPS|LIRE|LISP|LIST|LOFT|LOGO|LONE|LONG|LOOP|LOPS|LORD|LORE|LOSE|LOSS|LOST|LOTS|LOUD|LOUP|LUMP|LUNG|LURE|LUSH|LUST|MEET|MELD|MELT|MEMO|MENU|MERE|MESH|MESS|MILD|MILE|MILL|MIME|MIND|MINE|MINI|MINT|MISS|MIST|MODE|MOLD|MOLE|MOLT|MOOD|MOON|MOOR|MORE|MOSS|MOST|MOTH|MULE|MUST|MUTE|MYTH|NEED|NEON|NERD|NEST|NINE|NODE|NONE|NOON|NORM|NOSE|NOTE|NOUN|NUTS|ODDS|ODOR|OGRE|OILY|OMEN|OMIT|ONLY|ONTO|OPEN|ORES|ORGY|OURS|PEEL|PEER|PEST|PIER|PILE|PILL|PINE|PINT|PIPE|PITY|PLOT|PLOY|PLUG|PLUM|PLUS|POEM|POET|POLE|POLL|POLO|POND|PONY|POOL|POOR|PORT|POSE|POSH|POST|POUR|PREY|PROP|PROS|PULL|PUMP|PUNT|PUPS|PURE|PUSH|REEF|REEL|RELY|REND|RENT|REPS|REST|RHOS|RIDE|RIFT|RILE|RIND|RING|RIOT|RIPE|RIPS|RISE|RITE|RODE|ROLE|ROLL|ROOF|ROOM|ROOT|ROPE|ROSE|ROSY|RUDE|RUIN|RULE|RUNG|RUSH|RUST|SEED|SEEM|SEEN|SELF|SELL|SEND|SENT|SERF|SHED|SHIP|SHOE|SHOP|SHOT|SHUT|SIDE|SIGH|SIGN|SING|SITE|SLID|SLIM|SLIP|SLOP|SLOT|SLUM|SMUG|SOFT|SOIL|SOLD|SOLE|SOLO|SOME|SONG|SOON|SORE|SORT|SOUL|SOUP|SOUR|SPIN|SPOT|SPUN|SPUR|STEM|STEP|STIR|STOP|SUIT|SUNG|SURE|SUSS|TELL|TEND|TENT|TERM|TERN|TEST|THEE|THEM|THEN|THEY|THIN|THIS|THOU|THUD|THUS|TIDE|TIDY|TIED|TIER|TILE|TILL|TILT|TIME|TINY|TIRE|TOIL|TOLD|TOLL|TONE|TOOL|TORE|TORN|TORT|TORY|TOSS|TOUR|TREE|TREY|TRIM|TRIO|TRIP|TRON|TROY|TRUE|TUNE|TURF|TURN|TYPE|TYRE|UGLY|UNDO|UNIT|UNTO|UPON|URGE|USED|USER|USES|YELL|YOUR".Split('|');
        private const string TABLE = "ERIOTNSLUDPMHGFY";
        private static readonly Dictionary<char, (int x, int y)> _table = TABLE.ToDictionary(c => c, c => (TABLE.IndexOf(c) % 4, TABLE.IndexOf(c) / 4));
        private static readonly Cube.Rotation[][] _rotations = "U|D|L|R|UU|DD|LL|RR|LU|UR|RD|DL|LD|DR|RU|UL|LLR|LLD|LLL|LLU|ULD|URD|ULLD".Split('|')
            .Select(s => s.Select(c =>
            {
                return c switch
                {
                    'L' => Cube.Rotation.XY,
                    'R' => Cube.Rotation.YX,
                    'U' => Cube.Rotation.YZ,
                    'D' => Cube.Rotation.ZY,
                    _ => throw new ArgumentException()
                };
            }).ToArray()).ToArray();
        private static readonly List<((Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>))> _splitters = new List<((Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>))>()
        {
            ((c => c.Bottom, c => c.Left), (c => c.Back, c => c.Right), (c => c.Top, c => c.Front)),
            ((c => c.Bottom, c => c.Left), (c => c.Back, c => c.Top), (c => c.Right, c => c.Front)),
            ((c => c.Bottom, c => c.Back), (c => c.Front, c => c.Right), (c => c.Top, c => c.Left)),
            ((c => c.Bottom, c => c.Back), (c => c.Left, c => c.Front), (c => c.Top, c => c.Right)),
            ((c => c.Bottom, c => c.Right), (c => c.Back, c => c.Left), (c => c.Top, c => c.Front)),
            ((c => c.Bottom, c => c.Right), (c => c.Back, c => c.Top), (c => c.Left, c => c.Front)),
            ((c => c.Bottom, c => c.Front), (c => c.Back, c => c.Right), (c => c.Top, c => c.Left)),
            ((c => c.Bottom, c => c.Front), (c => c.Back, c => c.Left), (c => c.Top, c => c.Right))
        };


        public static CeruleanButtonPuzzle GeneratePuzzle(int seed, Action<string> log = null, Action<string> debuglog = null)
        {
            Random rnd = new Random(seed);
            if(log == null)
                log = s => { };
            if(debuglog == null)
                debuglog = s => { };
            int attempt = 0;

        tryagain:
            attempt++;
            if(attempt >= 1000)
            {
                log("After 1000 attempts, no puzzle was found!");
                return null;
            }
            string answer = _words[rnd.Next(_words.Length)]; // Pick a random solution
            debuglog($"Trying word: {answer}");
            debuglog($"This is attempt {attempt}.");

            BlueLatinSquare latinSquare;
            Cube left = null, right = null, leftFinal = null, rightFinal = null;

            for(int i = 0; i < BlueLatinSquare.AllSquares.Count; i++) //Try all Latin Squares to see if any work
            {
                debuglog($"This is inner attempt {i}.");
                latinSquare = BlueLatinSquare.AllSquares[i];
                char letter = answer[0];

                if(_table.Any(kvp => kvp.Key != letter && latinSquare[_table[letter].x, _table[letter].y] == latinSquare[kvp.Value.x, kvp.Value.y] && latinSquare[_table[letter].x + 1, _table[letter].y] == latinSquare[kvp.Value.x + 1, kvp.Value.y]))
                {
                    debuglog("First letter could not be found uniquely.");
                    continue;
                }

                letter = answer[1];
                if(_table.Any(kvp => kvp.Key != letter && latinSquare[_table[letter].x, _table[letter].y] == latinSquare[kvp.Value.x, kvp.Value.y] && latinSquare[_table[letter].x + 1, _table[letter].y + 1] == latinSquare[kvp.Value.x + 1, kvp.Value.y + 1]))
                {
                    debuglog("Second letter could not be found uniquely.");
                    continue;
                }

                letter = answer[2];
                if(_table.Any(kvp => kvp.Key != letter && latinSquare[_table[letter].x, _table[letter].y] == latinSquare[kvp.Value.x, kvp.Value.y] && latinSquare[_table[letter].x + 1, _table[letter].y - 1] == latinSquare[kvp.Value.x + 1, kvp.Value.y - 1]))
                {
                    debuglog("Third letter could not be found uniquely.");
                    continue;
                }

                letter = answer[3];
                if(_table.Any(kvp => kvp.Key != letter && latinSquare[_table[letter].x, _table[letter].y] == latinSquare[kvp.Value.x, kvp.Value.y] && latinSquare[_table[letter].x + 1, _table[letter].y] == latinSquare[kvp.Value.x + 1, kvp.Value.y]))
                {
                    debuglog("Fourth letter could not be found uniquely.");
                    continue;
                }

                //EFG = left cube's back, left, bottom faces' letters, HIJ = right cube's back, right, bottom faces' letters, M = top faces' letter, N = closest faces' letter
                //EH F   J N
                //    I G   M

                int top = latinSquare[_table[answer[3]].x + 1, _table[answer[3]].y + 1];
                int inner = latinSquare[_table[answer[3]].x, _table[answer[3]].y];
                int leftOuter = latinSquare[_table[answer[0]].x, _table[answer[0]].y];
                int leftBottom = latinSquare[_table[answer[2]].x, _table[answer[2]].y];
                int leftBack = latinSquare[_table[answer[1]].x, _table[answer[1]].y];
                int rightOuter = latinSquare[_table[answer[1]].x + 1, _table[answer[1]].y + 1];
                int rightBottom = latinSquare[_table[answer[2]].x + 1, _table[answer[2]].y - 1];
                int rightBack = latinSquare[_table[answer[0]].x + 1, _table[answer[0]].y];

                for(int lfront = 1; lfront <= 3; lfront++)
                {
                    for(int rfront = 1; rfront <= 3; rfront++)
                    {
                        debuglog($"Trying wildcards {lfront} and {rfront}.");
                        left = new Cube(new int[] { top, leftBottom, leftOuter, inner, leftBack, lfront }); // TBLRBF order
                        right = new Cube(new int[] { top, rightBottom, inner, rightOuter, rightBack, rfront });
                        foreach(Cube.Rotation[] rot in _rotations)
                        {
                            Cube l = left;
                            Cube r = right;
                            foreach(Cube.Rotation rotPiece in rot)
                            {
                                l = l.Rotate(rotPiece);
                                r = r.Rotate(rotPiece);
                            }
                            if(l.Right == r.Left && l.Front == r.Front && l.Back == r.Back) goto badwildcard;
                        }
                        debuglog($"Good(?) wildcards {lfront} and {rfront} found.");

                        // Make sure the cube is possible to form
                        List<(int, int)> patches = Enumerable.Range(0, 16).Where(i => latinSquare[i / 4, i % 4] == 0).Select(i => (latinSquare[(i / 4 + 3) % 4, i % 4], latinSquare[(i / 4 + 2) % 4, i % 4])).ToList();
                        List<(int, int)> allPatches = patches.SelectMany(t => new (int, int)[] { t, (t.Item2, t.Item1) }).ToList();
                        ((Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>), (Func<Cube, int>, Func<Cube, int>)) finalSplitterL = ((null, null), (null, null), (null, null)), finalSplitterR = ((null, null), (null, null), (null, null));
                        foreach(var splitter in _splitters)
                        {
                            if(new[] { splitter.Item1, splitter.Item2, splitter.Item3 }.All(t => allPatches.Contains((t.Item1(left), t.Item2(left)))))
                            {
                                finalSplitterL = splitter;
                                goto goodL;
                            }
                        }
                        debuglog("Couldn't find valid splitter. (left side)");
                        goto badwildcard;
                    goodL:;
                        foreach(var splitter in _splitters)
                        {
                            if(new[] { splitter.Item1, splitter.Item2, splitter.Item3 }.All(t => allPatches.Contains((t.Item1(right), t.Item2(right)))))
                            {
                                finalSplitterR = splitter;
                                goto goodR;
                            }
                        }
                        debuglog("Couldn't find valid splitter. (right side)");
                        goto badwildcard;
                    goodR:;

                        debuglog($"Found valid splitters! {_splitters.IndexOf(finalSplitterL)} & {_splitters.IndexOf(finalSplitterR)}");

                        Cube referenceCube = new Cube(new int[] { 0, 1, 2, 3, 4, 5 });
                        int[] leftFaces = new int[] { -1, -1, -1, -1, -1, -1 }, rightFaces = new int[] { -1, -1, -1, -1, -1, -1 };

                        foreach(var patch in new[] { finalSplitterL.Item1, finalSplitterL.Item2, finalSplitterL.Item3 })
                        {
                            if(patches.Contains((patch.Item1(left), patch.Item2(left))))
                                leftFaces[patch.Item1(referenceCube)] = patches.IndexOf((patch.Item1(left), patch.Item2(left))) + 4 * patch.Item2(referenceCube);
                            else
                                leftFaces[patch.Item2(referenceCube)] = patches.IndexOf((patch.Item2(left), patch.Item1(left))) + 4 * patch.Item1(referenceCube);
                        }
                        foreach(var patch in new[] { finalSplitterR.Item1, finalSplitterR.Item2, finalSplitterR.Item3 })
                        {
                            if(patches.Contains((patch.Item1(right), patch.Item2(right))))
                                rightFaces[patch.Item1(referenceCube)] = patches.IndexOf((patch.Item1(right), patch.Item2(right))) + 4 * patch.Item2(referenceCube);
                            else
                                rightFaces[patch.Item2(referenceCube)] = patches.IndexOf((patch.Item2(right), patch.Item1(right))) + 4 * patch.Item1(referenceCube);
                        }

                        leftFinal = new Cube(leftFaces);
                        rightFinal = new Cube(rightFaces);

                        goto answerfound;
                    badwildcard:;
                    }
                }
                debuglog("No good wildcards found!");
                goto tryagain;
            }
            goto tryagain; // 100 Latin Squares didn't work, so choose another word

        answerfound:

            EndViewConstraint[] allConstraints = EndViewConstraint.AllFromBlueLatinSquare(latinSquare);
            EndViewConstraint[] reducedConstraints = Ut.ReduceRequiredSet(allConstraints, s => EndViewConstraint.IsUnique(s.SetToTest.ToArray()), skipConsistencyTest: true).ToArray();

            return new CeruleanButtonPuzzle(latinSquare, reducedConstraints, leftFinal, rightFinal, answer);
        }
    }
}
