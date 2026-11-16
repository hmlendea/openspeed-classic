using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Configuration
{
    internal sealed class CarColourJsonConverter : JsonConverter<TrackColour>
    {
        private static int HexadecimalRgbLength => 6;

        public override TrackColour Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("The car colour must be a hexadecimal RGB string or null.");
            }

            string colour = reader.GetString()!;
            string hexadecimal = colour;

            if (hexadecimal.StartsWith('#'))
            {
                hexadecimal = hexadecimal[1..];
            }

            if (hexadecimal.Length != HexadecimalRgbLength || !hexadecimal.All(Uri.IsHexDigit))
            {
                throw new JsonException(
                    $"The car colour '{colour}' must contain six hexadecimal digits, optionally prefixed with '#'.");
            }

            byte[] channels = Convert.FromHexString(hexadecimal);

            return new TrackColour
            {
                Red = channels[0],
                Green = channels[1],
                Blue = channels[2],
                Alpha = byte.MaxValue
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            TrackColour value,
            JsonSerializerOptions options)
            => writer.WriteStringValue($"#{value.Red:X2}{value.Green:X2}{value.Blue:X2}");
    }
}