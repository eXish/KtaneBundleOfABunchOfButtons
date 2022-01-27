using System;

namespace BlueButtonLib
{
    public class Cube
    {
        internal Cube(int[] faces)
        {
            if(faces?.Length != 6)
                throw new ArgumentException($"Bad number of faces: {faces?.Length}");
            Top = faces[0];
            Bottom = faces[1];
            Left = faces[2];
            Right = faces[3];
            Back = faces[4];
            Front = faces[5];
        }

        public int Top { get; }
        public int Bottom { get; }
        public int Left { get; }
        public int Right { get; }
        public int Back { get; }
        public int Front { get; }

        public Cube Rotate(Rotation r) => r switch
        {
            Rotation.XY => new Cube(new int[] { Top, Bottom, Front, Back, Left, Right }),
            Rotation.YX => new Cube(new int[] { Top, Bottom, Back, Front, Right, Left }),
            Rotation.ZY => new Cube(new int[] { Back, Front, Left, Right, Bottom, Top }),
            Rotation.YZ => new Cube(new int[] { Front, Back, Left, Right, Top, Bottom }),
            _ => new Cube(new int[] { Top, Bottom, Left, Right, Back, Front }),
        };

        public enum Rotation
        {
            XY,
            YX,
            ZY,
            YZ
        }

        public override string ToString()
        {
            return $"(T:{Top} B:{Bottom} L:{Left} R:{Right} B:{Back} F:{Front})";
        }
    }
}
