using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarLateralVelocityUpdaterTests
    {
        [TestCase(1.0f, -6.5f)]
        [TestCase(-1.0f, 6.5f)]
        public void GivenTheHandbrakeWhileSteering_WhenUpdating_ThenLateralSlipBuilds(
            float turningInput,
            float expectedLateralVelocity)
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 10.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                0.25f,
                turningInput,
                true);

            Assert.That(
                physicsState.LateralVelocity,
                Is.EqualTo(expectedLateralVelocity));
        }

        [Test]
        public void GivenAnActiveDrift_WhenReleasingTheHandbrake_ThenGripRecoversProgressively()
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = -8.0f,
                LongitudinalVelocity = 16.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                0.25f,
                1.0f,
                false);

            Assert.That(physicsState.LateralVelocity, Is.EqualTo(-5.5f));
        }

        [Test]
        public void GivenNormalSteeringWithoutSlip_WhenUpdating_ThenTheCarRemainsPlanted()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 16.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                1.0f,
                1.0f,
                false);

            Assert.That(physicsState.LateralVelocity, Is.Zero);
        }

        [Test]
        public void GivenTheHandbrakeWithoutSteering_WhenUpdating_ThenExistingSlipPersists()
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = 4.0f,
                LongitudinalVelocity = 16.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                0.5f,
                0.0f,
                true);

            Assert.That(physicsState.LateralVelocity, Is.EqualTo(3.25f));
        }

        [Test]
        public void GivenCounterSteeringDuringADrift_WhenUpdating_ThenTheCarStabilises()
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = -6.0f,
                LongitudinalVelocity = 16.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                0.25f,
                -1.0f,
                true);

            Assert.That(physicsState.LateralVelocity, Is.Zero);
        }

        [Test]
        public void GivenInsufficientVelocity_WhenApplyingTheHandbrake_ThenNoDriftStarts()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 3.0f
            };

            CarLateralVelocityUpdater.Update(
                physicsState,
                1.0f,
                1.0f,
                true);

            Assert.That(physicsState.LateralVelocity, Is.Zero);
        }

        [TestCase(float.NaN, 1.0f)]
        [TestCase(1.0f, float.NaN)]
        [TestCase(1.0f, -1.0f)]
        public void GivenInvalidPhysicsValues_WhenUpdating_ThenTheValueIsRejected(
            float lateralVelocity,
            float elapsedSeconds)
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = lateralVelocity
            };

            Assert.That(
                () => CarLateralVelocityUpdater.Update(
                    physicsState,
                    elapsedSeconds,
                    1.0f,
                    true),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GivenNullPhysicsState_WhenUpdating_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => CarLateralVelocityUpdater.Update(
                    null!,
                    1.0f,
                    1.0f,
                    true),
                Throws.TypeOf<ArgumentNullException>());
    }
}