using System;
using System.Collections.Generic;

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

            TrackRouteProjection? routeProjection = TrackRouteProjector.Project(
                world.Translation,
                routePoints);

            return Resolve(world, routeProjection);
        }

        internal static Matrix Resolve(
            Matrix world,
            TrackRouteProjection? routeProjection)
        {
            if (routeProjection is null || routeProjection.Right.LengthSquared() == 0.0f)
            {
                return world;
            }

            float lateralPosition = Vector3.Dot(
                world.Translation - routeProjection.Position,
                routeProjection.Right);
            float minimumLateralPosition = -MathF.Max(
                0.0f,
                routeProjection.LeftBorderDistance - CarHalfWidth);
            float maximumLateralPosition = MathF.Max(
                0.0f,
                routeProjection.RightBorderDistance - CarHalfWidth);
            float resolvedLateralPosition = MathHelper.Clamp(
                lateralPosition,
                minimumLateralPosition,
                maximumLateralPosition);
            Vector3 resolvedPosition = world.Translation +
                routeProjection.Right * (resolvedLateralPosition - lateralPosition);

            return Matrix.CreateWorld(resolvedPosition, world.Forward, world.Up);
        }
    }
}