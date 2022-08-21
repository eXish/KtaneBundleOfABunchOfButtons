using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlueButtonLib
{
    public sealed class SapphireButtonPuzzle
    {
        private SapphireButtonPuzzle(string answer, string[] words, bool[][] fonted, bool[][] fontedXored, bool[][] binaryRaw, bool[][] binaryTransposed, int[] modes, string logging)
        {
            Answer = answer;
            Words = words;
            Fonted = fonted;
            FontedXored = fontedXored;
            BinaryRaw = binaryRaw;
            BinaryTransposed = binaryTransposed;
            Modes = modes;
            Logging = logging;
        }

        public string Answer { get; private set; }
        public string[] Words { get; private set; }
        public bool[][] Fonted { get; private set; }
        public bool[][] FontedXored { get; private set; }
        public bool[][] BinaryRaw { get; private set; }
        public bool[][] BinaryTransposed { get; private set; }
        public int[] Modes { get; private set; }
        public string Logging { get; private set; }
        public int Cells => Fonted[0].Length;
        public int Width => Cells / 5;

        // _font[row][ltr][bit]
        private static readonly bool[][][] _font = ".#.,##.,.#,##.,##,##,.##,#.#,#,.#,#.#,#.,#...#,#..#,.#.,##.,.#.,##.,.##,###,#.#,#...#,#...#,#.#,#.#,###;#.#,#.#,#.,#.#,#.,#.,#..,#.#,#,.#,#.#,#.,##.##,##.#,#.#,#.#,#.#,#.#,#..,.#.,#.#,#...#,#...#,#.#,#.#,..#;###,##.,#.,#.#,##,##,#.#,###,#,.#,##.,#.,#.#.#,#.##,#.#,##.,#.#,##.,.#.,.#.,#.#,.#.#.,#.#.#,.#.,.#.,.#.;#.#,#.#,#.,#.#,#.,#.,#.#,#.#,#,.#,#.#,#.,#...#,#.##,#.#,#..,###,#.#,..#,.#.,#.#,.#.#.,##.##,#.#,.#.,#..;#.#,##.,.#,##.,##,#.,.##,#.#,#,#.,#.#,##,#...#,#..#,.#.,#..,.##,#.#,##.,.#.,.#.,..#..,#...#,#.#,.#.,###"
            .Split(';').Select(row => row.Split(',').Select(str => str.Select(ch => ch == '#').ToArray()).ToArray()).ToArray();

        public static SapphireButtonPuzzle GeneratePuzzle(int seed)
        {
            bool[] bitsFromWord(string word) => Enumerable.Range(0, 5).SelectMany(ltr => Enumerable.Range(0, 5).Select(bit => ((word[ltr] - 'A' + 1) & (1 << (4 - bit))) != 0)).ToArray();
            string wordFromBits(bool[] bits) => Enumerable.Range(0, 5).Select(row => Enumerable.Range(0, 5).Select(bit => bits[bit + 5 * row]).Aggregate(0, (p, n) => p * 2 + (n ? 1 : 0))).Select(i => (char) ('A' + i - 1)).JoinString();
            bool[] transpose(bool[] bits, int mode) => Enumerable.Range(0, 25).Select(ix => { var row = ix / 5; var col = ix % 5; return bits[((mode & 1) != 0 ? row : 4 - row) + 5 * ((mode & 2) != 0 ? col : 4 - col)]; }).ToArray();
            int reverseMode(int mode) => (mode >> 1) | ((mode & 1) << 1);
            string[] outputBits(bool[] bits, int width = 5) => Enumerable.Range(0, 5)
                .Select(row => Enumerable.Range(0, width).Select(bit => bits[bit + width * row] ? "██" : "░░").JoinString())
                .ToArray();

            var words = new HashSet<string>(WordLists.FiveLetters);
            var rnd = new Random(seed);

            var solWordIx = rnd.Next(0, WordLists.FiveLetters.Length);
            var solWord = WordLists.FiveLetters[solWordIx];
            var solRawBits = bitsFromWord(solWord);

            tryAgain:
            var xor1WordIx = rnd.Next(0, WordLists.FiveLetters.Length);
            if (xor1WordIx == solWordIx)
                goto tryAgain;
            var xor1Word = WordLists.FiveLetters[xor1WordIx];
            var xor1RawBits = bitsFromWord(xor1Word);

            var xor2ofs = rnd.Next(0, WordLists.FiveLetters.Length);
            for (var xor2WordIxR = 0; xor2WordIxR < 1000; xor2WordIxR++)
            {
                var xor2WordIx = (xor2WordIxR + xor2ofs) % WordLists.FiveLetters.Length;
                if (xor2WordIx == xor1WordIx || xor2WordIx == solWordIx)
                    continue;
                var xor2Word = WordLists.FiveLetters[xor2WordIx];
                var xor2RawBits = bitsFromWord(xor2Word);
                for (var xor1Mode = 0; xor1Mode < 4; xor1Mode++)
                {
                    var xor1Bits = transpose(xor1RawBits, xor1Mode);
                    for (var xor2Mode = 0; xor2Mode < 4; xor2Mode++)
                    {
                        var xor2Bits = transpose(xor2RawBits, xor2Mode);
                        var xor3Bits = Ut.NewArray(25, ix => xor1Bits[ix] ^ xor2Bits[ix] ^ solRawBits[ix]);
                        for (var xor3Mode = 0; xor3Mode < 4; xor3Mode++)
                        {
                            var xor3RawBits = transpose(xor3Bits, reverseMode(xor3Mode));
                            var xor3Word = wordFromBits(xor3RawBits);
                            if (xor3Word != xor1Word && xor3Word != xor2Word && xor3Word != solWord && words.Contains(xor3Word))
                            {
                                var xorWords = new[] { xor1Word, xor2Word, xor3Word };
                                var xorModes = new[] { xor1Mode, xor2Mode, xor3Mode };
                                var width = xorWords.Max(wrd => wrd.Sum(ch => _font[0][ch - 'A'].Length) + wrd.Length - 1) + 1;
                                var bitmapSz = width * 5;
                                var fonted = new bool[3][];
                                for (var wrd = 0; wrd < 3; wrd++)
                                {
                                    fonted[wrd] = new bool[bitmapSz];
                                    var x = width - Enumerable.Range(0, xorModes[wrd] + 1).Sum(ix => _font[0][xorWords[wrd][ix] - 'A'].Length) - xorModes[wrd];
                                    for (var ltr = 0; ltr < xorWords[wrd].Length; ltr++)
                                    {
                                        for (var row = 0; row < 5; row++)
                                            Array.Copy(_font[row][xorWords[wrd][ltr] - 'A'], 0, fonted[wrd], x + width * row, _font[row][xorWords[wrd][ltr] - 'A'].Length);
                                        x = (x + _font[0][xorWords[wrd][ltr] - 'A'].Length + 1) % (width + 1);
                                    }
                                }

                                var fontedXor = Ut.NewArray(
                                    fonted[0].Select((b, ix) => b ^ fonted[1][ix]).ToArray(),
                                    fonted[0].Select((b, ix) => b ^ fonted[1][ix] ^ fonted[2][ix]).ToArray(),
                                    fonted[1].Select((b, ix) => b ^ fonted[2][ix]).ToArray());

                                var logging = new StringBuilder();
                                logging.Append("Bitmaps shown on module:\n");
                                foreach (var f in fontedXor)
                                    foreach (var line in outputBits(f, width))
                                        logging.Append($"{line}\n");
                                logging.Append("Reconstructed bitmaps:\n");
                                foreach (var f in fonted)
                                    foreach (var line in outputBits(f, width))
                                        logging.Append($"{line}\n");

                                logging.Append($"{xor1Word}/{xor1Mode + 1} ^ {xor2Word}/{xor2Mode + 1} ^ {xor3Word}/{xor3Mode + 1} = {solWord}\n");
                                var outputs = new[] { xor1RawBits, xor1Bits, xor2RawBits, xor2Bits, xor3RawBits, xor3Bits, solRawBits }.Select(b => outputBits(b)).ToArray();
                                var afters = " → ,  ^  , → ,  ^  , → ,  =  ,\n".Split(',');
                                for (var row = 0; row < 5; row++)
                                    for (var i = 0; i < outputs.Length; i++)
                                        logging.Append(outputs[i][row] + afters[i]);

                                return new SapphireButtonPuzzle(solWord,
                                    new[] { xor1Word, xor2Word, xor3Word },
                                    fonted, fontedXor,
                                    new[] { xor1RawBits, xor2RawBits, xor3RawBits },
                                    new[] { xor1Bits, xor2Bits, xor3Bits },
                                    new[] { xor1Mode, xor2Mode, xor3Mode },
                                    logging.ToString());
                            }
                        }
                    }
                }
            }
            goto tryAgain;
        }
    }
}