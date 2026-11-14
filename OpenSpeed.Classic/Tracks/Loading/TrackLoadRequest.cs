using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Tracks.Loading
{
    public sealed class TrackLoadRequest
    {
        public string Identifier { get; set; } = string.Empty;

        public GameVersion Game { get; set; }
    }
}