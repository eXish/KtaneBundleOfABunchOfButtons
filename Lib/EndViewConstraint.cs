using System;
using System.Linq;

namespace BlueButtonLib
{
    public struct EndViewConstraint
    {
        public CardinalDirection Direction { get; }
        public int Index { get; }
        public int Letter { get; }

        public EndViewConstraint(CardinalDirection dir, int ix, int l)
        {
            Direction = dir;
            Index = ix;
            Letter = l;
        }

        public static EndViewConstraint[] AllFromBlueLatinSquare(BlueLatinSquare square)
        {
            EndViewConstraint[] allConstraints = new EndViewConstraint[16];
            for(int i = 0; i < 4; i++)
            {
                if(square[0, i] != 0)
                    allConstraints[i] = new EndViewConstraint(CardinalDirection.Up, i, square[0, i]);
                else
                    allConstraints[i] = new EndViewConstraint(CardinalDirection.Up, i, square[1, i]);
            }
            for(int i = 0; i < 4; i++)
            {
                if(square[i, 3] != 0)
                    allConstraints[i + 4] = new EndViewConstraint(CardinalDirection.Right, i, square[i, 3]);
                else
                    allConstraints[i + 4] = new EndViewConstraint(CardinalDirection.Right, i, square[i, 2]);
            }
            for(int i = 0; i < 4; i++)
            {
                if(square[3, 3 - i] != 0)
                    allConstraints[i + 8] = new EndViewConstraint(CardinalDirection.Down, i, square[3, 3 - i]);
                else
                    allConstraints[i + 8] = new EndViewConstraint(CardinalDirection.Down, i, square[2, 3 - i]);
            }
            for(int i = 0; i < 4; i++)
            {
                if(square[3 - i, 0] != 0)
                    allConstraints[i + 12] = new EndViewConstraint(CardinalDirection.Left, i, square[3 - i, 0]);
                else
                    allConstraints[i + 12] = new EndViewConstraint(CardinalDirection.Left, i, square[3 - i, 1]);
            }
            return allConstraints;
        }

        internal static bool IsUnique(EndViewConstraint[] test)
        {
            return BlueLatinSquare.AllSquares.Count(s => test.All(c => AllFromBlueLatinSquare(s).Contains(c))) == 1;
        }
    }
}
