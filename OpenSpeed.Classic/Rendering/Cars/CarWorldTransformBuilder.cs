using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarWorldTransformBuilder
    {
        public static Matrix Build(IEnumerable<TrackRoutePoint> routePoints)
            => Build(routePoints, 0);

        public static Matrix Build(
            IEnumerable<TrackRoutePoint> routePoints,
            int initialRoutePointIndex)
        {
            ArgumentNullException.ThrowIfNull(routePoints);

            TrackRoutePoint[] routePointArray = routePoints.ToArray();

            if (routePointArray.Length == 0)
            {
                throw new ArgumentException(
                    "At least one track route point is required.",
                    nameof(routePoints));
            }

            int routePointIndex = Math.Clamp(
                initialRoutePointIndex,
                0,
                routePointArray.Length - 1);
            TrackRoutePoint routePoint = routePointArray[routePointIndex];
            Vector3 forward = NormaliseOrFallback(
                ToVector3(routePoint.Forward),
                Vector3.Forward);
            Vector3 up = NormaliseOrFallback(
                ToVector3(routePoint.Normal),
                Vector3.Up);

            if (Vector3.Cross(forward, up).LengthSquared() == 0.0f)
            {
                up = Vector3.Up;
            }

            return Matrix.CreateWorld(
                ToVector3(routePoint.Position),
                forward,
                up);
        }

        private static Vector3 NormaliseOrFallback(
            Vector3 direction,
            Vector3 fallback)
        {
            if (direction.LengthSquared() == 0.0f)
            {
                return fallback;
            }

            return Vector3.Normalize(direction);
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}