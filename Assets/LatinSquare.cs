using System.Collections.Generic;
using System.Linq;

namespace BunchOfButtons
{
    static class LatinSquare
    {
        public static int[] Generate(MonoRandom rnd, int size)
        {
            return generateColorGrid(rnd, size, new int?[size * size],
                Enumerable.Range(0, size * size).Select(_ => Enumerable.Range(0, size).ToList()).ToArray());
        }

        private static int[] generateColorGrid(MonoRandom rnd, int size, int?[] sofar, List<int>[] available)
        {
            var ixs = new List<int>();
            var lowest = int.MaxValue;
            for (var sq = 0; sq < size * size; sq++)
            {
                if (sofar[sq] != null)
                    continue;
                if (available[sq].Count < lowest)
                {
                    ixs.Clear();
                    ixs.Add(sq);
                    lowest = available[sq].Count;
                }
                else if (available[sq].Count == lowest)
                    ixs.Add(sq);
                if (lowest == 1)
                    break;
            }

            if (ixs.Count == 0)
                return sofar.Select(i => i.Value).ToArray();

            var square = ixs[rnd.Next(0, ixs.Count)];
            var offset = rnd.Next(0, available[square].Count);
            for (var fAvIx = 0; fAvIx < available[square].Count; fAvIx++)
            {
                var avIx = (fAvIx + offset) % available[square].Count;
                var v = available[square][avIx];
                sofar[square] = v;

                var result = generateColorGrid(rnd, size, sofar, processAvailable(available, square, v, size));
                if (result != null)
                    return result;
            }
            sofar[square] = null;
            return null;
        }

        private static List<int>[] processAvailable(List<int>[] available, int sq, int v, int size)
        {
            var newAvailable = available.ToArray();
            for (var c = 0; c < size; c++)
            {
                var avIx = c + size * (sq / size);
                var ix = newAvailable[avIx].IndexOf(v);
                if (ix != -1)
                {
                    newAvailable[avIx] = newAvailable[avIx].ToList();
                    newAvailable[avIx].RemoveAt(ix);
                }
            }
            for (var r = 0; r < size; r++)
            {
                var avIx = (sq % size) + size * r;
                var ix = newAvailable[avIx].IndexOf(v);
                if (ix != -1)
                {
                    newAvailable[avIx] = newAvailable[avIx].ToList();
                    newAvailable[avIx].RemoveAt(ix);
                }
            }
            return newAvailable;
        }
    }
}
