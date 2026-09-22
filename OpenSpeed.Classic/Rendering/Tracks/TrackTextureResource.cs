using System;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    internal sealed class TrackTextureResource : IDisposable
    {
        internal int Identifier { get; }

        internal Texture2D Texture { get; }

        internal TrackTextureResource(GraphicsDevice graphicsDevice, TrackTexture trackTexture)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(trackTexture);

            long expectedPixelCount = (long)trackTexture.Width * trackTexture.Height;
            TrackColour[] sourcePixels = trackTexture.Pixels.ToArray();

            if (trackTexture.Width <= 0 ||
                trackTexture.Height <= 0 ||
                expectedPixelCount != sourcePixels.Length)
            {
                throw new InvalidDataException(
                    $"Track texture {trackTexture.Identifier} has dimensions " +
                    $"{trackTexture.Width}x{trackTexture.Height}, but contains " +
                    $"{sourcePixels.Length} pixels.");
            }

            Color[] pixels = sourcePixels
                .Select(ConvertColour)
                .ToArray();
            Identifier = trackTexture.Identifier;
            Texture = new Texture2D(
                graphicsDevice,
                trackTexture.Width,
                trackTexture.Height,
                false,
                SurfaceFormat.Color);
            Texture.SetData(pixels);
        }

        public void Dispose() => Texture.Dispose();

        private static Color ConvertColour(TrackColour sourceColour)
            => new(
                sourceColour.Red,
                sourceColour.Green,
                sourceColour.Blue,
                sourceColour.Alpha);
    }
}
