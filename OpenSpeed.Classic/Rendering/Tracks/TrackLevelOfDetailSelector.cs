using System;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackLevelOfDetailSelector
    {
        private static float HighDetailDistance => 110.0f;

        private static float MediumDetailDistance => 150.0f;

        public static TrackGeometryDetailLevel Select(float horizontalDistanceSquared)
        {
            if (!float.IsFinite(horizontalDistanceSquared) ||
                horizontalDistanceSquared < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(horizontalDistanceSquared),
                    horizontalDistanceSquared,
                    "The horizontal squared distance must be finite and non-negative.");
            }

            if (horizontalDistanceSquared > MediumDetailDistance * MediumDetailDistance)
            {
                return TrackGeometryDetailLevel.Low;
            }

            if (horizontalDistanceSquared <= HighDetailDistance * HighDetailDistance)
            {
                return TrackGeometryDetailLevel.High;
            }

            return TrackGeometryDetailLevel.Medium;
        }
    }
}