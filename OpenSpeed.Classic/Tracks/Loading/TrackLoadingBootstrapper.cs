using System;

using NuciLog;
using NuciLog.Core;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Configuration;
using OpenSpeed.Classic.Tracks.NeedForSpeed2;

namespace OpenSpeed.Classic.Tracks.Loading
{
    public static class TrackLoadingBootstrapper
    {
        public static LoadedTrack LoadConfiguredTrack(string settingsFilePath)
        {
            IApplicationSettingsLoader settingsLoader = new ApplicationSettingsLoader();
            ApplicationSettings settings = settingsLoader.Load(settingsFilePath);
            ILogger logger = new NuciLogger(settings.NuciLoggerSettings);

            return LoadConfiguredTrack(settings, logger);
        }

        public static LoadedTrack LoadConfiguredTrack(ApplicationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            ILogger logger = new NuciLogger(settings.NuciLoggerSettings);

            return LoadConfiguredTrack(settings, logger);
        }

        public static LoadedTrack LoadConfiguredTrack(
            ApplicationSettings settings,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            GameVersion gameVersion = ParseGameVersion(settings.StartupTrack.Game);
            IFilePathResolver filePathResolver = new FilePathResolver(logger);
            ITrackFormatLoader[] formatLoaders =
            [
                new NeedForSpeed2TrackLoader(filePathResolver)
            ];
            ITrackLoadingService trackLoadingService = new TrackLoadingService(
                settings,
                formatLoaders);
            TrackLoadRequest request = new()
            {
                Identifier = settings.StartupTrack.Identifier,
                Game = gameVersion
            };

            return trackLoadingService.Load(request);
        }

        private static GameVersion ParseGameVersion(string game)
        {
            bool gameWasParsed = Enum.TryParse(game, true, out GameVersion gameVersion);

            if (!gameWasParsed || !Enum.IsDefined(gameVersion))
            {
                throw new InvalidOperationException(
                    $"The configured game '{game}' has no registered track loader.");
            }

            return gameVersion;
        }
    }
}
