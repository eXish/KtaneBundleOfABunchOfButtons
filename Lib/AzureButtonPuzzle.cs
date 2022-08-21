using System;
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
        public AzureButtonArrowInfo[] Arrows;
        public int DecoyArrowPosition;

        private static readonly int[] _powersOf3 = { 1, 3, 9, 27 };

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
            var sol = WordLists.FourLetters[rnd.Next(0, WordLists.FourLetters.Length)];

            // Find all arrows that can potentially form 000 from any diagonal square
            var decoyArrows = AzureButtonArrowInfo.AllArrows
                .Where(ar => Enumerable.Range(0, 4).Any(pos => ar.Coordinates.All(c => grid[c.AddWrap(pos, pos).Index] == 0)))
                .ToArray();
            if (decoyArrows.Length == 0)
                goto tryAgain;
            var arrows = new AzureButtonArrowInfo[5];
            arrows[0] = decoyArrows[rnd.Next(0, decoyArrows.Length)];

            for (var ltrIx = 0; ltrIx < 4; ltrIx++)
            {
                // Pick a random arrow that isn’t a duplicate
                var candidates = AzureButtonArrowInfo.AllArrows
                    .Where(ar =>
                        Enumerable.Range(0, AzureButtonArrowInfo.MaxArrowLength)
                            .All(arIx => grid[ar.Coordinates[arIx].AddWrap(ltrIx, ltrIx).Index] == ((sol[ltrIx] - 'A' + 1) / _powersOf3[2 - arIx]) % 3) &&
                        !arrows.Contains(ar) &&
                        !decoyArrows.Contains(ar))
                    .ToArray();
                if (candidates.Length == 0)
                    goto tryAgain;
                arrows[ltrIx + 1] = candidates[rnd.Next(0, candidates.Length)];
            }

            return new AzureButtonPuzzle
            {
                SetE = setE,
                SetS = setS,
                CardT = cardT,
                Grid = grid,
                SolutionWord = sol,
                Arrows = arrows,
                DecoyArrowPosition = Enumerable.Range(0, 4).First(pos => arrows[0].Coordinates.All(c => grid[c.AddWrap(pos, pos).Index] == 0))
            };
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
