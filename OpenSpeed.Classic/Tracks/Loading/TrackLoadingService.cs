using System;
using System.Collections.Generic;
using System.Linq;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Configuration;

namespace OpenSpeed.Classic.Tracks.Loading
{
    public sealed class TrackLoadingService(
        ApplicationSettings applicationSettings,
        IEnumerable<ITrackFormatLoader> trackFormatLoaders) : ITrackLoadingService
    {
        private readonly ITrackFormatLoader[] trackFormatLoaders = trackFormatLoaders.ToArray();

        public LoadedTrack Load(TrackLoadRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            AssetSourceSettings? assetSource = applicationSettings.Assets.Sources
                .SingleOrDefault(source => IsConfiguredFor(source, request.Game));

            if (assetSource is null)
            {
                throw new InvalidOperationException(
                    $"No asset source is configured for '{request.Game}'.");
            }

            ITrackFormatLoader? trackFormatLoader = trackFormatLoaders
                .SingleOrDefault(loader => Equals(loader.Game, request.Game));

            if (trackFormatLoader is null)
            {
                throw new NotSupportedException(
                    $"No track loader supports '{request.Game}'.");
            }

            return trackFormatLoader.Load(assetSource.RootDirectory, request.Identifier);
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
    }
}