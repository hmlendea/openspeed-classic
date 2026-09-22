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

        public static IEnumerable<VertexPositionColor> BuildColoured(
            CarGeometryTriangle triangle,
            Color colour)
        {
            ArgumentNullException.ThrowIfNull(triangle);

            return
            [
                new VertexPositionColor(ToVector3(triangle, triangle.First), colour),
                new VertexPositionColor(ToVector3(triangle, triangle.Second), colour),
                new VertexPositionColor(ToVector3(triangle, triangle.Third), colour)
            ];
        }

        public static IEnumerable<VertexPositionColorTexture> BuildTextured(
            CarGeometryTriangle triangle,
            Color colour)
        {
            ArgumentNullException.ThrowIfNull(triangle);

            return
            [
                CreateTexturedVertex(
                    triangle,
                    triangle.First,
                    triangle.FirstTextureCorner,
                    colour),
                CreateTexturedVertex(
                    triangle,
                    triangle.Second,
                    triangle.SecondTextureCorner,
                    colour),
                CreateTexturedVertex(
                    triangle,
                    triangle.Third,
                    triangle.ThirdTextureCorner,
                    colour)
            ];
        }

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