using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackSurfaceVertexBuilder
    {
        private static Color DefaultSurfaceColour => new(72, 88, 80);

        private static int MaximumColourValue => byte.MaxValue;

        private static int SurfacePointCount => 4;

        public static IEnumerable<VertexPositionColor> BuildColoured(TrackSurface surface)
        {
            ArgumentNullException.ThrowIfNull(surface);

            TrackPoint[] points = GetPoints(surface);
            Color[] lightingColours = TrackSurfaceLighting
                .Build(surface.LightingLevels)
                .ToArray();

            return
            [
                CreateColouredVertex(points[0], lightingColours[0]),
                CreateColouredVertex(points[1], lightingColours[1]),
                CreateColouredVertex(points[2], lightingColours[2]),
                CreateColouredVertex(points[0], lightingColours[0]),
                CreateColouredVertex(points[2], lightingColours[2]),
                CreateColouredVertex(points[3], lightingColours[3])
            ];
        }

        public static IEnumerable<VertexPositionColorTexture> BuildTextured(
            TrackSurface surface,
            TrackMaterial material)
        {
            ArgumentNullException.ThrowIfNull(surface);
            ArgumentNullException.ThrowIfNull(material);

            TrackPoint[] points = GetPoints(surface);
            Color[] lightingColours = TrackSurfaceLighting
                .Build(surface.LightingLevels)
                .ToArray();
            Vector2[] textureCoordinates = TrackTextureCoordinateBuilder
                .Build(material)
                .ToArray();

            return
            [
                CreateTexturedVertex(
                    points[0],
                    lightingColours[0],
                    textureCoordinates[0]),
                CreateTexturedVertex(
                    points[1],
                    lightingColours[1],
                    textureCoordinates[1]),
                CreateTexturedVertex(
                    points[2],
                    lightingColours[2],
                    textureCoordinates[2]),
                CreateTexturedVertex(
                    points[0],
                    lightingColours[0],
                    textureCoordinates[0]),
                CreateTexturedVertex(
                    points[2],
                    lightingColours[2],
                    textureCoordinates[2]),
                CreateTexturedVertex(
                    points[3],
                    lightingColours[3],
                    textureCoordinates[3])
            ];
        }

        private static VertexPositionColor CreateColouredVertex(
            TrackPoint point,
            Color lightingColour)
            => new(ToVector3(point), ApplyLighting(lightingColour));

        private static VertexPositionColorTexture CreateTexturedVertex(
            TrackPoint point,
            Color lightingColour,
            Vector2 textureCoordinate)
            => new(ToVector3(point), lightingColour, textureCoordinate);

        private static TrackPoint[] GetPoints(TrackSurface surface)
        {
            TrackPoint[] points = surface.Points.ToArray();

            if (points.Length != SurfacePointCount)
            {
                throw new InvalidDataException(
                    $"Track surface {surface.Identifier} contains {points.Length} points; " +
                    $"exactly {SurfacePointCount} are required.");
            }

            return points;
        }

        private static Color ApplyLighting(Color lightingColour)
            => new(
                DefaultSurfaceColour.R * lightingColour.R / MaximumColourValue,
                DefaultSurfaceColour.G * lightingColour.G / MaximumColourValue,
                DefaultSurfaceColour.B * lightingColour.B / MaximumColourValue);

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);
    }
}