using System.Collections.Generic;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackTexture
    {
        public int Identifier { get; set; }

        public int Height { get; set; }

        public string Name { get; set; } = string.Empty;

        public IEnumerable<TrackColour> Pixels { get; set; } = [];

        public int Width { get; set; }
    }
}