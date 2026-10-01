using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class VignetteMaskCalculatorTests
    {
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.06f, 0.5f)]
        [TestCase(0.94f, 0.5f)]
        [TestCase(0.5f, 0.1f)]
        [TestCase(0.5f, 0.9f)]
        public void GivenAPositionInsideTheLargeOval_WhenCalculating_ThenTheScreenIsClear(
            float normalizedPositionX,
            float normalizedPositionY)
            => Assert.That(
                VignetteMaskCalculator.CalculateDarkness(
                    normalizedPositionX,
                    normalizedPositionY),
                Is.Zero.Within(0.001f));

        [TestCase(0.0f, 0.5f)]
        [TestCase(1.0f, 0.5f)]
        [TestCase(0.5f, 0.0f)]
        [TestCase(0.5f, 1.0f)]
        [TestCase(0.0f, 0.0f)]
        public void GivenAPositionAtTheScreenEdge_WhenCalculating_ThenDarknessIsMaximum(
            float normalizedPositionX,
            float normalizedPositionY)
            => Assert.That(
                VignetteMaskCalculator.CalculateDarkness(
                    normalizedPositionX,
                    normalizedPositionY),
                Is.EqualTo(0.15f).Within(0.001f));

        [Test]
        public void GivenOuterPositions_WhenCalculating_ThenDarknessIncreases()
        {
            float innerDarkness = VignetteMaskCalculator.CalculateDarkness(
                0.04f,
                0.5f);
            float outerDarkness = VignetteMaskCalculator.CalculateDarkness(
                0.02f,
                0.5f);

            Assert.Multiple(() =>
            {
                Assert.That(innerDarkness, Is.GreaterThan(0.0f));
                Assert.That(outerDarkness, Is.GreaterThan(innerDarkness));
                Assert.That(outerDarkness, Is.LessThanOrEqualTo(0.15f));
            });
        }

        [TestCase(0.01f, 0.5f)]
        [TestCase(0.99f, 0.5f)]
        [TestCase(0.5f, 0.01f)]
        [TestCase(0.5f, 0.99f)]
        public void GivenAPositionNearAnEdge_WhenCalculating_ThenDarknessIsBelowMaximum(
            float normalizedPositionX,
            float normalizedPositionY)
            => Assert.That(
                VignetteMaskCalculator.CalculateDarkness(
                    normalizedPositionX,
                    normalizedPositionY),
                Is.LessThan(0.15f));
    }
}