using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Cars
{
    public static class CarTextureColourRemapper
    {
        private static float GreenHueMaximum => 0.50f;

        private static float GreenHueMinimum => 0.20f;

        private static float MinimumPaintSaturation => 0.35f;

        private static float MinimumPaintValue => 0.12f;

        public static void Apply(
            IEnumerable<CarTexture> textures,
            CarIdentifier carIdentifier)
        {
            ArgumentNullException.ThrowIfNull(textures);

            TrackColour targetColour = CarColourCatalogue.GetTargetColour(carIdentifier);
            Vector3 targetHsv = ConvertRgbToHsv(targetColour);

            foreach (CarTexture texture in textures)
            {
                texture.Pixels = texture.Pixels
                    .Select(sourceColour => RemapColour(
                        sourceColour,
                        targetHsv.X,
                        targetHsv.Z))
                    .ToArray();
            }
        }

        internal static Vector3 ConvertHsvToRgb(
            float hue,
            float saturation,
            float value)
        {
            float scaledHue = hue * 6.0f;
            int sector = (int)MathF.Floor(scaledHue);
            float fraction = scaledHue - sector;
            float first = value * (1.0f - saturation);
            float second = value * (1.0f - saturation * fraction);
            float third = value * (1.0f - saturation * (1.0f - fraction));
            float red = value;
            float green = third;
            float blue = first;

            switch (sector % 6)
            {
                case 1:
                    red = second;
                    green = value;
                    break;
                case 2:
                    red = first;
                    green = value;
                    blue = third;
                    break;
                case 3:
                    red = first;
                    green = second;
                    blue = value;
                    break;
                case 4:
                    red = third;
                    green = first;
                    blue = value;
                    break;
                case 5:
                    red = value;
                    green = first;
                    blue = second;
                    break;
                default:
                    break;
            }

            return new Vector3(red, green, blue);
        }

        private static Vector3 ConvertRgbToHsv(TrackColour colour)
        {
            float red = colour.Red / (float)byte.MaxValue;
            float green = colour.Green / (float)byte.MaxValue;
            float blue = colour.Blue / (float)byte.MaxValue;
            float maximum = MathF.Max(red, MathF.Max(green, blue));
            float minimum = MathF.Min(red, MathF.Min(green, blue));
            float difference = maximum - minimum;
            float saturation = 0.0f;
            float hue = 0.0f;

            if (maximum != 0.0f)
            {
                saturation = difference / maximum;
            }

            if (difference == 0.0f)
            {
                return new Vector3(hue, saturation, maximum);
            }

            if (maximum == red)
            {
                hue = (green - blue) / difference;
            }
            else if (maximum == green)
            {
                hue = 2.0f + (blue - red) / difference;
            }
            else
            {
                hue = 4.0f + (red - green) / difference;
            }

            hue /= 6.0f;

            if (hue < 0.0f)
            {
                hue += 1.0f;
            }

            return new Vector3(hue, saturation, maximum);
        }

        private static TrackColour RemapColour(
            TrackColour sourceColour,
            float targetHue,
            float targetValue)
        {
            Vector3 sourceHsv = ConvertRgbToHsv(sourceColour);

            if (sourceHsv.X < GreenHueMinimum ||
                sourceHsv.X > GreenHueMaximum ||
                sourceHsv.Y < MinimumPaintSaturation ||
                sourceHsv.Z < MinimumPaintValue)
            {
                return sourceColour;
            }

            Vector3 remappedColour = ConvertHsvToRgb(
                targetHue,
                sourceHsv.Y,
                sourceHsv.Z * targetValue);

            return new TrackColour
            {
                Red = ToByte(remappedColour.X),
                Green = ToByte(remappedColour.Y),
                Blue = ToByte(remappedColour.Z),
                Alpha = sourceColour.Alpha
            };
        }

        private static byte ToByte(float value)
            => (byte)Math.Clamp(
                (int)MathF.Round(value * byte.MaxValue),
                byte.MinValue,
                byte.MaxValue);
    }
}