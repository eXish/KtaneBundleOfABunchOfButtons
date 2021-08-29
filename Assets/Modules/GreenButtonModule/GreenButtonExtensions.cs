using System;
using System.Linq;

public static class GreenButtonExtensions
{
    public static int[] ToNewYorkPoint(this char c)
    {
        switch(c.ToString().ToLowerInvariant().First())
        {
            case 'a':
                return new int[] { 1, 1, 0 };
            case 'b':
                return new int[] { 3, 1, 1, 0 };
            case 'c':
                return new int[] { 1, 1, 2, 0 };
            case 'd':
                return new int[] { 1, 3, 0 };
            case 'e':
                return new int[] { 1, 0 };
            case 'f':
                return new int[] { 1, 1, 1, 0 };
            case 'g':
                return new int[] { 2, 2, 3, 0 };
            case 'h':
                return new int[] { 2, 3, 3, 0 };
            case 'i':
                return new int[] { 3, 0 };
            case 'j':
                return new int[] { 1, 3, 1, 0 };
            case 'k':
                return new int[] { 1, 1, 3, 0 };
            case 'l':
                return new int[] { 3, 2, 0 };
            case 'm':
                return new int[] { 3, 1, 0 };
            case 'n':
                return new int[] { 2, 2, 0 };
            case 'o':
                return new int[] { 2, 1, 0 };
            case 'p':
                return new int[] { 1, 2, 2, 0 };
            case 'q':
                return new int[] { 3, 2, 2, 0 };
            case 'r':
                return new int[] { 2, 3, 0 };
            case 's':
                return new int[] { 1, 2, 0 };
            case 't':
                return new int[] { 2, 0 };
            case 'u':
                return new int[] { 2, 2, 2, 0 };
            case 'v':
                return new int[] { 1, 2, 1, 0 };
            case 'w':
                return new int[] { 2, 2, 1, 0 };
            case 'x':
                return new int[] { 3, 2, 3, 0 };
            case 'y':
                return new int[] { 2, 1, 2, 0 };
            case 'z':
                return new int[] { 1, 3, 3, 0 };
        }
        throw new ArgumentException();
    }
}