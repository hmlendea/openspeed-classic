using System.Collections.Generic;

using Microsoft.Xna.Framework;

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
    }
}