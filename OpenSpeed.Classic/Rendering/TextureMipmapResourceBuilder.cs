using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering
{
    internal static class TextureMipmapResourceBuilder
    {
        internal static Texture2D Build(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            IEnumerable<Color> pixels)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            Texture2D texture = new(
                graphicsDevice,
                width,
                height,
                true,
                SurfaceFormat.Color);
            TextureMipmapLevel[] levels = TextureMipmapGenerator
                .Generate(pixels, width, height)
                .ToArray();

            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex += 1)
            {
                Color[] levelPixels = levels[levelIndex].Pixels.ToArray();
                texture.SetData(
                    levelIndex,
                    null,
                    levelPixels,
                    0,
                    levelPixels.Length);
            }

            return texture;
        }
    }
}