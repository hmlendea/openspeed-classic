using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering
{
    public static class MinimapRoadVertexBuilder
    {
        private static float DefaultLineWidth => 8.0f;

        private static int JoinSegmentCount => 12;

        private static Color RoadColour => new(210, 215, 215);

        public static IEnumerable<VertexPositionColor> Build(IEnumerable<TrackRoutePoint> routePoints)
            => Build(routePoints, DefaultLineWidth);

        public static IEnumerable<VertexPositionColor> Build(
            IEnumerable<TrackRoutePoint> routePoints,
            float lineWidth)
        {
            ArgumentNullException.ThrowIfNull(routePoints);

            if (!float.IsFinite(lineWidth) || lineWidth <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lineWidth),
                    lineWidth,
                    "The minimap line width must be finite and positive.");
            }

            TrackRoutePoint[] route = [.. routePoints];
            List<VertexPositionColor> vertices = [];

            if (route.Length < 2)
            {
                return vertices;
            }

            for (int pointIndex = 0; pointIndex < route.Length; pointIndex += 1)
            {
                AddSegment(
                    vertices,
                    ToMapPosition(route[pointIndex]),
                    ToMapPosition(route[(pointIndex + 1) % route.Length]),
                    lineWidth / 2.0f);
            }

            return vertices;
        }

        private static void AddSegment(
            List<VertexPositionColor> vertices,
            Vector3 start,
            Vector3 end,
            float halfWidth)
        {
            Vector3 direction = end - start;

            if (direction.LengthSquared() == 0.0f)
            {
                return;
            }

            Vector3 offset = Vector3.Normalize(new Vector3(-direction.Z, 0.0f, direction.X)) * halfWidth;
            vertices.AddRange([
                new(start - offset, RoadColour),
                new(start + offset, RoadColour),
                new(end + offset, RoadColour),
                new(start - offset, RoadColour),
                new(end + offset, RoadColour),
                new(end - offset, RoadColour)
            ]);
            AddRoundJoin(vertices, start, halfWidth);
        }

        private static void AddRoundJoin(
            List<VertexPositionColor> vertices,
            Vector3 centre,
            float radius)
        {
            for (int segmentIndex = 0; segmentIndex < JoinSegmentCount; segmentIndex += 1)
            {
                float startAngle = MathHelper.TwoPi * segmentIndex / JoinSegmentCount;
                float endAngle = MathHelper.TwoPi * (segmentIndex + 1) / JoinSegmentCount;
                vertices.Add(new VertexPositionColor(centre, RoadColour));
                vertices.Add(new VertexPositionColor(
                    centre + new Vector3(MathF.Cos(startAngle), 0.0f, MathF.Sin(startAngle)) * radius,
                    RoadColour));
                vertices.Add(new VertexPositionColor(
                    centre + new Vector3(MathF.Cos(endAngle), 0.0f, MathF.Sin(endAngle)) * radius,
                    RoadColour));
            }
        }

        private static Vector3 ToMapPosition(TrackRoutePoint routePoint)
            => new((float)routePoint.Position.X, 0.0f, (float)routePoint.Position.Z);
    }
}