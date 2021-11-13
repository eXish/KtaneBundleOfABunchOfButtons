using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BlueButtonLib
{
    public sealed class BlueLatinSquare
    {
        public int this[int row, int col] => _resultScrambler[Rows[_rowScrambler[(row % 4 + 4) % 4]][_columnScrambler[(col % 4 + 4) % 4]]];
        //public static readonly ReadOnlyCollection<BlueLatinSquare> AllSquares = GenerateAllSquares();
        private readonly bool _isFirstBaseSquare;
        private readonly ReadOnlyCollection<int> _rowScrambler, _columnScrambler, _resultScrambler;

        public static BlueLatinSquare Random(int seed)
        {
            Random rng = new Random(seed);
            ReadOnlyCollection<int> rs = Array.AsReadOnly(new int[] { 0, 1, 2, 3 }.Shuffle(rng));
            ReadOnlyCollection<int> cs = Array.AsReadOnly(new int[] { 0, 1, 2, 3 }.Shuffle(rng));
            ReadOnlyCollection<int> rss = Array.AsReadOnly(new int[] { 0, 1, 2, 3 }.Shuffle(rng));
            return new BlueLatinSquare(rng.Next(2) == 1, rs, cs, rss);
        }

        public OrderedPair[] LocationsOf(int toFind)
        {
            if(!new int[] { 0, 1, 2, 3 }.Contains(toFind))
                throw new ArgumentException($"Cannot find {toFind} in a Latin Square!");

            List<OrderedPair> locs = new List<OrderedPair>();

            for(int x = 0; x < 4; x++)
                for(int y = 0; y < 4; y++)
                    if(this[x, y] == toFind)
                        locs.Add(new OrderedPair() { X = x, Y = y });
            return locs.ToArray();
        }

        /// <summary>
        /// This method is really, REALLY inefficient.
        /// </summary>
        public static ReadOnlyCollection<BlueLatinSquare> GenerateAllSquares()
        {
            List<BlueLatinSquare> squares = new List<BlueLatinSquare>() { new BlueLatinSquare(false), new BlueLatinSquare(true) };
            List<BlueLatinSquare> squares2 = new List<BlueLatinSquare>();
            List<BlueLatinSquare> squares3 = new List<BlueLatinSquare>();
            List<BlueLatinSquare> squares4 = new List<BlueLatinSquare>();
            foreach(BlueLatinSquare sq in squares)
                foreach(IEnumerable<int> r in new int[] { 0, 1, 2, 3 }.Permutations())
                    squares2.Add(sq.WithRows(Array.AsReadOnly(r.ToArray())));
            for(int i = squares2.Count - 1; i > 0; i--)
                if(squares2.Take(i).Any(l => l == squares2[i]))
                    squares2.RemoveAt(i);
            foreach(BlueLatinSquare sq in squares2)
                foreach(IEnumerable<int> c in new int[] { 0, 1, 2, 3 }.Permutations())
                    squares3.Add(sq.WithColumns(Array.AsReadOnly(c.ToArray())));
            for(int i = squares3.Count - 1; i > 0; i--)
                if(squares3.Take(i).Any(l => l == squares3[i]))
                    squares3.RemoveAt(i);
            foreach(BlueLatinSquare sq in squares3)
                foreach(IEnumerable<int> l in new int[] { 0, 1, 2, 3 }.Permutations())
                    squares4.Add(sq.WithLetters(Array.AsReadOnly(l.ToArray())));
            for(int i = squares4.Count - 1; i > 0; i--)
                if(squares4.Take(i).Any(l => l == squares4[i]))
                    squares4.RemoveAt(i);
            return Array.AsReadOnly(squares4.ToArray());
        }

        public BlueLatinSquare WithRows(ReadOnlyCollection<int> rs) => new BlueLatinSquare(_isFirstBaseSquare, rs, _columnScrambler, _resultScrambler);
        public BlueLatinSquare WithColumns(ReadOnlyCollection<int> cs) => new BlueLatinSquare(_isFirstBaseSquare, _rowScrambler, cs, _resultScrambler);
        public BlueLatinSquare WithLetters(ReadOnlyCollection<int> ls) => new BlueLatinSquare(_isFirstBaseSquare, _rowScrambler, _columnScrambler, ls);

        public BlueLatinSquare SwapRows(int a, int b)
        {
            int[] rs = new int[4];
            _rowScrambler.CopyTo(rs, 0);
            (rs[a], rs[b]) = (rs[b], rs[a]);
            return WithRows(Array.AsReadOnly(rs));
        }

        public BlueLatinSquare SwapColumns(int a, int b)
        {
            int[] cs = new int[4];
            _rowScrambler.CopyTo(cs, 0);
            (cs[a], cs[b]) = (cs[b], cs[a]);
            return WithColumns(Array.AsReadOnly(cs));
        }

        public BlueLatinSquare SwapLetters(int a, int b)
        {
            int[] rs = new int[4];
            _rowScrambler.CopyTo(rs, 0);
            (rs[a], rs[b]) = (rs[b], rs[a]);
            return WithLetters(Array.AsReadOnly(rs));
        }

        private BlueLatinSquare(bool isFirstBaseSquare, ReadOnlyCollection<int> rowScrambler = null, ReadOnlyCollection<int> columnScrambler = null, ReadOnlyCollection<int> resultScrambler = null)
        {
            _isFirstBaseSquare = isFirstBaseSquare;
            _rowScrambler = rowScrambler ?? Array.AsReadOnly(new int[] { 0, 1, 2, 3 });
            _columnScrambler = columnScrambler ?? Array.AsReadOnly(new int[] { 0, 1, 2, 3 });
            _resultScrambler = resultScrambler ?? Array.AsReadOnly(new int[] { 0, 1, 2, 3 });
        }

        private ReadOnlyCollection<LatinSquareRow> Rows => _isFirstBaseSquare ?
            Array.AsReadOnly(new LatinSquareRow[] {
                new LatinSquareRow(new int[] { 1, 2, 3, 0 }),
                new LatinSquareRow(new int[] { 2, 1, 0, 3 }),
                new LatinSquareRow(new int[] { 3, 0, 1, 2 }),
                new LatinSquareRow(new int[] { 0, 3, 2, 1 })
            }) :
            Array.AsReadOnly(new LatinSquareRow[] {
                new LatinSquareRow(new int[] { 1, 2, 0, 3 }),
                new LatinSquareRow(new int[] { 2, 1, 3, 0 }),
                new LatinSquareRow(new int[] { 3, 0, 1, 2 }),
                new LatinSquareRow(new int[] { 0, 3, 2, 1 })
            });

        public override bool Equals(object obj) => obj is BlueLatinSquare other
                ? new int[] { 0, 1, 2, 3 }.All(r =>
                    new int[] { 0, 1, 2, 3 }.All(j =>
                        _resultScrambler[Rows[_rowScrambler[r]][_columnScrambler[j]]] == _resultScrambler[other.Rows[other._rowScrambler[r]][other._columnScrambler[j]]]
                ))
                : base.Equals(obj);

        public override string ToString() => new int[] { 0, 1, 2, 3 }.Select(r =>
                new int[] { 0, 1, 2, 3 }.Select(c =>
                    _resultScrambler[Rows[_rowScrambler[r]][_columnScrambler[c]]].ToString()
                ).Aggregate((a, b) => a + b)
            ).Aggregate((a, b) => a + "|" + b);

        public override int GetHashCode()
        {
            int hashCode = 1356586536;
            hashCode = hashCode * -1521134295 + _isFirstBaseSquare.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ReadOnlyCollection<int>>.Default.GetHashCode(_rowScrambler);
            hashCode = hashCode * -1521134295 + EqualityComparer<ReadOnlyCollection<int>>.Default.GetHashCode(_columnScrambler);
            hashCode = hashCode * -1521134295 + EqualityComparer<ReadOnlyCollection<int>>.Default.GetHashCode(_resultScrambler);
            return hashCode;
        }

        public static bool operator ==(BlueLatinSquare first, BlueLatinSquare other) => first is null ? other is null : first.Equals(other);
        public static bool operator !=(BlueLatinSquare first, BlueLatinSquare other) => !(first == other);

        private sealed class LatinSquareRow
        {
            private readonly ReadOnlyCollection<int> _cells;

            internal LatinSquareRow(int[] cells)
            {
                _cells = Array.AsReadOnly(cells);
            }

            internal int this[int index] => _cells[index];
        }
    }
}
