using System;
using System.Collections.Generic;
using System.Linq;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Physics
{
    public sealed class TrackRoadGeometry
    {
        private readonly List<TrackRoadTriangle> triangles = [];

        public TrackRoadGeometry(IEnumerable<TrackBlock> blocks)
        {
            ArgumentNullException.ThrowIfNull(blocks);

            foreach (TrackBlock block in blocks)
            {
                foreach (TrackSurface surface in block.Surfaces)
                {
                    if (surface.DetailLevel != TrackGeometryDetailLevel.High &&
                        surface.DetailLevel != TrackGeometryDetailLevel.Unrestricted)
                    {
                        continue;
                    }

                    FixedVector[] points = surface.Points.Select(ConvertPoint).ToArray();

                    for (int index = 1; index + 1 < points.Length; index += 1)
                    {
                        AddTriangle([points[0], points[index], points[index + 1]]);
                    }
                }
            }
        }

        public bool TryApplyPlane(CarMemory car, int expectedHeight)
        {
            ArgumentNullException.ThrowIfNull(car);
            FixedVector position = FixedVectors.Read(car, 0x9C);
            FixedVector? selected = null;
            FixedVector? selectedNormal = null;
            long bestDistance = long.MaxValue;

            foreach (TrackRoadTriangle triangle in triangles)
            {
                if (!triangle.ContainsBounds(position) || !Contains(triangle.Points, position))
                {
                    continue;
                }

                FixedVector normal = triangle.Normal;
                int height = CalculateHeight(triangle.Points, position);
                long distance = Math.Abs((long)height - expectedHeight);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    selected = new FixedVector { First = position.First, Second = height, Third = position.Third };
                    selectedNormal = normal;
                }
            }

            if (selected is null || selectedNormal is null)
            {
                return false;
            }

            FixedVectors.Write(car, 0x13C, selected);
            FixedVectors.Write(car, 0x124, selectedNormal);

            return true;
        }

        private void AddTriangle(FixedVector[] points)
        {
            FixedVector normal = CalculateNormal(points);

            if (normal.Second < 0)
            {
                normal = FixedVectors.Subtract(new FixedVector(), normal);
            }

            if (normal.Second > 0x1999)
            {
                triangles.Add(new TrackRoadTriangle(points, normal));
            }
        }

        private static bool Contains(FixedVector[] triangle, FixedVector point)
        {
            Int128 first = Edge(triangle[0], triangle[1], point);
            Int128 second = Edge(triangle[1], triangle[2], point);
            Int128 third = Edge(triangle[2], triangle[0], point);

            return (first >= 0 && second >= 0 && third >= 0 ||
                first <= 0 && second <= 0 && third <= 0) &&
                Edge(triangle[0], triangle[1], triangle[2]) != 0;
        }

        private static FixedVector CalculateNormal(FixedVector[] triangle)
        {
            long firstHorizontal = (long)triangle[1].First - triangle[0].First;
            long firstVertical = (long)triangle[1].Second - triangle[0].Second;
            long firstDepth = (long)triangle[1].Third - triangle[0].Third;
            long secondHorizontal = (long)triangle[2].First - triangle[0].First;
            long secondVertical = (long)triangle[2].Second - triangle[0].Second;
            long secondDepth = (long)triangle[2].Third - triangle[0].Third;
            Int128 first = (Int128)firstVertical * secondDepth - (Int128)firstDepth * secondVertical;
            Int128 second = (Int128)firstDepth * secondHorizontal - (Int128)firstHorizontal * secondDepth;
            Int128 third = (Int128)firstHorizontal * secondVertical - (Int128)firstVertical * secondHorizontal;
            Int128 magnitude = Int128.Max(Int128.Abs(first), Int128.Max(Int128.Abs(second), Int128.Abs(third)));

            if (magnitude == 0)
            {
                return new FixedVector();
            }

            FixedVector normal = new()
            {
                First = (int)(first * X86Math.One / magnitude),
                Second = (int)(second * X86Math.One / magnitude),
                Third = (int)(third * X86Math.One / magnitude)
            };
            FixedVectors.NormaliseBasisVector(normal);

            return normal;
        }

        private static Int128 Edge(FixedVector first, FixedVector second, FixedVector point)
            => (Int128)((long)second.First - first.First) * ((long)point.Third - first.Third) -
                (Int128)((long)second.Third - first.Third) * ((long)point.First - first.First);

        private static int CalculateHeight(FixedVector[] points, FixedVector position)
        {
            Int128 area = Edge(points[0], points[1], points[2]);
            Int128 firstWeight = Edge(points[1], points[2], position);
            Int128 secondWeight = Edge(points[2], points[0], position);
            Int128 thirdWeight = Edge(points[0], points[1], position);
            Int128 numerator = firstWeight * points[0].Second +
                secondWeight * points[1].Second + thirdWeight * points[2].Second;

            return checked((int)(numerator / area));
        }

        private static FixedVector ConvertPoint(TrackPoint point) => new()
        {
            First = checked((int)Math.Round(point.X * X86Math.One)),
            Second = checked((int)Math.Round(point.Y * X86Math.One)),
            Third = checked((int)Math.Round(-point.Z * X86Math.One))
        };
    }
}