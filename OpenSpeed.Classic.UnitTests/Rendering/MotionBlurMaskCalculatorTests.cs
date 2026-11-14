using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class MotionBlurMaskCalculatorTests
    {
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.2f, 0.5f)]
        [TestCase(0.8f, 0.5f)]
        [TestCase(0.5f, 0.28f)]
        [TestCase(0.5f, 0.72f)]
        public void GivenAPositionInsideTheCentralOval_WhenCalculating_ThenBlurIsAbsent(
            float normalizedPositionX,
            float normalizedPositionY)
            => Assert.That(
                MotionBlurMaskCalculator.CalculateStrength(
                    normalizedPositionX,
                    normalizedPositionY,
                    0.33f),
                Is.Zero.Within(0.001f));

        [TestCase(0.0f, 0.5f)]
        [TestCase(1.0f, 0.5f)]
        [TestCase(0.5f, 0.0f)]
        [TestCase(0.5f, 1.0f)]
        [TestCase(0.0f, 0.0f)]
        public void GivenAPositionAtTheScreenEdge_WhenCalculating_ThenBlurIsMaximum(
            float normalizedPositionX,
            float normalizedPositionY)
            => Assert.That(
                MotionBlurMaskCalculator.CalculateStrength(
                    normalizedPositionX,
                    normalizedPositionY,
                    0.33f),
                Is.EqualTo(0.33f).Within(0.001f));

        [Test]
        public void GivenPositionsOutsideTheOval_WhenCalculating_ThenBlurIncreasesGradually()
        {
            float innerStrength = MotionBlurMaskCalculator.CalculateStrength(
                0.15f,
                0.5f,
                0.33f);
            float outerStrength = MotionBlurMaskCalculator.CalculateStrength(
                0.05f,
                0.5f,
                0.33f);

            Assert.Multiple(() =>
            {
                Assert.That(innerStrength, Is.GreaterThan(0.0f));
                Assert.That(outerStrength, Is.GreaterThan(innerStrength));
                Assert.That(outerStrength, Is.LessThanOrEqualTo(0.33f));
            });
        }

        [TestCase(float.NaN, 0.5f, 0.33f)]
        [TestCase(-0.01f, 0.5f, 0.33f)]
        [TestCase(1.01f, 0.5f, 0.33f)]
        [TestCase(0.5f, float.NaN, 0.33f)]
        [TestCase(0.5f, -0.01f, 0.33f)]
        [TestCase(0.5f, 1.01f, 0.33f)]
        [TestCase(0.5f, 0.5f, float.NaN)]
        [TestCase(0.5f, 0.5f, -0.01f)]
        [TestCase(0.5f, 0.5f, 1.01f)]
        public void GivenAnInvalidValue_WhenCalculating_ThenTheValueIsRejected(
            float normalizedPositionX,
            float normalizedPositionY,
            float edgeStrength)
            => Assert.That(
                () => MotionBlurMaskCalculator.CalculateStrength(
                    normalizedPositionX,
                    normalizedPositionY,
                    edgeStrength),
                Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}