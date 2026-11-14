using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class SunCalculator
    {
        public static float SunHeight => 1000.0f;

        public static Vector3 CalculateSunPosition(IEnumerable<TrackBlock> trackBlocks)
        {
            ArgumentNullException.ThrowIfNull(trackBlocks);

            Vector3[] centres = [.. trackBlocks.Select(trackBlock => ToVector3(trackBlock.Centre))];

            if (centres.Length == 0)
            {
                return new Vector3(0.0f, SunHeight, 0.0f);
            }

            Vector3 centre = Vector3.Zero;
            foreach (Vector3 blockCentre in centres)
            {
                centre += blockCentre;
            }
            centre /= centres.Length;

            return new Vector3(centre.X, SunHeight, centre.Z);
        }

        public static Vector3 CalculateSunPosition(IEnumerable<Vector3> centres)
        {
            Vector3[] centreArray = [.. centres];

            if (centreArray.Length == 0)
            {
                return new Vector3(0.0f, SunHeight, 0.0f);
            }

            Vector3 centre = Vector3.Zero;
            foreach (Vector3 blockCentre in centreArray)
            {
                centre += blockCentre;
            }
            centre /= centreArray.Length;

            return new Vector3(centre.X, SunHeight, centre.Z);
        }

        public static Vector3 CalculateSunDirection(IEnumerable<TrackBlock> trackBlocks)
        {
            Vector3 sunPosition = CalculateSunPosition(trackBlocks);
            Vector3 trackCentre = CalculateTrackCentre(trackBlocks);
            Vector3 direction = trackCentre - sunPosition;

            if (direction.LengthSquared() == 0.0f)
            {
                return Vector3.Down;
            }

            return Vector3.Normalize(direction);
        }

        private static Vector3 CalculateTrackCentre(IEnumerable<TrackBlock> trackBlocks)
        {
            Vector3[] centres = [.. trackBlocks.Select(trackBlock => ToVector3(trackBlock.Centre))];

            if (centres.Length == 0)
            {
                return Vector3.Zero;
            }

            Vector3 centre = Vector3.Zero;
            foreach (Vector3 blockCentre in centres)
            {
                centre += blockCentre;
            }
            centre /= centres.Length;

            return centre;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);
    }
}