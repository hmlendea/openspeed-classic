using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenSpeed.Classic.Tracks
{
    public static class TrackTextureStripBuilder
    {
        public static TrackTexture Build(
            int identifier,
            string name,
            IEnumerable<TrackTexture> sourceTextures)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(sourceTextures);

            TrackTexture[] textures = sourceTextures.ToArray();

            if (textures.Length == 0)
            {
                throw new ArgumentException(
                    "At least one source texture is required.",
                    nameof(sourceTextures));
            }

            int height = textures[0].Height;

            if (height <= 0 || textures.Any(texture => texture.Height != height))
            {
                throw new ArgumentException(
                    "All source textures must have the same positive height.",
                    nameof(sourceTextures));
            }

            int width = textures.Sum(texture => texture.Width);
            TrackColour[] pixels = new TrackColour[width * height];
            int destinationX = 0;

            foreach (TrackTexture texture in textures)
            {
                TrackColour[] sourcePixels = texture.Pixels.ToArray();

                if (texture.Width <= 0 || sourcePixels.Length != texture.Width * height)
                {
                    throw new ArgumentException(
                        $"Source texture {texture.Identifier} has invalid dimensions or pixels.",
                        nameof(sourceTextures));
                }

                for (int rowIndex = 0; rowIndex < height; rowIndex += 1)
                {
                    Array.Copy(
                        sourcePixels,
                        rowIndex * texture.Width,
                        pixels,
                        rowIndex * width + destinationX,
                        texture.Width);
                }

                destinationX += texture.Width;
            }

            return new TrackTexture
            {
                Identifier = identifier,
                Name = name,
                Width = width,
                Height = height,
                Pixels = pixels
            };
        }

        public static TrackTexture BuildMirrored(
            int identifier,
            string name,
            IEnumerable<TrackTexture> sourceTextures)
        {
            TrackTexture sourceStrip = Build(identifier, name, sourceTextures);
            TrackColour[] sourcePixels = sourceStrip.Pixels.ToArray();
            int width = sourceStrip.Width * 2;
            TrackColour[] pixels = new TrackColour[width * sourceStrip.Height];

            for (int rowIndex = 0; rowIndex < sourceStrip.Height; rowIndex += 1)
            {
                int sourceOffset = rowIndex * sourceStrip.Width;
                int destinationOffset = rowIndex * width;
                Array.Copy(
                    sourcePixels,
                    sourceOffset,
                    pixels,
                    destinationOffset,
                    sourceStrip.Width);

                for (int columnIndex = 0;
                    columnIndex < sourceStrip.Width;
                    columnIndex += 1)
                {
                    pixels[destinationOffset + sourceStrip.Width + columnIndex] =
                        sourcePixels[sourceOffset + sourceStrip.Width - columnIndex - 1];
                }
            }

            return new TrackTexture
            {
                Identifier = identifier,
                Name = name,
                Width = width,
                Height = sourceStrip.Height,
                Pixels = pixels
            };
        }
    }
}
