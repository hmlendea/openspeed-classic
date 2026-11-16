using System.Collections.Generic;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackHorizon
    {
        public IEnumerable<TrackColour> Colours { get; set; } = [];

        public int DomeHeightOffset { get; set; }

        public int DomeHeightScale { get; set; }

        public int FlatProjectionDistance { get; set; }

        public bool HasBlackHorizon { get; set; }

        public int HorizonTextureBottomHeight { get; set; }

        public int HorizonTextureTopHeight { get; set; }

        public bool IsMirrored { get; set; }

        public int RingBaseHeight { get; set; }

        public int RingHeight { get; set; }

        public int RingMidpointHeight { get; set; }

        public int RingRadius { get; set; }

        public int RingRotationDegrees { get; set; }

        public TrackTexture? PanoramaTexture { get; set; }

        public TrackColour SkyColour { get; set; } = new();
    }
}
