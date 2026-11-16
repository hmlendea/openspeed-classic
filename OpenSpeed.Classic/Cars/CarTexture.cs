using System.Collections.Generic;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Cars
{
    public sealed class CarTexture
    {
        public int Identifier { get; set; }

        public int Height { get; set; }

        public string Name { get; set; } = string.Empty;

        public IEnumerable<TrackColour> Pixels { get; set; } = [];

        public int Width { get; set; }
    }
}