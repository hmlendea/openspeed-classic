using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;
using OpenSpeed.Classic.Configuration;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests
{
    [TestFixture]
    public sealed class OpenSpeedClassicGameTests
    {
        [Test]
        public void GivenTheGameType_WhenInspectingItsBaseType_ThenItIsAMonoGameGame()
            => Assert.That(typeof(Game).IsAssignableFrom(typeof(OpenSpeedClassicGame)));

        [Test]
        public void GivenALoadedTrack_WhenConstructingTheGame_ThenTheTrackIsRetained()
        {
            LoadedTrack loadedTrack = new()
            {
                Identifier = "Outback"
            };

            using OpenSpeedClassicGame game = new(loadedTrack);

            Assert.That(game.CurrentTrack, Is.SameAs(loadedTrack));
        }

        [Test]
        public void GivenANullTrack_WhenConstructingTheGame_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => new OpenSpeedClassicGame(null!),
                Throws.TypeOf<ArgumentNullException>());

        [Test]
        public void GivenNullRenderingSettings_WhenConstructingTheGame_ThenAnArgumentNullExceptionIsThrown()
        {
            LoadedTrack loadedTrack = new() { Identifier = "Outback" };

            Assert.That(
                () => new OpenSpeedClassicGame(loadedTrack, null!, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void GivenNullControls_WhenConstructingTheGame_ThenAnArgumentNullExceptionIsThrown()
        {
            LoadedTrack loadedTrack = new() { Identifier = "Outback" };

            Assert.That(
                () => new OpenSpeedClassicGame(
                    loadedTrack,
                    null,
                    new RenderingSettings(),
                    null!,
                    null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase(0, 720)]
        [TestCase(1280, 0)]
        [TestCase(-1, 720)]
        [TestCase(1280, -1)]
        public void GivenAnInvalidScreenDimension_WhenConstructing_ThenTheSettingsAreRejected(
            int screenWidth,
            int screenHeight)
        {
            LoadedTrack loadedTrack = new() { Identifier = "Outback" };
            RenderingSettings renderingSettings = new()
            {
                ScreenHeight = screenHeight,
                ScreenWidth = screenWidth
            };

            Assert.That(
                () => new OpenSpeedClassicGame(
                    loadedTrack,
                    renderingSettings,
                    null),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
