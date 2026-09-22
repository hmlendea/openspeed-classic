using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Tracks.Loading
{
    public interface ITrackFormatLoader
    {
        public GameVersion Game { get; }

        public LoadedTrack Load(string rootDirectory, string trackIdentifier);

        public LoadedTrack Load(
            string rootDirectory,
            string trackIdentifier,
            TrackTextureVariant textureVariant)
            => Load(rootDirectory, trackIdentifier);

        public LoadedTrack Load(
            string rootDirectory,
            string overridesDirectory,
            string trackIdentifier,
            TrackTextureVariant textureVariant)
            => Load(rootDirectory, trackIdentifier, textureVariant);
    }
}
