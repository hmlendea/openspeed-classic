using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Configuration
{
    internal sealed class TrackTextureVariantJsonConverter : JsonConverter<TrackTextureVariant>
    {
        public override bool HandleNull => true;

        public override TrackTextureVariant Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (!Equals(reader.TokenType, JsonTokenType.String))
            {
                throw new JsonException("The track texture variant must be a string.");
            }

            string? variantName = reader.GetString();

            if (string.IsNullOrWhiteSpace(variantName))
            {
                throw new JsonException("The track texture variant is empty.");
            }

            TrackTextureVariant variant = TrackTextureVariant.FromString(variantName);

            if (!string.Equals(variant.Name, variantName, StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException(
                    $"The track texture variant '{variantName}' is unsupported.");
            }

            return variant;
        }

        public override void Write(
            Utf8JsonWriter writer,
            TrackTextureVariant value,
            JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);
            writer.WriteStringValue(value.Name);
        }
    }
}
