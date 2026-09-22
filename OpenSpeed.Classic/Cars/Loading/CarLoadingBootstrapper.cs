using System;
using System.Linq;

using NuciLog;
using NuciLog.Core;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Cars.NeedForSpeed2;
using OpenSpeed.Classic.Configuration;

namespace OpenSpeed.Classic.Cars.Loading
{
    public static class CarLoadingBootstrapper
    {
        public static LoadedCar LoadConfiguredCar(ApplicationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            ILogger logger = new NuciLogger(settings.NuciLoggerSettings);

            return LoadConfiguredCar(settings, logger);
        }

        public static LoadedCar LoadConfiguredCar(
            ApplicationSettings settings,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            GameVersion gameVersion = ParseGameVersion(settings.StartupCar.Game);
            AssetSourceSettings? assetSource = settings.Assets.Sources.SingleOrDefault(
                source => IsConfiguredFor(source, gameVersion));

            if (assetSource is null)
            {
                throw new InvalidOperationException(
                    $"No asset source is configured for '{gameVersion}'.");
            }

            IFilePathResolver filePathResolver = new FilePathResolver(logger);
            ICarFormatLoader[] formatLoaders =
            [
                new NeedForSpeed2CarLoader(filePathResolver)
            ];
            ICarFormatLoader? formatLoader = formatLoaders.SingleOrDefault(
                loader => Equals(loader.Game, gameVersion));

            if (formatLoader is null)
            {
                throw new NotSupportedException(
                    $"No car loader supports '{gameVersion}'.");
            }

            return formatLoader.Load(
                assetSource.RootDirectory,
                assetSource.OverridesDirectory,
                settings.StartupCar.Identifier);
        }

        private static bool IsConfiguredFor(
            AssetSourceSettings assetSource,
            GameVersion requestedGame)
        {
            bool gameWasParsed = Enum.TryParse(
                assetSource.Game,
                true,
                out GameVersion configuredGame);

            return gameWasParsed && Equals(configuredGame, requestedGame);
        }

        private static GameVersion ParseGameVersion(string game)
        {
            bool gameWasParsed = Enum.TryParse(game, true, out GameVersion gameVersion);

            if (!gameWasParsed || !Enum.IsDefined(gameVersion))
            {
                throw new InvalidOperationException(
                    $"The configured game '{game}' has no registered car loader.");
            }

            return gameVersion;
        }
    }
}