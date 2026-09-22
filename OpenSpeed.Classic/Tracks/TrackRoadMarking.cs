using System.Collections.Generic;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackRoadMarking
    {
        public int Identifier { get; set; }

        public IEnumerable<TrackPoint> Points { get; set; } = [];
    }
}