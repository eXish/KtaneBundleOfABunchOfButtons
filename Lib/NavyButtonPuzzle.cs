using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButtonLib
{
    public sealed class NavyButtonPuzzle
    {
        private NavyButtonPuzzle(int[] latinSquare, int givenIx, int givenValue, int[] greekLetterIxs, int[] squaredDistances, int[] numbers, int[] stencilIxs, string answer)
        {
            LatinSquare = latinSquare;
            GivenIndex = givenIx;
            GivenValue = givenValue;
            GreekLetterIxs = greekLetterIxs;
            SquaredDistances = squaredDistances;
            Numbers = numbers;
            TapsRequired = numbers[3] - squaredDistances[3 % squaredDistances.Length];
            StencilIxs = stencilIxs;
            Answer = answer;
        }

        public int[] LatinSquare { get; private set; }
        public int GivenIndex { get; private set; }
        public int GivenValue { get; private set; }
        public int[] GreekLetterIxs { get; private set; }    // 0–23 = upper-case, 24−47 = lower-case
        public int[] SquaredDistances { get; private set; }
        public int[] Numbers { get; private set; }
        public int TapsRequired { get; private set; }
        public int[] StencilIxs { get; private set; }
        public string Answer { get; private set; }
        public int Size => _sz;

        public int[] XCoordinates { get; private set; }
        public int[] YCoordinates { get; private set; }

        public const int _sz = 4;

        public static NavyButtonPuzzle GeneratePuzzle(int seed)
        {
            // Pick a word
            var rnd = new Random(seed);
            var (word, inf) = NavyButtonData.Data[rnd.Next(0, NavyButtonData.Data.Length)];

            // Pick a Latin Square
            var (latinSquare, stencilIxs) = inf[rnd.Next(0, inf.Length)];
            var threes = latinSquare.SelectIndexWhere(v => v == 3).Select(ix => new Coord(_sz, _sz, ix)).ToArray();

            // Pick a set of stencils
            var stencils = stencilIxs[rnd.Next(0, stencilIxs.Length)];

            // Pick a decoy stencil
            var decoyStencils = NavyButtonData.Stencils.SelectIndexWhere(decoy => "1234,2340,3401,4012".Split(',')
                    .Select(str => str.Select(ch => ch - '0').Select(ix => ix == 4 ? decoy : NavyButtonData.Stencils[stencils[ix]]))
                    .All(stencils => stencils.Any((stencil, thrIx) => stencil.Any(tup => latinSquare[threes[thrIx].AddWrap(tup.dx, tup.dy).Index] == 3)))).ToArray();
            var decoyStencilIx = decoyStencils[rnd.Next(0, decoyStencils.Length)];

            // Find all Latin squares that satisfy the same less-than/greater-than constraints
            var upConstraints = Enumerable.Range(0, _sz * _sz).Where(ix => ix / _sz > 0 && latinSquare[ix - _sz] < latinSquare[ix]).Select(ix => (sm: ix - _sz, la: ix));
            var rightConstraints = Enumerable.Range(0, _sz * _sz).Where(ix => ix % _sz < _sz - 1 && latinSquare[ix + 1] < latinSquare[ix]).Select(ix => (sm: ix + 1, la: ix));
            var downConstraints = Enumerable.Range(0, _sz * _sz).Where(ix => ix / _sz < _sz - 1 && latinSquare[ix + _sz] < latinSquare[ix]).Select(ix => (sm: ix + _sz, la: ix));
            var leftConstraints = Enumerable.Range(0, _sz * _sz).Where(ix => ix % _sz > 0 && latinSquare[ix - 1] < latinSquare[ix]).Select(ix => (sm: ix - 1, la: ix));
            var constraints = upConstraints.Concat(rightConstraints).Concat(downConstraints).Concat(leftConstraints).ToArray().Shuffle(rnd);
            var competingLatinSquares = FindSolutions(new int?[_sz * _sz], constraints).ToArray();

            // Pick a given that disambiguates the Latin square
            var candidateGivenIxs = Enumerable.Range(0, _sz * _sz).Where(ix => competingLatinSquares.Count(cls => cls[ix] == latinSquare[ix]) == 1).ToArray();
            var givenIx = candidateGivenIxs[rnd.Next(0, candidateGivenIxs.Length)];

            // Generate a reduced set of constraints
            var grid = new int?[_sz * _sz];
            grid[givenIx] = latinSquare[givenIx];
            var requiredConstraints = Ut.ReduceRequiredSet(constraints, test: state => !FindSolutions(grid.ToArray(), state.SetToTest.ToArray()).Skip(1).Any(), skipConsistencyTest: true);

            // Find a set of numbers for Stage 2 that works
            foreach (var constraintsPermutation in requiredConstraints.Permutations())
            {
                var greekLetterIxs = constraintsPermutation.Select(tup =>
                {
                    var isHoriz = Math.Abs(tup.sm - tup.la) == 1;
                    var firstIx = Math.Min(tup.sm, tup.la);
                    var ix = (firstIx % 4) + (isHoriz ? 3 : 4) * (firstIx / 4) + (isHoriz ? 0 : 12);
                    return ix + (tup.sm < tup.la ? 24 : 0);
                }).ToArray();

                var coordinates = greekLetterIxs.Select(ixRaw =>
                {
                    var ix = ixRaw % 24;
                    var x = ix < 12 ? 2 * (ix % 3) + 1 : 2 * ((ix - 12) % 4);
                    var y = ix < 12 ? 2 * (ix / 3) : 2 * ((ix - 12) / 4) + 1;
                    return (x, y);
                }).ToArray();

                static int sqDist((int x, int y) p1, (int x, int y) p2) => (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);
                var sqDists = Enumerable.Range(0, coordinates.Length).Select(ix => sqDist(coordinates[ix], coordinates[(ix + 1) % coordinates.Length])).ToArray();

                var n0 = givenIx % _sz + sqDists[0];
                var n1 = givenIx / _sz + sqDists[1 % sqDists.Length];
                var n2 = latinSquare[givenIx] + sqDists[2 % sqDists.Length];
                var n3candidates = Enumerable.Range(sqDists[3 % sqDists.Length] + 4, 6).ToList();
                for (var i = 1; i < sqDists.Length; i++)
                    if (sqDists[i] - n0 >= 0 && sqDists[i] - n0 < 4
                            && sqDists[(i + 1) % sqDists.Length] - n1 >= 0 && sqDists[(i + 1) % sqDists.Length] - n1 < 4
                            && sqDists[(i + 2) % sqDists.Length] - n2 >= 0 && sqDists[(i + 2) % sqDists.Length] - n2 < 4)
                        n3candidates.RemoveAll(n3c => sqDists[(i + 3) % sqDists.Length] - n3c >= 4);
                if (n3candidates.Count > 0)
                    return new NavyButtonPuzzle(latinSquare, givenIx, latinSquare[givenIx], greekLetterIxs, sqDists,
                        new[] { n0, n1, n2, n3candidates[rnd.Next(0, n3candidates.Count)] }, stencils.Concat(new[] { decoyStencilIx }).ToArray(), word)
                    {
                        XCoordinates = coordinates.Select(tup => tup.x).ToArray(),
                        YCoordinates = coordinates.Select(tup => tup.y).ToArray()
                    };
            }
            throw new InvalidOperationException();
        }

        private static IEnumerable<int[]> FindSolutions(int?[] sofar, (int sm, int la)[] constraints)
        {
            var ix = -1;
            int[] best = null;
            for (var i = 0; i < sofar.Length; i++)
            {
                var x = i % _sz;
                var y = i / _sz;
                if (sofar[i] != null)
                    continue;
                var taken = new bool[_sz];
                // Same row
                for (var c = 0; c < _sz; c++)
                    if (sofar[c + _sz * y] is int v)
                        taken[v] = true;
                // Same column
                for (var r = 0; r < _sz; r++)
                    if (sofar[x + _sz * r] is int v)
                        taken[v] = true;
                // Constraints
                if (constraints != null)
                {
                    foreach (var (sm, la) in constraints)
                    {
                        if (i == sm && sofar[la] != null) // i is the cell with the smaller value, so it can’t be anything larger than la
                            for (var ov = sofar[la].Value; ov < _sz; ov++)
                                taken[ov] = true;
                        else if (i == la && sofar[sm] != null)  // i is the cell with the larger value, so it can’t be anything smaller than sm
                            for (var ov = sofar[sm].Value; ov >= 0; ov--)
                                taken[ov] = true;
                    }
                }
                var values = taken.SelectIndexWhere(b => !b).ToArray();
                if (values.Length == 0)
                    yield break;
                if (best == null || values.Length < best.Length)
                {
                    ix = i;
                    best = values;
                    if (values.Length == 1)
                        goto shortcut;
                }
            }

            if (ix == -1)
            {
                yield return sofar.Select(i => i.Value).ToArray();
                yield break;
            }

            shortcut:
            for (var i = 0; i < best.Length; i++)
            {
                sofar[ix] = best[i];
                foreach (var solution in FindSolutions(sofar, constraints))
                    yield return solution;
            }
            sofar[ix] = null;
        }
    }
}