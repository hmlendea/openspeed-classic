using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Cars;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarGeometryVertexBuilder
    {
        private static readonly Vector2[] TextureCoordinates =
        [
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f)
        ];

        private static float CoordinateScale => 65536.0f;

        private static int VertexFractionShift => 8;

        private static float ShadowIntensityFactor => 0.3f;

        public static IEnumerable<VertexPositionColor> BuildColoured(
            CarGeometryTriangle triangle,
            Color colour)
            => BuildColoured(triangle, colour, Vector3.Down);

        public static IEnumerable<VertexPositionColor> BuildColoured(
            CarGeometryTriangle triangle,
            Color colour,
            Vector3 sunDirection)
        {
            ArgumentNullException.ThrowIfNull(triangle);

            Color[] lightingColours = BuildLightingColours(triangle, colour, sunDirection);

            return
            [
                new VertexPositionColor(ToVector3(triangle, triangle.First), lightingColours[0]),
                new VertexPositionColor(ToVector3(triangle, triangle.Second), lightingColours[1]),
                new VertexPositionColor(ToVector3(triangle, triangle.Third), lightingColours[2])
            ];
        }

        public static IEnumerable<VertexPositionColorTexture> BuildTextured(
            CarGeometryTriangle triangle,
            Color colour)
            => BuildTextured(triangle, colour, Vector3.Down);

        public static IEnumerable<VertexPositionColorTexture> BuildTextured(
            CarGeometryTriangle triangle,
            Color colour,
            Vector3 sunDirection)
        {
            ArgumentNullException.ThrowIfNull(triangle);

            Color[] lightingColours = BuildLightingColours(triangle, colour, sunDirection);

            return
            [
                CreateTexturedVertex(
                    triangle,
                    triangle.First,
                    triangle.FirstTextureCorner,
                    lightingColours[0]),
                CreateTexturedVertex(
                    triangle,
                    triangle.Second,
                    triangle.SecondTextureCorner,
                    lightingColours[1]),
                CreateTexturedVertex(
                    triangle,
                    triangle.Third,
                    triangle.ThirdTextureCorner,
                    lightingColours[2])
            ];
        }

        private static Color[] BuildLightingColours(
            CarGeometryTriangle triangle,
            Color baseColour,
            Vector3 sunDirection)
        {
            Vector3 normal = CalculateTriangleNormal(triangle);
            float sunDotNormal = Vector3.Dot(normal, -sunDirection);
            float shadowFactor = MathHelper.Clamp(sunDotNormal, 0.0f, 1.0f);
            float shadowIntensity = 1.0f - (1.0f - shadowFactor) * ShadowIntensityFactor;

            return
            [
                ApplyLighting(baseColour, shadowIntensity),
                ApplyLighting(baseColour, shadowIntensity),
                ApplyLighting(baseColour, shadowIntensity)
            ];
        }

        private static Vector3 CalculateTriangleNormal(CarGeometryTriangle triangle)
        {
            Vector3 p0 = ToVector3(triangle, triangle.First);
            Vector3 p1 = ToVector3(triangle, triangle.Second);
            Vector3 p2 = ToVector3(triangle, triangle.Third);

            Vector3 edge1 = p1 - p0;
            Vector3 edge2 = p2 - p0;
            Vector3 normal = Vector3.Cross(edge1, edge2);

            if (normal.LengthSquared() == 0.0f)
            {
                return Vector3.Up;
            }

            normal = Vector3.Normalize(normal);

            // Ensure normal points upward for ground-facing surfaces
            if (normal.Y < 0.0f)
            {
                normal = -normal;
            }

            return normal;
        }

        private static Color ApplyLighting(Color baseColour, float intensity)
            => new(
                (byte)(baseColour.R * intensity),
                (byte)(baseColour.G * intensity),
                (byte)(baseColour.B * intensity),
                baseColour.A);

        private static VertexPositionColorTexture CreateTexturedVertex(
            CarGeometryTriangle triangle,
            CarGeometryVertex vertex,
            int textureCorner,
            Color colour)
        {
            if (textureCorner < 0 || textureCorner >= TextureCoordinates.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(textureCorner),
                    textureCorner,
                    "The car texture corner must be between zero and three.");
            }

            return new VertexPositionColorTexture(
                ToVector3(triangle, vertex),
                colour,
                TextureCoordinates[textureCorner]);
        }

        private static Vector3 ToVector3(
            CarGeometryTriangle triangle,
            CarGeometryVertex vertex)
        {
            int fixedPositionX = unchecked(
                triangle.SectionPositionX +
                (vertex.X << VertexFractionShift));
            int fixedPositionY = unchecked(
                triangle.SectionPositionY +
                (vertex.Y << VertexFractionShift));
            int fixedPositionZ = unchecked(
                triangle.SectionPositionZ +
                (vertex.Z << VertexFractionShift));

            return new Vector3(
                fixedPositionX / CoordinateScale,
                fixedPositionZ / CoordinateScale,
                -fixedPositionY / CoordinateScale) *
                CarDimensions.ModelScale;
        }
    }
}
