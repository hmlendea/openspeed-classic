using System.Collections.Generic;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackBlock
    {
        public int Identifier { get; set; }

        public TrackPoint Centre { get; set; } = new();

        public IEnumerable<TrackPoint> ClippingPoints { get; set; } = [];

        public int ConnectedVertexCount { get; set; }

        public IEnumerable<TrackSurface> Surfaces { get; set; } = [];
    }
}