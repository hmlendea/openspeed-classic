using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    internal static class TrackRouteProjector
    {
        internal static TrackRouteProjection? Project(
            Vector3 worldPosition,
            IEnumerable<TrackRoutePoint> routePoints)
        {
            ArgumentNullException.ThrowIfNull(routePoints);

            TrackRoutePoint[] routePointArray = routePoints.ToArray();

            if (routePointArray.Length == 0)
            {
                return null;
            }

            if (routePointArray.Length == 1)
            {
                return CreateProjection(routePointArray[0], routePointArray[0], 0.0f);
            }

            TrackRoutePoint? nearestStart = null;
            TrackRoutePoint? nearestEnd = null;
            float nearestInterpolation = 0.0f;
            float nearestDistanceSquared = float.MaxValue;

            for (int routePointIndex = 0;
                routePointIndex < routePointArray.Length;
                routePointIndex += 1)
            {
                TrackRoutePoint start = routePointArray[routePointIndex];
                int endIndex = (routePointIndex + 1) % routePointArray.Length;
                TrackRoutePoint end = routePointArray[endIndex];
                Vector3 startPosition = ToVector3(start.Position);
                Vector3 endPosition = ToVector3(end.Position);
                Vector2 segment = new(
                    endPosition.X - startPosition.X,
                    endPosition.Z - startPosition.Z);
                float segmentLengthSquared = segment.LengthSquared();

                if (segmentLengthSquared == 0.0f)
                {
                    continue;
                }

                Vector2 positionFromStart = new(
                    worldPosition.X - startPosition.X,
                    worldPosition.Z - startPosition.Z);
                float interpolation = MathHelper.Clamp(
                    Vector2.Dot(positionFromStart, segment) / segmentLengthSquared,
                    0.0f,
                    1.0f);
                Vector3 projectedPosition = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    interpolation);
                float distanceSquared = HorizontalDistanceSquared(
                    worldPosition,
                    projectedPosition);

                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestStart = start;
                    nearestEnd = end;
                    nearestInterpolation = interpolation;
                }
            }

            if (nearestStart is not null && nearestEnd is not null)
            {
                return CreateProjection(
                    nearestStart,
                    nearestEnd,
                    nearestInterpolation);
            }

            TrackRoutePoint nearestRoutePoint = routePointArray.MinBy(routePoint =>
                HorizontalDistanceSquared(
                    worldPosition,
                    ToVector3(routePoint.Position)))!;

            return CreateProjection(nearestRoutePoint, nearestRoutePoint, 0.0f);
        }

        private static TrackRouteProjection CreateProjection(
            TrackRoutePoint start,
            TrackRoutePoint end,
            float interpolation)
            => new()
            {
                Forward = NormaliseOrZero(Vector3.Lerp(
                    ToVector3(start.Forward),
                    ToVector3(end.Forward),
                    interpolation)),
                LeftBorderDistance = MathHelper.Lerp(
                    (float)start.LeftBorderDistance,
                    (float)end.LeftBorderDistance,
                    interpolation),
                Normal = NormaliseOrZero(Vector3.Lerp(
                    ToVector3(start.Normal),
                    ToVector3(end.Normal),
                    interpolation)),
                Position = Vector3.Lerp(
                    ToVector3(start.Position),
                    ToVector3(end.Position),
                    interpolation),
                Right = NormaliseOrZero(Vector3.Lerp(
                    ToVector3(start.Right),
                    ToVector3(end.Right),
                    interpolation)),
                RightBorderDistance = MathHelper.Lerp(
                    (float)start.RightBorderDistance,
                    (float)end.RightBorderDistance,
                    interpolation)
            };

        private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
        {
            float differenceX = first.X - second.X;
            float differenceZ = first.Z - second.Z;

            return differenceX * differenceX + differenceZ * differenceZ;
        }

        private static Vector3 NormaliseOrZero(Vector3 vector)
        {
            if (vector.LengthSquared() == 0.0f)
            {
                return Vector3.Zero;
            }

            return Vector3.Normalize(vector);
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}