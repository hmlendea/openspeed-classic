using System.Text.Json.Serialization;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class StartupCarSettings
    {
        public string Game { get; set; } = "NeedForSpeed2SpecialEdition";

        public string Identifier { get; set; } = "McLarenF1";

        [JsonConverter(typeof(CarColourJsonConverter))]
        public TrackColour? Colour { get; set; }
    }
}