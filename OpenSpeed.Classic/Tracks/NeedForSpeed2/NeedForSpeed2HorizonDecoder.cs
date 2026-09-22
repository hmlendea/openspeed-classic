using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2HorizonDecoder
    {
        private static int ColourCount => 5;

        private static int ColourValueOffset => 12;

        private static int LegacyRequiredValueCount => 26;

        private static int LegacySkyColourOffset => 8;

        private static int RequiredValueCount => 27;

        internal static TrackHorizon Decode(
            string text,
            string legacyText,
            TrackTexture? panoramaTexture)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(legacyText);

            int[] values = ParseValues(text, RequiredValueCount);
            int[] legacyValues = ParseValues(legacyText, LegacyRequiredValueCount);
            TrackColour[] colours = new TrackColour[ColourCount];

            for (int colourIndex = 0; colourIndex < colours.Length; colourIndex += 1)
            {
                int valueOffset = ColourValueOffset + colourIndex * 3;
                colours[colourIndex] = new TrackColour
                {
                    Alpha = byte.MaxValue,
                    Red = unchecked((byte)values[valueOffset]),
                    Green = unchecked((byte)values[valueOffset + 1]),
                    Blue = unchecked((byte)values[valueOffset + 2])
                };
            }

            return new TrackHorizon
            {
                DomeHeightOffset = values[0],
                DomeHeightScale = values[1],
                HasBlackHorizon = values[2] != 0,
                IsMirrored = values[3] != 0,
                RingRadius = values[4],
                RingRotationDegrees = values[5],
                SkyColour = new TrackColour
                {
                    Alpha = byte.MaxValue,
                    Red = unchecked((byte)legacyValues[LegacySkyColourOffset]),
                    Green = unchecked((byte)legacyValues[LegacySkyColourOffset + 1]),
                    Blue = unchecked((byte)legacyValues[LegacySkyColourOffset + 2])
                },
                FlatProjectionDistance = values[6],
                RingBaseHeight = values[7],
                RingHeight = values[8],
                RingMidpointHeight = values[9],
                HorizonTextureTopHeight = values[10],
                HorizonTextureBottomHeight = values[11],
                Colours = colours,
                PanoramaTexture = panoramaTexture
            };
        }

        private static int[] ParseValues(string text, int requiredValueCount)
        {
            List<int> values = [];
            int position = 0;

            while (position < text.Length && values.Count < requiredValueCount)
            {
                char character = text[position];

                if (IsNumberCharacter(character))
                {
                    int tokenStart = position;

                    while (position < text.Length && IsNumberCharacter(text[position]))
                    {
                        position += 1;
                    }

                    string token = text[tokenStart..position];

                    if (!int.TryParse(
                            token,
                            NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture,
                            out int value))
                    {
                        throw new InvalidDataException(
                            $"The horizon value '{token}' is not a signed integer.");
                    }

                    values.Add(value);

                    continue;
                }

                position += 1;

                if (character == '/')
                {
                    SkipSlashDelimitedComment(text, ref position);
                }
            }

            if (values.Count != requiredValueCount)
            {
                throw new InvalidDataException(
                    $"The horizon file contains {values.Count} values; " +
                    $"{requiredValueCount} are required.");
            }

            return [.. values];
        }

        private static bool IsNumberCharacter(char character)
            => char.IsAsciiDigit(character) || character == '-' || character == '+';

        private static void SkipSlashDelimitedComment(string text, ref int position)
        {
            while (position < text.Length && text[position] != '/')
            {
                position += 1;
            }

            if (position < text.Length)
            {
                position += 1;
            }
        }
    }
}
