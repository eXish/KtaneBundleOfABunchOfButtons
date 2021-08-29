using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButton
{
    public class MazeLayout
    {
        private int _w;
        private int _h;
        private bool[] _canGoRightData;
        private bool[] _canGoDownData;

        public bool CanGoRight(int cell) { return CanGoRight(cell % _w, cell / _w); }
        public bool CanGoRight(int x, int y) { return x < _w - 1 && _canGoRightData[x + (_w - 1) * y]; }
        public bool CanGoDown(int cell) { return CanGoDown(cell % _w, cell / _w); }
        public bool CanGoDown(int x, int y) { return y < _h - 1 && _canGoDownData[x + _w * y]; }
        public bool CanGoLeft(int cell) { return CanGoLeft(cell % _w, cell / _w); }
        public bool CanGoLeft(int x, int y) { return x > 0 && _canGoRightData[x - 1 + (_w - 1) * y]; }
        public bool CanGoUp(int cell) { return CanGoUp(cell % _w, cell / _w); }
        public bool CanGoUp(int x, int y) { return y > 0 && _canGoDownData[x + _w * (y - 1)]; }

        public bool CanGo(int cell, int direction)
        {
            switch (direction)
            {
                case 0: return CanGoUp(cell);
                case 1: return CanGoRight(cell);
                case 2: return CanGoDown(cell);
                default: return CanGoLeft(cell);
            }
        }

        public int Move(int cell, int direction)
        {
            var x = cell % _w;
            var y = cell / _w;
            switch (direction)
            {
                case 0: if (y == 0) throw new InvalidOperationException("Maze: can’t go up from the top edge."); return x + _w * (y - 1);
                case 1: if (x == _w - 1) throw new InvalidOperationException("Maze: can’t go right from the rightmost edge."); return x + 1 + _w * y;
                case 2: if (y == _h - 1) throw new InvalidOperationException("Maze: can’t go down from the bottom edge."); return x + _w * (y + 1);
                default: if (x == 0) throw new InvalidOperationException("Maze: can’t go left from the leftmost edge."); return x - 1 + _w * y;
            }
        }

        public static MazeLayout Generate(int w, int h, MonoRandom rnd)
        {
            var canGoRight = new bool[(w - 1) * h];
            var canGoDown = new bool[w * (h - 1)];

            var todo = new List<int>(Enumerable.Range(0, w * h));
            var active = new List<int>();

            var start = rnd.Next(0, todo.Count);
            active.Add(todo[start]);
            todo.RemoveAt(start);

            while (todo.Count > 0)
            {
                var activeIx = rnd.Next(0, active.Count);
                var sq = active[activeIx];
                var adjs = new List<int>();
                if (sq % w > 0 && todo.Contains(sq - 1))
                    adjs.Add(sq - 1);
                if (sq % w < w - 1 && todo.Contains(sq + 1))
                    adjs.Add(sq + 1);
                if (sq / w > 0 && todo.Contains(sq - w))
                    adjs.Add(sq - w);
                if (sq / w < h - 1 && todo.Contains(sq + w))
                    adjs.Add(sq + w);

                if (adjs.Count == 0)
                {
                    active.RemoveAt(activeIx);
                    continue;
                }
                else
                {
                    var adj = adjs[rnd.Next(0, adjs.Count)];
                    todo.Remove(adj);
                    active.Add(adj);

                    if (adj == sq - 1)
                        canGoRight[adj % w + (w - 1) * (adj / w)] = true;
                    else if (adj == sq + 1)
                        canGoRight[sq % w + (w - 1) * (sq / w)] = true;
                    else if (adj == sq - w)
                        canGoDown[adj] = true;
                    else if (adj == sq + w)
                        canGoDown[sq] = true;
                }
            }

            return new MazeLayout { _w = w, _h = h, _canGoDownData = canGoDown, _canGoRightData = canGoRight };
        }
    }
}