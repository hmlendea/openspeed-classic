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

        private static float GroundNormalVerticalRatioMinimum => 0.8f;

        public static IEnumerable<VertexPositionColor> BuildColoured(TrackSurface surface)
            => BuildColoured(surface, true);

        public static IEnumerable<VertexPositionColor> BuildColoured(
            TrackSurface surface,
            bool areShadowsEnabled)
        {
            ArgumentNullException.ThrowIfNull(surface);

            TrackPoint[] points = GetPoints(surface);
            Color[] lightingColours = BuildLightingColours(
                surface.LightingLevels,
                areShadowsEnabled);

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
            => BuildTextured(surface, material, true);

        public static IEnumerable<VertexPositionColorTexture> BuildTextured(
            TrackSurface surface,
            TrackMaterial material,
            bool areShadowsEnabled)
        {
            ArgumentNullException.ThrowIfNull(surface);
            ArgumentNullException.ThrowIfNull(material);

            TrackPoint[] points = GetPoints(surface);
            Color[] lightingColours = BuildLightingColours(
                surface.LightingLevels,
                areShadowsEnabled);
            Vector2[] textureCoordinates = TrackTextureCoordinateBuilder
                .Build(material)
                .ToArray();

            if (surface.Side == TrackSurfaceSide.Right)
            {
                textureCoordinates = ReflectAcrossLateralAxis(textureCoordinates);
            }
            else if (surface.Side == TrackSurfaceSide.Centre &&
                IsBackgroundWall(surface, points))
            {
                textureCoordinates = RotateByHalfTurn(textureCoordinates);
            }

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

        private static Color[] BuildLightingColours(
            ushort lightingLevels,
            bool areShadowsEnabled)
        {
            if (!areShadowsEnabled)
            {
                return [Color.White, Color.White, Color.White, Color.White];
            }

            return TrackSurfaceLighting.Build(lightingLevels).ToArray();
        }

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

        private static Vector2[] ReflectAcrossLateralAxis(
            Vector2[] textureCoordinates) =>
            [
                textureCoordinates[3],
                textureCoordinates[2],
                textureCoordinates[1],
                textureCoordinates[0]
            ];

        private static Vector2[] RotateByHalfTurn(Vector2[] textureCoordinates) =>
            [
                textureCoordinates[2],
                textureCoordinates[3],
                textureCoordinates[0],
                textureCoordinates[1]
            ];

        private static bool IsBackgroundWall(
            TrackSurface surface,
            TrackPoint[] points)
        {
            if (surface.Group == TrackSurfaceGroup.Unrestricted)
            {
                return false;
            }

            Vector3 firstEdge = ToVector3(points[1]) - ToVector3(points[0]);
            Vector3 secondEdge = ToVector3(points[2]) - ToVector3(points[0]);
            Vector3 normal = Vector3.Cross(firstEdge, secondEdge);

            if (normal.LengthSquared() == 0.0f || firstEdge.Y >= 0.0f)
            {
                return false;
            }

            float verticalRatio = MathF.Abs(normal.Y) / normal.Length();

            return verticalRatio < GroundNormalVerticalRatioMinimum;
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
