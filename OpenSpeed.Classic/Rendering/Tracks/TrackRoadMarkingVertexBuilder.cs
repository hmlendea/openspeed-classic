using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackRoadMarkingVertexBuilder
    {
        private static Color MarkingColour => new(224, 224, 208);

        private static float HalfWidth => 0.08f;

        private static float MinimumSegmentLengthSquared => 0.0001f;

        private static float SurfaceOffset => 0.02f;

        public static IEnumerable<VertexPositionColor> Build(TrackRoadMarking roadMarking)
        {
            ArgumentNullException.ThrowIfNull(roadMarking);

            TrackPoint[] points = roadMarking.Points.ToArray();
            List<VertexPositionColor> vertices = [];

            for (int pointIndex = 0; pointIndex < points.Length - 1; pointIndex += 1)
            {
                Vector3 start = ToVector3(points[pointIndex]);
                Vector3 end = ToVector3(points[pointIndex + 1]);
                Vector3 segment = end - start;
                Vector3 lateral = Vector3.Cross(segment, Vector3.Up);

                if (lateral.LengthSquared() <= MinimumSegmentLengthSquared)
                {
                    continue;
                }

                lateral.Normalize();
                lateral *= HalfWidth;
                Vector3 surfaceOffset = Vector3.Up * SurfaceOffset;
                Vector3 startLeft = start - lateral + surfaceOffset;
                Vector3 startRight = start + lateral + surfaceOffset;
                Vector3 endLeft = end - lateral + surfaceOffset;
                Vector3 endRight = end + lateral + surfaceOffset;
                vertices.Add(new VertexPositionColor(startLeft, MarkingColour));
                vertices.Add(new VertexPositionColor(startRight, MarkingColour));
                vertices.Add(new VertexPositionColor(endRight, MarkingColour));
                vertices.Add(new VertexPositionColor(startLeft, MarkingColour));
                vertices.Add(new VertexPositionColor(endRight, MarkingColour));
                vertices.Add(new VertexPositionColor(endLeft, MarkingColour));
            }

            return vertices;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);
    }
}