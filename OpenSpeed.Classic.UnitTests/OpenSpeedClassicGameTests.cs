using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic;
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
    }
}
