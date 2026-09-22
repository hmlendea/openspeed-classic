using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Tracks
{
    public static class TrackRoadShoulderSelector
    {
        private static double BorderTolerance => 0.75;

        private static double GroundNormalAlignmentMinimum => 0.8;

        private static double RoadHeightTolerance => 1.0;

        public static TrackSurfaceSide Select(
            TrackSurface surface,
            IEnumerable<TrackRoutePoint> routePoints)
        {
            ArgumentNullException.ThrowIfNull(surface);
            ArgumentNullException.ThrowIfNull(routePoints);

            TrackPoint[] points = surface.Points.ToArray();
            TrackRoutePoint[] routePointArray = routePoints.ToArray();

            if (points.Length != 4 || routePointArray.Length == 0)
            {
                return TrackSurfaceSide.Centre;
            }

            Vector3 centre = new(
                (float)points.Average(point => point.X),
                (float)points.Average(point => point.Y),
                (float)points.Average(point => point.Z));
            TrackRoutePoint routePoint = routePointArray.MinBy(point =>
                Vector3.DistanceSquared(centre, ToVector3(point.Position)))!;
            Vector3 normal = Vector3.Cross(
                ToVector3(points[1]) - ToVector3(points[0]),
                ToVector3(points[2]) - ToVector3(points[0]));
            Vector3 routeNormal = ToVector3(routePoint.Normal);
            Vector3 routeRight = ToVector3(routePoint.Right);

            if (normal.LengthSquared() == 0.0f ||
                routeNormal.LengthSquared() == 0.0f ||
                routeRight.LengthSquared() == 0.0f)
            {
                return TrackSurfaceSide.Centre;
            }

            normal.Normalize();
            routeNormal.Normalize();
            routeRight.Normalize();

            if (Math.Abs(Vector3.Dot(normal, routeNormal)) <
                    GroundNormalAlignmentMinimum ||
                Math.Abs(Vector3.Dot(
                    centre - ToVector3(routePoint.Position),
                    routeNormal)) > RoadHeightTolerance)
            {
                return TrackSurfaceSide.Centre;
            }

            double[] lateralPositions = points
                .Select(point => (double)Vector3.Dot(
                    ToVector3(point) - ToVector3(routePoint.Position),
                    routeRight))
                .ToArray();
            double minimumLateralPosition = lateralPositions.Min();
            double maximumLateralPosition = lateralPositions.Max();
            double leftBoundary = -routePoint.LeftBorderDistance;
            double rightBoundary = routePoint.RightBorderDistance;
            bool crossesLeftBoundary =
                minimumLateralPosition <= leftBoundary + BorderTolerance &&
                maximumLateralPosition > leftBoundary + BorderTolerance;
            bool crossesRightBoundary =
                maximumLateralPosition >= rightBoundary - BorderTolerance &&
                minimumLateralPosition < rightBoundary - BorderTolerance;

            if (crossesLeftBoundary && !crossesRightBoundary)
            {
                return TrackSurfaceSide.Left;
            }

            if (crossesRightBoundary && !crossesLeftBoundary)
            {
                return TrackSurfaceSide.Right;
            }

            return TrackSurfaceSide.Centre;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}
