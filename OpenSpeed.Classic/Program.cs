using System;
using System.IO;

using NuciLog;
using NuciLog.Core;

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
            ILogger logger = new NuciLogger(settings.NuciLoggerSettings);
            LoadedTrack loadedTrack = TrackLoadingBootstrapper.LoadConfiguredTrack(
                settings,
                logger);
            LoadedCar loadedCar = CarLoadingBootstrapper.LoadConfiguredCar(
                settings,
                logger);
            using OpenSpeedClassicGame game = new(
                loadedTrack,
                loadedCar,
                settings.Rendering,
                settings.Controls,
                applicationArguments.CaptureFramePath);
            game.Run();
        }
    }
}
