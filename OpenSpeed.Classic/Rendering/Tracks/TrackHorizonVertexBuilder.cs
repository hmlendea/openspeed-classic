using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackHorizonVertexBuilder
    {
        private static int DomeLatitudePointCount => 9;

        private static int DomeLongitudePointCount => RingPointCount + 1;

        private static int RingPointCount => 32;

        public static IEnumerable<VertexPositionColorTexture> BuildDome(
            TrackHorizon horizon)
        {
            ArgumentNullException.ThrowIfNull(horizon);

            Vector3[] points = BuildCapPoints(horizon);
            List<VertexPositionColorTexture> vertices = [];

            for (int latitudeIndex = 0;
                latitudeIndex < DomeLatitudePointCount - 1;
                latitudeIndex += 1)
            {
                for (int longitudeIndex = 0;
                    longitudeIndex < DomeLongitudePointCount - 1;
                    longitudeIndex += 1)
                {
                    int topLeft =
                        latitudeIndex * DomeLongitudePointCount + longitudeIndex;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + DomeLongitudePointCount;
                    int bottomRight = bottomLeft + 1;
                    AddCapVertex(vertices, points, topLeft);
                    AddCapVertex(vertices, points, bottomLeft);
                    AddCapVertex(vertices, points, bottomRight);
                    AddCapVertex(vertices, points, topLeft);
                    AddCapVertex(vertices, points, bottomRight);
                    AddCapVertex(vertices, points, topRight);
                }
            }

            return vertices;
        }

        public static IEnumerable<VertexPositionColorTexture> BuildPanorama(
            TrackHorizon horizon)
        {
            ArgumentNullException.ThrowIfNull(horizon);

            Vector3[] bottomPoints = BuildRingPoints(
                horizon,
                horizon.RingBaseHeight + horizon.HorizonTextureBottomHeight);
            Vector3[] topPoints = BuildRingPoints(
                horizon,
                horizon.RingBaseHeight + horizon.HorizonTextureTopHeight);
            List<VertexPositionColorTexture> vertices = [];

            for (int pointIndex = 0; pointIndex < RingPointCount; pointIndex += 1)
            {
                int nextPointIndex = (pointIndex + 1) % RingPointCount;
                float textureCoordinate = (float)pointIndex / RingPointCount;
                float nextTextureCoordinate = (float)(pointIndex + 1) / RingPointCount;

                if (horizon.IsMirrored)
                {
                    textureCoordinate = 1.0f - textureCoordinate;
                    nextTextureCoordinate = 1.0f - nextTextureCoordinate;
                }

                vertices.Add(new VertexPositionColorTexture(
                    bottomPoints[pointIndex],
                    Color.White,
                    new Vector2(textureCoordinate, 1.0f)));
                vertices.Add(new VertexPositionColorTexture(
                    topPoints[pointIndex],
                    Color.White,
                    new Vector2(textureCoordinate, 0.0f)));
                vertices.Add(new VertexPositionColorTexture(
                    topPoints[nextPointIndex],
                    Color.White,
                    new Vector2(nextTextureCoordinate, 0.0f)));
                vertices.Add(new VertexPositionColorTexture(
                    bottomPoints[pointIndex],
                    Color.White,
                    new Vector2(textureCoordinate, 1.0f)));
                vertices.Add(new VertexPositionColorTexture(
                    topPoints[nextPointIndex],
                    Color.White,
                    new Vector2(nextTextureCoordinate, 0.0f)));
                vertices.Add(new VertexPositionColorTexture(
                    bottomPoints[nextPointIndex],
                    Color.White,
                    new Vector2(nextTextureCoordinate, 1.0f)));
            }

            return vertices;
        }

        public static IEnumerable<VertexPositionColor> BuildRing(TrackHorizon horizon)
        {
            ArgumentNullException.ThrowIfNull(horizon);

            Color skyColour = ToColor(horizon.SkyColour);
            Vector3[] bottomPoints = BuildRingPoints(horizon, horizon.RingBaseHeight);
            Vector3[] topPoints = BuildRingPoints(
                horizon,
                unchecked(horizon.RingBaseHeight + horizon.RingHeight));
            List<VertexPositionColor> vertices = [];

            for (int pointIndex = 0; pointIndex < RingPointCount; pointIndex += 1)
            {
                int nextPointIndex = (pointIndex + 1) % RingPointCount;
                if (horizon.HasBlackHorizon)
                {
                    skyColour = Color.Black;
                }

                vertices.Add(new VertexPositionColor(
                    bottomPoints[pointIndex],
                    skyColour));
                vertices.Add(new VertexPositionColor(topPoints[pointIndex], skyColour));
                vertices.Add(new VertexPositionColor(topPoints[nextPointIndex], skyColour));
                vertices.Add(new VertexPositionColor(
                    bottomPoints[pointIndex],
                    skyColour));
                vertices.Add(new VertexPositionColor(topPoints[nextPointIndex], skyColour));
                vertices.Add(new VertexPositionColor(
                    bottomPoints[nextPointIndex],
                    skyColour));
            }

            return vertices;
        }

        private static void AddCapVertex(
            List<VertexPositionColorTexture> vertices,
            Vector3[] points,
            int pointIndex)
        {
            vertices.Add(new VertexPositionColorTexture(
                points[pointIndex],
                Color.White,
                Vector2.Zero));
        }

        private static Vector3[] BuildCapPoints(TrackHorizon horizon)
        {
            Vector3[] points =
                new Vector3[DomeLatitudePointCount * DomeLongitudePointCount];
            float ringTopHeight = horizon.RingBaseHeight + horizon.RingHeight;
            double domeRadius = Math.Sqrt(
                horizon.RingRadius * horizon.RingRadius +
                ringTopHeight * ringTopHeight);
            double minimumElevation = Math.Atan2(
                ringTopHeight,
                horizon.RingRadius);
            double elevationStep =
                (Math.PI / 2.0 - minimumElevation) /
                (DomeLatitudePointCount - 1);
            double rotationRadians = MathHelper.ToRadians(horizon.RingRotationDegrees);
            double longitudeStep =
                Math.PI * 2.0 /
                (DomeLongitudePointCount - 1);

            for (int latitudeIndex = 0;
                latitudeIndex < DomeLatitudePointCount;
                latitudeIndex += 1)
            {
                double elevation = minimumElevation + latitudeIndex * elevationStep;
                double horizontalRadius = Math.Cos(elevation) * domeRadius;
                float positionY = (float)(Math.Sin(elevation) * domeRadius);

                for (int longitudeIndex = 0;
                    longitudeIndex < DomeLongitudePointCount;
                    longitudeIndex += 1)
                {
                    double longitude =
                        rotationRadians + longitudeIndex * longitudeStep;
                    points[latitudeIndex * DomeLongitudePointCount + longitudeIndex] =
                        new Vector3(
                        (float)(Math.Sin(longitude) * horizontalRadius),
                        positionY,
                        (float)(Math.Cos(longitude) * horizontalRadius));
                }
            }

            return points;
        }

        private static Vector3[] BuildRingPoints(TrackHorizon horizon, int height)
        {
            Vector3[] points = new Vector3[RingPointCount];
            double rotationRadians = MathHelper.ToRadians(horizon.RingRotationDegrees);
            double angleStep = Math.PI * 2.0 / RingPointCount;

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

        private static Color ToColor(TrackColour sourceColour)
            => new(
                sourceColour.Red,
                sourceColour.Green,
                sourceColour.Blue,
                sourceColour.Alpha);
    }
}
