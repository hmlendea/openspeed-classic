using System;

using Microsoft.Xna.Framework.Input;

using NUnit.Framework;

using OpenSpeed.Classic.Configuration;
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
        [TestCase(Keys.W, 1.0f, 0.0f)]
        [TestCase(Keys.S, -1.0f, 0.0f)]
        [TestCase(Keys.A, 0.0f, -1.0f)]
        [TestCase(Keys.D, 0.0f, 1.0f)]
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

        [Test]
        public void GivenCustomBindings_WhenReadingInput_ThenBothBindingsAreRecognised()
        {
            DrivingControlsSettings controls = new()
            {
                Accelerate = new ControlBindingSettings
                {
                    Primary = Keys.Space,
                    Secondary = Keys.Enter
                }
            };
            KeyboardState primaryKeyboardState = new(Keys.Space);
            KeyboardState secondaryKeyboardState = new(Keys.Enter);

            TrackCameraInput primaryInput = TrackCameraInputReader.Read(
                primaryKeyboardState,
                controls);
            TrackCameraInput secondaryInput = TrackCameraInputReader.Read(
                secondaryKeyboardState,
                controls);

            Assert.Multiple(() =>
            {
                Assert.That(primaryInput.MovementInput, Is.EqualTo(1.0f));
                Assert.That(secondaryInput.MovementInput, Is.EqualTo(1.0f));
            });
        }

        [TestCase(Keys.Space)]
        [TestCase(Keys.LeftShift)]
        public void GivenAHandbrakeBinding_WhenReadingInput_ThenTheHandbrakeIsApplied(
            Keys key)
        {
            TrackCameraInput input = TrackCameraInputReader.Read(new KeyboardState(key));

            Assert.That(input.IsHandbrakeApplied);
        }

        [Test]
        public void GivenNullControls_WhenReadingInput_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => TrackCameraInputReader.Read(new KeyboardState(), null!),
                Throws.TypeOf<ArgumentNullException>());

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