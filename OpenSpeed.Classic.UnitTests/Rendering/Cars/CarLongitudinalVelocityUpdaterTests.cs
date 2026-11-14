using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarLongitudinalVelocityUpdaterTests
    {
        [Test]
        public void GivenForwardInput_WhenUpdatingRepeatedly_ThenTheCarAcceleratesToItsLimit()
        {
            CarPhysicsState physicsState = new();

            CarLongitudinalVelocityUpdater.Update(physicsState, 1.0f, 1.0f);
            float firstVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 4.0f, 1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(firstVelocity, Is.EqualTo(12.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(40.0f));
            });
        }

        [Test]
        public void GivenNoInputWhileMoving_WhenUpdating_ThenTheCarCoastsToAStop()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 12.0f
            };

            CarLongitudinalVelocityUpdater.Update(physicsState, 1.0f, 0.0f);
            float coastingVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 1.0f, 0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(coastingVelocity, Is.EqualTo(6.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.Zero);
            });
        }

        [Test]
        public void GivenReverseInputWhileMovingForwards_WhenUpdating_ThenTheCarBrakesFirst()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 12.0f
            };

            CarLongitudinalVelocityUpdater.Update(physicsState, 0.25f, -1.0f);
            float brakingVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 0.25f, -1.0f);
            float stoppedVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 1.0f, -1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(brakingVelocity, Is.EqualTo(6.0f));
                Assert.That(stoppedVelocity, Is.Zero);
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(-8.0f));
            });
        }

        [Test]
        public void GivenReverseInput_WhenUpdatingRepeatedly_ThenTheCarReversesToItsLimit()
        {
            CarPhysicsState physicsState = new();

            CarLongitudinalVelocityUpdater.Update(physicsState, 1.0f, -1.0f);
            float firstVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 4.0f, -1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(firstVelocity, Is.EqualTo(-8.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(-16.0f));
            });
        }

        [Test]
        public void GivenForwardInputWhileReversing_WhenUpdating_ThenTheCarBrakesFirst()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = -8.0f
            };

            CarLongitudinalVelocityUpdater.Update(physicsState, 0.25f, 1.0f);
            float brakingVelocity = physicsState.LongitudinalVelocity;
            CarLongitudinalVelocityUpdater.Update(physicsState, 0.25f, 1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(brakingVelocity, Is.EqualTo(-2.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.Zero);
            });
        }

        [TestCase(12.0f, 9.0f)]
        [TestCase(-12.0f, -9.0f)]
        public void GivenTheHandbrakeWhileMoving_WhenUpdating_ThenMostVelocityIsRetained(
            float initialVelocity,
            float expectedVelocity)
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = initialVelocity
            };

            CarLongitudinalVelocityUpdater.Update(
                physicsState,
                0.25f,
                1.0f,
                true);

            Assert.That(
                physicsState.LongitudinalVelocity,
                Is.EqualTo(expectedVelocity));
        }

        [Test]
        public void GivenTheHandbrakeAtRest_WhenUpdating_ThenReverseIsNotEngaged()
        {
            CarPhysicsState physicsState = new();

            CarLongitudinalVelocityUpdater.Update(
                physicsState,
                1.0f,
                -1.0f,
                true);

            Assert.That(physicsState.LongitudinalVelocity, Is.Zero);
        }

        [TestCase(8.0f, 12.0f)]
        [TestCase(-8.0f, -8.0f)]
        public void GivenInputBeyondTheAxisRange_WhenUpdating_ThenTheInputIsClamped(
            float movementInput,
            float expectedVelocity)
        {
            CarPhysicsState physicsState = new();

            CarLongitudinalVelocityUpdater.Update(
                physicsState,
                1.0f,
                movementInput);

            Assert.That(
                physicsState.LongitudinalVelocity,
                Is.EqualTo(expectedVelocity));
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1.0f)]
        public void GivenInvalidElapsedTime_WhenUpdating_ThenTheElapsedTimeIsRejected(
            float elapsedSeconds)
            => Assert.That(
                () => CarLongitudinalVelocityUpdater.Update(
                    new CarPhysicsState(),
                    elapsedSeconds,
                    1.0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        [Test]
        public void GivenInvalidVelocity_WhenUpdating_ThenTheVelocityIsRejected()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = float.NaN
            };

            Assert.That(
                () => CarLongitudinalVelocityUpdater.Update(
                    physicsState,
                    1.0f,
                    1.0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GivenNullPhysicsState_WhenUpdating_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => CarLongitudinalVelocityUpdater.Update(null!, 1.0f, 1.0f),
                Throws.TypeOf<ArgumentNullException>());
    }
}