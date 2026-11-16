using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarSteeringCalculatorTests
    {
        [Test]
        public void GivenLowForwardVelocity_WhenSteeringRight_ThenTheCarTurnsResponsively()
        {
            float rotation = CarSteeringCalculator.CalculateRotation(
                1.0f,
                1.0f,
                1.0f);

            Assert.That(
                rotation,
                Is.LessThan(-MathHelper.ToRadians(15.0f)));
        }

        [Test]
        public void GivenMaximumForwardVelocity_WhenSteering_ThenYawVelocityIsLimited()
        {
            float rotation = CarSteeringCalculator.CalculateRotation(
                40.0f,
                1.0f,
                1.0f);

            Assert.That(
                rotation,
                Is.EqualTo(-MathHelper.ToRadians(90.0f)).Within(0.001f));
        }

        [Test]
        public void GivenTheHandbrakeAtVelocity_WhenSteering_ThenYawIsAmplified()
        {
            float normalRotation = CarSteeringCalculator.CalculateRotation(
                8.0f,
                1.0f,
                0.25f,
                false);
            float handbrakeRotation = CarSteeringCalculator.CalculateRotation(
                8.0f,
                1.0f,
                0.25f,
                true);

            Assert.That(
                MathF.Abs(handbrakeRotation),
                Is.GreaterThan(MathF.Abs(normalRotation)));
        }

        [Test]
        public void GivenReverseVelocity_WhenSteeringRight_ThenTheRotationIsReversed()
        {
            float rotation = CarSteeringCalculator.CalculateRotation(
                -1.0f,
                1.0f,
                1.0f);

            Assert.That(rotation, Is.GreaterThan(MathHelper.ToRadians(15.0f)));
        }

        [Test]
        public void GivenNoVelocity_WhenSteering_ThenTheCarDoesNotRotate()
            => Assert.That(
                CarSteeringCalculator.CalculateRotation(0.0f, 1.0f, 1.0f),
                Is.Zero);

        [TestCase(float.NaN, 1.0f)]
        [TestCase(1.0f, float.NaN)]
        [TestCase(1.0f, -1.0f)]
        public void GivenInvalidPhysicsValues_WhenCalculating_ThenTheValueIsRejected(
            float longitudinalVelocity,
            float elapsedSeconds)
            => Assert.That(
                () => CarSteeringCalculator.CalculateRotation(
                    longitudinalVelocity,
                    1.0f,
                    elapsedSeconds),
                Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}