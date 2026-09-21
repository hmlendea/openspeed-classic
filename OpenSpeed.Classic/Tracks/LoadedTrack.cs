using System.Collections.Generic;

using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Tracks
{
    public sealed class LoadedTrack
    {
        public string Identifier { get; set; } = string.Empty;

        public IEnumerable<TrackBlock> Blocks { get; set; } = [];

        public string DisplayName { get; set; } = string.Empty;

        public GameVersion Game { get; set; }

        public TrackHorizon? Horizon { get; set; }

        public IEnumerable<TrackMaterial> Materials { get; set; } = [];

        public IEnumerable<TrackSurface> ScenerySurfaces { get; set; } = [];

        public IEnumerable<TrackAssetFile> SourceFiles { get; set; } = [];

        public IEnumerable<TrackTexture> Textures { get; set; } = [];
    }
}