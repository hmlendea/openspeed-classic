using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic;

namespace OpenSpeed.Classic.UnitTests
{
    [TestFixture]
    public sealed class OpenSpeedClassicGameTests
    {
        [Test]
        public void GivenTheGameType_WhenInspectingItsBaseType_ThenItIsAMonoGameGame()
            => Assert.That(typeof(Game).IsAssignableFrom(typeof(OpenSpeedClassicGame)));
    }
}