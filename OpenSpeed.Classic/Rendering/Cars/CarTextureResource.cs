using System;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Rendering;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    internal sealed class CarTextureResource : IDisposable
    {
        internal string Name { get; }

        internal Texture2D Texture { get; }

        internal CarTextureResource(GraphicsDevice graphicsDevice, CarTexture carTexture)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(carTexture);

            long expectedPixelCount = (long)carTexture.Width * carTexture.Height;
            TrackColour[] sourcePixels = carTexture.Pixels.ToArray();

            if (carTexture.Width <= 0 ||
                carTexture.Height <= 0 ||
                expectedPixelCount != sourcePixels.Length)
            {
                throw new InvalidDataException(
                    $"Car texture '{carTexture.Name}' has dimensions " +
                    $"{carTexture.Width}x{carTexture.Height}, but contains " +
                    $"{sourcePixels.Length} pixels.");
            }

            Name = carTexture.Name;
            Texture = TextureMipmapResourceBuilder.Build(
                graphicsDevice,
                carTexture.Width,
                carTexture.Height,
                sourcePixels.Select(ToColour));
        }

        public void Dispose() => Texture.Dispose();

        private static Color ToColour(TrackColour sourceColour)
            => new(
                sourceColour.Red,
                sourceColour.Green,
                sourceColour.Blue,
                sourceColour.Alpha);
    }
}