using NuciLog.Configuration;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class ApplicationSettings
    {
        public AssetsSettings Assets { get; set; } = new();

        public DrivingControlsSettings Controls { get; set; } = new();

        public NuciLoggerSettings NuciLoggerSettings { get; set; } = new();

        public RenderingSettings Rendering { get; set; } = new();

        public StartupCarSettings StartupCar { get; set; } = new();

        public StartupTrackSettings StartupTrack { get; set; } = new();
    }
}
