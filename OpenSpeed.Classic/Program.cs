using System;
using System.IO;

using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.Loading;

namespace OpenSpeed.Classic
{
    public static class Program
    {
        private static string SettingsFileName => "appsettings.json";

        public static void Main()
        {
            string settingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
            LoadedTrack loadedTrack = TrackLoadingBootstrapper.LoadConfiguredTrack(
                settingsFilePath);
            using OpenSpeedClassicGame game = new(loadedTrack);
            game.Run();
        }
    }
}
