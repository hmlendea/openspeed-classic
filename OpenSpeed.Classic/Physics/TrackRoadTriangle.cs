using System;

namespace OpenSpeed.Classic.Physics
{
    internal sealed class TrackRoadTriangle(FixedVector[] points, FixedVector normal)
    {
        public FixedVector[] Points { get; } = points;

        public FixedVector Normal { get; } = normal;

        public int MinimumFirst { get; } = Math.Min(points[0].First, Math.Min(points[1].First, points[2].First));

        public int MaximumFirst { get; } = Math.Max(points[0].First, Math.Max(points[1].First, points[2].First));

        public int MinimumThird { get; } = Math.Min(points[0].Third, Math.Min(points[1].Third, points[2].Third));

        public int MaximumThird { get; } = Math.Max(points[0].Third, Math.Max(points[1].Third, points[2].Third));

        public bool ContainsBounds(FixedVector point)
            => point.First >= MinimumFirst && point.First <= MaximumFirst &&
                point.Third >= MinimumThird && point.Third <= MaximumThird;
    }
}