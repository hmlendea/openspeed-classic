using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class SpeedometerReadingCalculatorTests
    {
        [TestCase(0.0f, 0, 0.0f)]
        [TestCase(87.3f, 87, 0.36375f)]
        [TestCase(-87.3f, 87, 0.36375f)]
        [TestCase(239.0f, 239, 0.995833f)]
        [TestCase(240.0f, 240, 1.0f)]
        [TestCase(241.0f, 241, 1.0f)]
        public void GivenAVelocity_WhenCalculating_ThenTheReadingIsBounded(
            float speedKilometresPerHour,
            int expectedRoundedSpeed,
            float expectedDialRatio)
        {
            SpeedometerReading reading = SpeedometerReadingCalculator.Calculate(
                speedKilometresPerHour / 3.6f);

            Assert.Multiple(() =>
            {
                Assert.That(
                    reading.SpeedKilometresPerHour,
                    Is.EqualTo(expectedRoundedSpeed));
                Assert.That(
                    reading.DialRatio,
                    Is.EqualTo(expectedDialRatio).Within(0.00001f));
            });
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void GivenAnInvalidVelocity_WhenCalculating_ThenTheValueIsRejected(
            float longitudinalVelocity)
            => Assert.That(
                () => SpeedometerReadingCalculator.Calculate(longitudinalVelocity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}