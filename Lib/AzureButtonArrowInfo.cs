using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueButtonLib
{
    public sealed class AzureButtonArrowInfo
    {
        public Coord[] Coordinates { get; private set; }
        public int[] Directions { get; private set; }
        public int Rotation { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public string ModelName { get; private set; }

        public AzureButtonArrowInfo(Coord[] coordinates, int[] directions, float minX, float maxX, float minY, float maxY)
        {
            Coordinates = coordinates;
            Directions = directions;

            var rotation = directions[0] / 2;
            Width = maxX - minX;
            Height = maxY - minY;
            CenterX = (minX + maxX) / 2f;
            CenterY = (minY + maxY) / 2f;
            Rotation = 90 * rotation;
            ModelName = $"Arrow-{directions.Select(d => (8 + d - 2 * rotation) % 8).JoinString()}";
        }

        public static readonly AzureButtonArrowInfo[] AllArrows;

        public const int MaxArrowLength = 3;

        static AzureButtonArrowInfo()
        {
            // Generates all possible arrows that don’t intersect with themselves
            var dxs = new[] { 0, 1, 1, 1, 0, -1, -1, -1 };
            var dys = new[] { -1, -1, 0, 1, 1, 1, 0, -1 };

            IEnumerable<AzureButtonArrowInfo> generateArrows(Coord[] coordsSofar, int[] dirsSofar, float minX, float maxX, float minY, float maxY, int x, int y)
            {
                if (coordsSofar.Length == MaxArrowLength)
                {
                    yield return new AzureButtonArrowInfo(coordsSofar, dirsSofar, minX, maxX, minY, maxY);
                    yield break;
                }

                var start = coordsSofar.Length == 0 ? new Coord(4, 4, 0, 0) : coordsSofar.Last();
                for (var dir = 0; dir < 8; dir++)
                {
                    var next = start.AddWrap(dxs[dir], dys[dir]);
                    var newX = x + dxs[dir];
                    var newY = y + dys[dir];
                    if (next.Index != 0 && !coordsSofar.Contains(next))
                        foreach (var result in generateArrows(
                                insert(coordsSofar, coordsSofar.Length, next),
                                insert(dirsSofar, dirsSofar.Length, dir),
                                Math.Min(minX, newX),
                                Math.Max(maxX, newX),
                                Math.Min(minY, newY),
                                Math.Max(maxY, newY),
                                newX,
                                newY))
                            yield return result;
                }
            }
            AllArrows = generateArrows(new Coord[0], new int[0], -.5f, .5f, -.5f, .5f, 0, 0).ToArray();
        }

        /// <summary>
        ///     Similar to <see cref="string.Insert(int, string)"/>, but for arrays and for a single value. Returns a new
        ///     array with the <paramref name="value"/> inserted at the specified <paramref name="startIndex"/>.</summary>
        private static T[] insert<T>(T[] array, int startIndex, T value)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be between 0 and the size of the input array.");
            T[] result = new T[array.Length + 1];
            Array.Copy(array, 0, result, 0, startIndex);
            result[startIndex] = value;
            Array.Copy(array, startIndex, result, startIndex + 1, array.Length - startIndex);
            return result;
        }
    }
}
