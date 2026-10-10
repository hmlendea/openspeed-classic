using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class MinimapProjectionTests
    {
        [TestCase(0.0f)]
        [TestCase(MathHelper.PiOver2)]
        [TestCase(MathHelper.Pi)]
        [TestCase(-MathHelper.PiOver2)]
        public void GivenAPlayerHeading_WhenProjecting_ThenThePlayerIsCentredAndForwardIsUp(
            float heading)
        {
            Matrix playerWorld = Matrix.CreateRotationY(heading);
            playerWorld.Translation = new Vector3(42.0f, 8.0f, 64.0f);
            Matrix projection = MinimapProjection.CreateTransform(playerWorld);
            Vector3 centre = Vector3.Transform(playerWorld.Translation, projection);
            Vector3 ahead = Vector3.Transform(
                playerWorld.Translation + playerWorld.Forward * 16.0f,
                projection);
            Vector3 right = Vector3.Transform(
                playerWorld.Translation + playerWorld.Right * 16.0f,
                projection);

            Assert.Multiple(() =>
            {
                Assert.That(centre.Length(), Is.EqualTo(0.0f).Within(0.00001f));
                Assert.That(ahead.X, Is.EqualTo(0.0f).Within(0.00001f));
                Assert.That(ahead.Y, Is.EqualTo(-16.0f).Within(0.00001f));
                Assert.That(right.X, Is.EqualTo(16.0f).Within(0.00001f));
                Assert.That(right.Y, Is.EqualTo(0.0f).Within(0.00001f));
            });
        }

        [TestCase(0.0f)]
        [TestCase(0.5f)]
        [TestCase(-0.5f)]
        public void GivenAPitchedAndRolledPlayer_WhenProjecting_ThenElevationDoesNotDistortTheMap(
            float pitch)
        {
            Matrix playerWorld = Matrix.CreateFromYawPitchRoll(0.0f, pitch, 0.5f);
            Matrix projection = MinimapProjection.CreateTransform(playerWorld);
            Vector3 position = Vector3.Transform(new Vector3(16.0f, 42.0f, -64.0f), projection);

            Assert.That(
                Vector3.Distance(position, new Vector3(16.0f, -64.0f, 0.0f)),
                Is.EqualTo(0.0f).Within(0.00001f));
        }

        [Test]
        public void GivenAVerticalHeading_WhenProjecting_ThenTheDefaultHeadingIsUsed()
        {
            Matrix playerWorld = Matrix.Identity;
            playerWorld.Forward = Vector3.Up;
            Matrix projection = MinimapProjection.CreateTransform(playerWorld);
            Vector3 position = Vector3.Transform(new Vector3(16.0f, 42.0f, -64.0f), projection);

            Assert.That(position, Is.EqualTo(new Vector3(16.0f, -64.0f, 0.0f)));
        }
    }
}