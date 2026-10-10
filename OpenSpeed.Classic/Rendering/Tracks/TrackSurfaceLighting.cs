using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackSurfaceLighting
    {
        private static int FirstLevelShift => 4;

        private static int IntensityStep => 24;

        private static int LevelBitCount => 3;

        private static int LevelMask => 7;

        private static int MinimumIntensity => 50;

        private static int VertexCount => 4;

        private static float ShadowIntensityFactor => 0.3f;

        public static IEnumerable<Color> Build(ushort lightingLevels)
        {
            Color[] colours = new Color[VertexCount];

            for (int vertexIndex = 0; vertexIndex < colours.Length; vertexIndex += 1)
            {
                int shift = FirstLevelShift + vertexIndex * LevelBitCount;
                int lightLevel = lightingLevels >> shift & LevelMask;
                byte intensity = (byte)(MinimumIntensity + lightLevel * IntensityStep);
                colours[vertexIndex] = new Color(intensity, intensity, intensity);
            }

            return colours;
        }

        public static IEnumerable<Color> Build(
            ushort lightingLevels,
            Vector3 sunDirection,
            TrackPoint[] points)
        {
            Color[] colours = new Color[VertexCount];

            Vector3 surfaceNormal = CalculateSurfaceNormal(points);
            float sunDotNormal = Vector3.Dot(surfaceNormal, -sunDirection);
            float shadowFactor = MathHelper.Clamp(sunDotNormal, 0.0f, 1.0f);
            float shadowIntensity = 1.0f - (1.0f - shadowFactor) * ShadowIntensityFactor;

            for (int vertexIndex = 0; vertexIndex < colours.Length; vertexIndex += 1)
            {
                int shift = FirstLevelShift + vertexIndex * LevelBitCount;
                int lightLevel = lightingLevels >> shift & LevelMask;
                byte baseIntensity = (byte)(MinimumIntensity + lightLevel * IntensityStep);
                byte intensity = (byte)(baseIntensity * shadowIntensity);
                colours[vertexIndex] = new Color(intensity, intensity, intensity);
            }

            return colours;
        }

        private static Vector3 CalculateSurfaceNormal(TrackPoint[] points)
        {
            Vector3 p0 = ToVector3(points[0]);
            Vector3 p1 = ToVector3(points[1]);
            Vector3 p2 = ToVector3(points[2]);

            Vector3 edge1 = p1 - p0;
            Vector3 edge2 = p2 - p0;
            Vector3 normal = Vector3.Cross(edge1, edge2);

            if (normal.LengthSquared() == 0.0f)
            {
                return Vector3.Up;
            }

            normal = Vector3.Normalize(normal);

            // Ensure normal points upward for ground surfaces
            if (normal.Y < 0.0f)
            {
                normal = -normal;
            }

            return normal;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);
    }
}