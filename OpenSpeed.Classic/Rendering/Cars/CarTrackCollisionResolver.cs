using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarTrackCollisionResolver
    {
        private static float CarHalfWidth => 1.0f;

        public static Matrix Resolve(
            Matrix world,
            IEnumerable<TrackRoutePoint> routePoints)
        {
            ArgumentNullException.ThrowIfNull(routePoints);

            TrackRoutePoint[] routePointArray = routePoints.ToArray();

            if (routePointArray.Length == 0)
            {
                return world;
            }

            TrackRoutePoint nearestRoutePoint = routePointArray
                .OrderBy(routePoint => Vector3.DistanceSquared(
                    world.Translation,
                    ToVector3(routePoint.Position)))
                .First();
            Vector3 right = ToVector3(nearestRoutePoint.Right);

            if (right.LengthSquared() == 0.0f)
            {
                return world;
            }

            right = Vector3.Normalize(right);
            Vector3 routePosition = ToVector3(nearestRoutePoint.Position);
            float lateralPosition = Vector3.Dot(
                world.Translation - routePosition,
                right);
            float minimumLateralPosition = -MathF.Max(
                0.0f,
                (float)nearestRoutePoint.LeftBorderDistance - CarHalfWidth);
            float maximumLateralPosition = MathF.Max(
                0.0f,
                (float)nearestRoutePoint.RightBorderDistance - CarHalfWidth);
            float resolvedLateralPosition = MathHelper.Clamp(
                lateralPosition,
                minimumLateralPosition,
                maximumLateralPosition);
            Vector3 resolvedPosition = world.Translation +
                right * (resolvedLateralPosition - lateralPosition);

            return Matrix.CreateWorld(resolvedPosition, world.Forward, world.Up);
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}