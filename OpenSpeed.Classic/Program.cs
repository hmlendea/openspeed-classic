using System;
using System.IO;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Cars.Loading;
using OpenSpeed.Classic.Configuration;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.Loading;

namespace OpenSpeed.Classic
{
    public static class Program
    {
        private static string SettingsFileName => "appsettings.json";

        public static void Main(string[] arguments)
        {
            ApplicationArguments applicationArguments =
                ApplicationArgumentsParser.Parse(arguments);
            string settingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
            IApplicationSettingsLoader settingsLoader = new ApplicationSettingsLoader();
            ApplicationSettings settings = settingsLoader.Load(settingsFilePath);
            LoadedTrack loadedTrack = TrackLoadingBootstrapper.LoadConfiguredTrack(settings);
            LoadedCar loadedCar = CarLoadingBootstrapper.LoadConfiguredCar(settings);
            using OpenSpeedClassicGame game = new(
                loadedTrack,
                loadedCar,
                settings.Rendering,
                applicationArguments.CaptureFramePath);
            game.Run();
        }
    }
}
