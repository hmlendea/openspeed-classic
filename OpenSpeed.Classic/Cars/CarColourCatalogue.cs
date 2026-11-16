using System;
using System.Numerics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Cars
{
    public static class CarColourCatalogue
    {
        private static readonly byte[,] HsvValues =
        {
            { 0, 0, 210 },
            { 0, 0, 60 },
            { 254, 215, 249 },
            { 253, 219, 133 },
            { 249, 179, 101 },
            { 28, 253, 209 },
            { 13, 225, 249 },
            { 42, 151, 89 },
            { 89, 175, 63 },
            { 150, 255, 230 },
            { 169, 165, 177 },
            { 211, 205, 132 },
            { 127, 207, 100 },
            { 25, 196, 200 },
            { 0, 0, 120 }
        };

        public static TrackColour GetTargetColour(CarIdentifier carIdentifier)
        {
            int carIndex = (int)carIdentifier;

            if (carIndex < 0 || carIndex >= HsvValues.GetLength(0))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(carIdentifier),
                    carIdentifier,
                    "The car identifier has no colour record.");
            }

            return HsvToColour(
                HsvValues[carIndex, 0] / 255.0f,
                HsvValues[carIndex, 1] / 255.0f,
                HsvValues[carIndex, 2] / 255.0f);
        }

        private static TrackColour HsvToColour(
            float hue,
            float saturation,
            float value)
        {
            Vector3 colour = CarTextureColourRemapper.ConvertHsvToRgb(
                hue,
                saturation,
                value);

            return new TrackColour
            {
                Red = ToByte(colour.X),
                Green = ToByte(colour.Y),
                Blue = ToByte(colour.Z),
                Alpha = byte.MaxValue
            };
        }

        private static byte ToByte(float value)
            => (byte)Math.Clamp(
                (int)MathF.Round(value * byte.MaxValue),
                byte.MinValue,
                byte.MaxValue);
    }
}