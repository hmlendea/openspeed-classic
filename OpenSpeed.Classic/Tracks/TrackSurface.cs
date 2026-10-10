using System.Collections.Generic;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackSurface
    {
        public int Identifier { get; set; }

        public ushort LightingLevels { get; set; }

        public int MaterialIdentifier { get; set; }

        public IEnumerable<TrackPoint> Points { get; set; } = [];

        public TrackGeometryDetailLevel DetailLevel { get; set; }

        public TrackSurfaceGroup Group { get; set; }

        public TrackSurfaceSide Side { get; set; }
    }
}
