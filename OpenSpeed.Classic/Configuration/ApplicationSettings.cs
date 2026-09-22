namespace OpenSpeed.Classic.Configuration
{
    public sealed class ApplicationSettings
    {
        public AssetsSettings Assets { get; set; } = new();

        public RenderingSettings Rendering { get; set; } = new();

        public StartupTrackSettings StartupTrack { get; set; } = new();
    }
}
