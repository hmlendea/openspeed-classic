using Microsoft.Xna.Framework.Input;

using NUnit.Framework;

using OpenSpeed.Classic.Input;

namespace OpenSpeed.Classic.UnitTests.Input
{
    [TestFixture]
    public sealed class TrackCameraInputReaderTests
    {
        [TestCase(Keys.Up, 1.0f, 0.0f)]
        [TestCase(Keys.Down, -1.0f, 0.0f)]
        [TestCase(Keys.Left, 0.0f, -1.0f)]
        [TestCase(Keys.Right, 0.0f, 1.0f)]
        [TestCase(Keys.Escape, 0.0f, 0.0f)]
        public void GivenAKey_WhenReadingInput_ThenTheExpectedAxesAreReturned(
            Keys key,
            float expectedMovementInput,
            float expectedTurningInput)
        {
            KeyboardState keyboardState = new(key);

            TrackCameraInput input = TrackCameraInputReader.Read(keyboardState);

            Assert.Multiple(() =>
            {
                Assert.That(input.MovementInput, Is.EqualTo(expectedMovementInput));
                Assert.That(input.TurningInput, Is.EqualTo(expectedTurningInput));
            });
        }

        [TestCase(Keys.Up, Keys.Down)]
        [TestCase(Keys.Left, Keys.Right)]
        public void GivenOppositeKeys_WhenReadingInput_ThenTheAxesAreNeutral(
            Keys firstKey,
            Keys secondKey)
        {
            KeyboardState keyboardState = new(firstKey, secondKey);

            TrackCameraInput input = TrackCameraInputReader.Read(keyboardState);

            Assert.Multiple(() =>
            {
                Assert.That(input.MovementInput, Is.Zero);
                Assert.That(input.TurningInput, Is.Zero);
            });
        }
    }
}