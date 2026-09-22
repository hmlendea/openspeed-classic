using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class AssetSourceSettings
    {
        public string Game { get; set; } = string.Empty;

        public string RootDirectory { get; set; } = string.Empty;

        public TrackTextureVariant TextureVariant { get; set; } = TrackTextureVariant.SE;
    }
}
