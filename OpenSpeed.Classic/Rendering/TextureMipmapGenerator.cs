using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public static class TextureMipmapGenerator
    {
        public static IEnumerable<TextureMipmapLevel> Generate(
            IEnumerable<Color> sourcePixels,
            int width,
            int height)
        {
            ArgumentNullException.ThrowIfNull(sourcePixels);

            Color[] pixels = sourcePixels.ToArray();
            long expectedPixelCount = (long)width * height;

            if (width <= 0 || height <= 0 || expectedPixelCount != pixels.Length)
            {
                throw new InvalidDataException(
                    $"Texture dimensions {width}x{height} require {expectedPixelCount} " +
                    $"pixels, but {pixels.Length} were provided.");
            }

            List<TextureMipmapLevel> levels = [];
            int levelWidth = width;
            int levelHeight = height;
            Color[] levelPixels = pixels;

            while (true)
            {
                levels.Add(new TextureMipmapLevel
                {
                    Height = levelHeight,
                    Pixels = levelPixels,
                    Width = levelWidth
                });

                if (levelWidth == 1 && levelHeight == 1)
                {
                    return levels;
                }

                int nextWidth = Math.Max(1, levelWidth / 2);
                int nextHeight = Math.Max(1, levelHeight / 2);
                levelPixels = GenerateNextLevel(
                    levelPixels,
                    levelWidth,
                    levelHeight,
                    nextWidth,
                    nextHeight);
                levelWidth = nextWidth;
                levelHeight = nextHeight;
            }
        }

        private static Color[] GenerateNextLevel(
            Color[] sourcePixels,
            int sourceWidth,
            int sourceHeight,
            int targetWidth,
            int targetHeight)
        {
            Color[] targetPixels = new Color[targetWidth * targetHeight];

            for (int targetY = 0; targetY < targetHeight; targetY += 1)
            {
                for (int targetX = 0; targetX < targetWidth; targetX += 1)
                {
                    targetPixels[targetY * targetWidth + targetX] = AverageBlock(
                        sourcePixels,
                        sourceWidth,
                        sourceHeight,
                        targetX * 2,
                        targetY * 2);
                }
            }

            return targetPixels;
        }

        private static Color AverageBlock(
            Color[] sourcePixels,
            int sourceWidth,
            int sourceHeight,
            int sourceX,
            int sourceY)
        {
            int red = 0;
            int green = 0;
            int blue = 0;
            int alpha = 0;
            int sampleCount = 0;

            for (int offsetY = 0; offsetY < 2; offsetY += 1)
            {
                int sampleY = sourceY + offsetY;

                if (sampleY >= sourceHeight)
                {
                    continue;
                }

                for (int offsetX = 0; offsetX < 2; offsetX += 1)
                {
                    int sampleX = sourceX + offsetX;

                    if (sampleX >= sourceWidth)
                    {
                        continue;
                    }

                    Color sample = sourcePixels[sampleY * sourceWidth + sampleX];
                    red += sample.R;
                    green += sample.G;
                    blue += sample.B;
                    alpha += sample.A;
                    sampleCount += 1;
                }
            }

            return new Color(
                red / sampleCount,
                green / sampleCount,
                blue / sampleCount,
                alpha / sampleCount);
        }
    }
}