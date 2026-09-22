using System;

using Moq;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Configuration;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.Loading;

namespace OpenSpeed.Classic.UnitTests.Tracks.Loading
{
    [TestFixture]
    public sealed class TrackLoadingServiceTests
    {
        private static string AssetRootDirectory => "/test-assets";

        private Mock<ITrackFormatLoader> formatLoader = null!;
        private ITrackLoadingService trackLoadingService = null!;

        [SetUp]
        public void SetUp()
        {
            ApplicationSettings settings = new()
            {
                Assets = new AssetsSettings
                {
                    Sources =
                    [
                        new AssetSourceSettings
                        {
                            Game = nameof(GameVersion.NeedForSpeed2SpecialEdition),
                            RootDirectory = AssetRootDirectory,
                            TextureVariant = TrackTextureVariant.PC
                        }
                    ]
                }
            };
            formatLoader = new Mock<ITrackFormatLoader>();
            formatLoader
                .SetupGet(loader => loader.Game)
                .Returns(GameVersion.NeedForSpeed2SpecialEdition);
            trackLoadingService = new TrackLoadingService(settings, [formatLoader.Object]);
        }

        [Test]
        public void GivenAConfiguredLoader_WhenLoadingATrack_ThenTheRequestIsRoutedToIt()
        {
            LoadedTrack expectedTrack = new()
            {
                Identifier = "Outback"
            };
            formatLoader
                .Setup(loader => loader.Load(
                    AssetRootDirectory,
                    "Outback",
                    TrackTextureVariant.PC))
                .Returns(expectedTrack);
            TrackLoadRequest request = new()
            {
                Identifier = "Outback",
                Game = GameVersion.NeedForSpeed2SpecialEdition
            };

            LoadedTrack track = trackLoadingService.Load(request);

            Assert.That(track, Is.SameAs(expectedTrack));
            formatLoader.Verify(
                loader => loader.Load(
                    AssetRootDirectory,
                    "Outback",
                    TrackTextureVariant.PC),
                Times.Once);
        }

        [Test]
        public void GivenNoConfiguredSource_WhenLoadingATrack_ThenAnInvalidOperationExceptionIsThrown()
        {
            ApplicationSettings settings = new();
            ITrackLoadingService loadingService = new TrackLoadingService(
                settings,
                [formatLoader.Object]);
            TrackLoadRequest request = new()
            {
                Identifier = "Outback",
                Game = GameVersion.NeedForSpeed2SpecialEdition
            };

            Assert.That(
                () => loadingService.Load(request),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void GivenNoCompatibleLoader_WhenLoadingATrack_ThenANotSupportedExceptionIsThrown()
        {
            ApplicationSettings settings = new()
            {
                Assets = new AssetsSettings
                {
                    Sources =
                    [
                        new AssetSourceSettings
                        {
                            Game = nameof(GameVersion.NeedForSpeed2SpecialEdition),
                            RootDirectory = AssetRootDirectory
                        }
                    ]
                }
            };
            ITrackLoadingService loadingService = new TrackLoadingService(settings, []);
            TrackLoadRequest request = new()
            {
                Identifier = "Outback",
                Game = GameVersion.NeedForSpeed2SpecialEdition
            };

            Assert.That(
                () => loadingService.Load(request),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void GivenANullRequest_WhenLoadingATrack_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => trackLoadingService.Load(null!),
                Throws.TypeOf<ArgumentNullException>());
    }
}
