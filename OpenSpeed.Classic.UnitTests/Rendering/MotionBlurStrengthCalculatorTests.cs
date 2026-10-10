using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class MotionBlurStrengthCalculatorTests
    {
        [TestCase(79.0f, 0.0f)]
        [TestCase(80.0f, 0.0f)]
        [TestCase(100.0f, 0.225f)]
        [TestCase(120.0f, 0.45f)]
        [TestCase(121.0f, 0.45f)]
        [TestCase(-100.0f, 0.225f)]
        public void GivenASpeed_WhenCalculating_ThenTheBlurStrengthIsBounded(
            float speedKilometresPerHour,
            float expectedStrength)
            => Assert.That(
                MotionBlurStrengthCalculator.Calculate(
                    speedKilometresPerHour / 3.6f,
                    80.0f),
                Is.EqualTo(expectedStrength).Within(0.001f));

        [TestCase(float.NaN, 80.0f)]
        [TestCase(float.PositiveInfinity, 80.0f)]
        [TestCase(0.0f, float.NaN)]
        [TestCase(0.0f, -1.0f)]
        [TestCase(0.0f, 120.0f)]
        [TestCase(0.0f, 121.0f)]
        public void GivenAnInvalidValue_WhenCalculating_ThenTheValueIsRejected(
            float longitudinalVelocity,
            float minimumSpeedKilometresPerHour)
            => Assert.That(
                () => MotionBlurStrengthCalculator.Calculate(
                    longitudinalVelocity,
                    minimumSpeedKilometresPerHour),
                Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}