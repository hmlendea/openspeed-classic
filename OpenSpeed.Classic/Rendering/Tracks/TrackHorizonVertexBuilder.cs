using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackHorizonVertexBuilder
    {
        private static float DomeCoordinateStep => 150.0f;

        private static float DomeMinimumCoordinate => -1000.0f;

        private static double DomeRadialWaveRadiansPerUnit => 0.0015707961785;

        private static int DomeRowPointCount => 13;

        private static int HorizonColourCount => 5;

        private static int RingColourCount => 16;

        private static int RingPointCount => 32;

        public static IEnumerable<VertexPositionColorTexture> BuildDome(
            TrackHorizon horizon)
        {
            ArgumentNullException.ThrowIfNull(horizon);

            Vector3[] points = BuildDomePoints(horizon);
            List<VertexPositionColorTexture> vertices = [];

            for (int rowIndex = 0;
                rowIndex < DomeRowPointCount - 1;
                rowIndex += 1)
            {
                for (int columnIndex = 0;
                    columnIndex < DomeRowPointCount - 1;
                    columnIndex += 1)
                {
                    int topLeft = rowIndex * DomeRowPointCount + columnIndex;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + DomeRowPointCount;
                    int bottomRight = bottomLeft + 1;
                    AddDomeVertex(vertices, points, topLeft, horizon.IsMirrored);
                    AddDomeVertex(vertices, points, bottomLeft, horizon.IsMirrored);
                    AddDomeVertex(vertices, points, bottomRight, horizon.IsMirrored);
                    AddDomeVertex(vertices, points, topLeft, horizon.IsMirrored);
                    AddDomeVertex(vertices, points, bottomRight, horizon.IsMirrored);
                    AddDomeVertex(vertices, points, topRight, horizon.IsMirrored);
                }
            }

            return vertices;
        }

        public static IEnumerable<VertexPositionColor> BuildRing(TrackHorizon horizon)
        {
            ArgumentNullException.ThrowIfNull(horizon);

            Color[] colourRamp = BuildColourRamp(horizon);
            Vector3[] bottomPoints = BuildRingPoints(horizon, horizon.RingBaseHeight);
            Vector3[] topPoints = BuildRingPoints(
                horizon,
                unchecked(horizon.RingBaseHeight + horizon.RingHeight));
            List<VertexPositionColor> vertices = [];

            for (int pointIndex = 0; pointIndex < RingPointCount; pointIndex += 1)
            {
                int colourIndex = pointIndex % RingColourCount;
                int nextPointIndex = (pointIndex + 1) % RingPointCount;
                int nextColourIndex = nextPointIndex % RingColourCount;
                Color colour = colourRamp[colourIndex];
                Color nextColour = colourRamp[nextColourIndex];

                if (horizon.HasBlackHorizon)
                {
                    colour = Color.Black;
                    nextColour = Color.Black;
                }

                vertices.Add(new VertexPositionColor(bottomPoints[pointIndex], colour));
                vertices.Add(new VertexPositionColor(topPoints[pointIndex], colour));
                vertices.Add(new VertexPositionColor(topPoints[nextPointIndex], nextColour));
                vertices.Add(new VertexPositionColor(bottomPoints[pointIndex], colour));
                vertices.Add(new VertexPositionColor(topPoints[nextPointIndex], nextColour));
                vertices.Add(new VertexPositionColor(bottomPoints[nextPointIndex], nextColour));
            }

            return vertices;
        }

        private static void AddDomeVertex(
            List<VertexPositionColorTexture> vertices,
            Vector3[] points,
            int pointIndex,
            bool isMirrored)
        {
            int rowIndex = pointIndex / DomeRowPointCount;
            int columnIndex = pointIndex % DomeRowPointCount;
            float textureCoordinateX =
                (float)columnIndex /
                (DomeRowPointCount - 1);

            if (isMirrored)
            {
                textureCoordinateX = 1.0f - textureCoordinateX;
            }

            Vector2 textureCoordinate = new(
                textureCoordinateX,
                (float)rowIndex / (DomeRowPointCount - 1));
            vertices.Add(new VertexPositionColorTexture(
                points[pointIndex],
                Color.White,
                textureCoordinate));
        }

        private static Vector3[] BuildDomePoints(TrackHorizon horizon)
        {
            Vector3[] points = new Vector3[DomeRowPointCount * DomeRowPointCount];

            for (int rowIndex = 0; rowIndex < DomeRowPointCount; rowIndex += 1)
            {
                float positionZ = DomeMinimumCoordinate + rowIndex * DomeCoordinateStep;

                for (int columnIndex = 0;
                    columnIndex < DomeRowPointCount;
                    columnIndex += 1)
                {
                    float positionX =
                        DomeMinimumCoordinate + columnIndex * DomeCoordinateStep;
                    double radius = Math.Sqrt(
                        positionX * positionX + positionZ * positionZ);
                    float positionY = (float)(horizon.DomeHeightOffset +
                        horizon.DomeHeightScale *
                        Math.Cos(radius * DomeRadialWaveRadiansPerUnit));
                    points[rowIndex * DomeRowPointCount + columnIndex] = new Vector3(
                        positionX,
                        positionY,
                        positionZ);
                }
            }

            return points;
        }

        private static Color[] BuildColourRamp(TrackHorizon horizon)
        {
            TrackColour[] sourceColours = horizon.Colours.ToArray();

            if (sourceColours.Length != HorizonColourCount)
            {
                throw new InvalidDataException(
                    $"The horizon contains {sourceColours.Length} colours; " +
                    $"exactly {HorizonColourCount} are required.");
            }

            TrackColour start = sourceColours[3];
            TrackColour end = sourceColours[4];
            int red = start.Red << 16;
            int green = start.Green << 16;
            int blue = start.Blue << 16;
            int redStep = unchecked((end.Red - start.Red) << 13);
            int greenStep = unchecked((end.Green - start.Green) << 13);
            int blueStep = unchecked((end.Blue - start.Blue) << 13);
            Color[] colours = new Color[RingColourCount];

            for (int colourIndex = 0;
                colourIndex < RingColourCount / 2;
                colourIndex += 1)
            {
                Color colour = new(
                    ToByte(red),
                    ToByte(green),
                    ToByte(blue));
                colours[colourIndex] = colour;
                colours[RingColourCount - colourIndex - 1] = colour;
                red = unchecked(red + redStep);
                green = unchecked(green + greenStep);
                blue = unchecked(blue + blueStep);
            }

            return colours;
        }

        private static Vector3[] BuildRingPoints(TrackHorizon horizon, int height)
        {
            Vector3[] points = new Vector3[RingPointCount];
            double rotationRadians = MathHelper.ToRadians(horizon.RingRotationDegrees);
            double angleStep = Math.PI * 2.0 / RingColourCount;

            for (int pointIndex = 0; pointIndex < points.Length; pointIndex += 1)
            {
                double angle = rotationRadians + pointIndex * angleStep;
                points[pointIndex] = new Vector3(
                    (float)(Math.Sin(angle) * horizon.RingRadius),
                    height,
                    (float)(Math.Cos(angle) * horizon.RingRadius));
            }

            return points;
        }

        private static byte ToByte(int fixedValue)
            => unchecked((byte)(fixedValue / 65536));
    }
}