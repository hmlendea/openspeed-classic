using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OrientedBoxCandidateTests
    {
        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { 0, 1, 0 })]
        [TestCase(new[] { 0, 0, 1 })]
        [TestCase(new[] { -1, 0, 0 })]
        [TestCase(new[] { 0, -1, 0 })]
        [TestCase(new[] { 0, 0, -1 })]
        public void GivenCoincidentBoxes_WhenApplyingTheFirstProbe_ThenTheSecondCornerIsEmitted(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplyFirstProbe(
                first, second, scratch, coefficients, out FixedVector? point));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(point.Second, Is.EqualTo(unchecked(coefficients[1] * (2 * X86Math.One))));
            Assert.That(point.Third, Is.EqualTo(unchecked(coefficients[2] * (3 * X86Math.One))));
        }

        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { -1, 0, 0 })]
        public void GivenCoincidentBoxes_WhenApplyingTheFirstProbeWithNormal_ThenThePositiveSecondNormalIsEmitted(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplyFirstProbeWithNormal(
                first, second, scratch, coefficients, out FixedVector? point, out FixedVector? normal));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(normal!.First, Is.EqualTo(coefficients[0] * X86Math.One));
            Assert.That(normal.Second, Is.EqualTo(coefficients[1] * X86Math.One));
            Assert.That(normal.Third, Is.EqualTo(coefficients[2] * X86Math.One));
        }

        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { 0, 1, 0 })]
        [TestCase(new[] { 0, 0, 1 })]
        [TestCase(new[] { -1, 0, 0 })]
        [TestCase(new[] { 0, -1, 0 })]
        [TestCase(new[] { 0, 0, -1 })]
        public void GivenCoincidentBoxes_WhenApplyingTheSecondProbe_ThenTheFirstCornerIsEmitted(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplySecondProbe(
                first, second, scratch, coefficients, out FixedVector? point));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(point.Second, Is.EqualTo(unchecked(coefficients[1] * (2 * X86Math.One))));
            Assert.That(point.Third, Is.EqualTo(unchecked(coefficients[2] * (3 * X86Math.One))));
        }

        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { -1, 0, 0 })]
        public void GivenCoincidentBoxes_WhenApplyingTheSecondProbeWithNormal_ThenTheNegatedFirstNormalIsEmitted(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplySecondProbeWithNormal(
                first, second, scratch, coefficients, out FixedVector? point, out FixedVector? normal));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(normal!.First, Is.EqualTo(unchecked(-coefficients[0] * X86Math.One)));
            Assert.That(normal.Second, Is.EqualTo(unchecked(-coefficients[1] * X86Math.One)));
            Assert.That(normal.Third, Is.EqualTo(unchecked(-coefficients[2] * X86Math.One)));
        }

        [TestCase(0x10001)]
        [TestCase(-0x20001)]
        public void GivenAProbeOutsideTheFirstExtent_WhenApplyingTheFirstProbe_ThenItIsRejected(int separation)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = separation;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplyFirstProbe(
                first, second, scratch, [1, 0, 0], out FixedVector? point), Is.False);
            Assert.That(point, Is.Null);
        }

        [TestCase(-0x10001)]
        [TestCase(0x20001)]
        public void GivenAProbeOutsideTheSecondExtent_WhenApplyingTheSecondProbe_ThenItIsRejected(int separation)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = separation;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplySecondProbe(
                first, second, scratch, [1, 0, 0], out FixedVector? point), Is.False);
            Assert.That(point, Is.Null);
        }

        [Test]
        public void GivenAnAcceptedCandidate_WhenComputingTheCorner_ThenTheExtentIsAppliedBeforeTheCoefficient()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x108] = 0xFFFF;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxCandidate.TryApplyFirstProbeWithNormal(
                first, second, scratch, [1, 0, 0], out FixedVector? point, out _));
            Assert.That(point!.First, Is.EqualTo(0xFFFF));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void GivenMalformedOrMissingInputs_WhenApplying_ThenErrorsAreReported(int entryPoint)
        {
            CarMemory car = BuildCar();
            OrientedBoxScratch scratch = new();

            Assert.That(
                () => Invoke(entryPoint, null!, car, scratch, [1, 0, 0]),
                Throws.ArgumentNullException);
            Assert.That(
                () => Invoke(entryPoint, car, null!, scratch, [1, 0, 0]),
                Throws.ArgumentNullException);
            Assert.That(
                () => Invoke(entryPoint, car, car, null!, [1, 0, 0]),
                Throws.ArgumentNullException);
            Assert.That(
                () => Invoke(entryPoint, car, car, scratch, [1, 0]),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("coefficients"));
        }

        private static bool Invoke(
            int entryPoint,
            CarMemory first,
            CarMemory second,
            OrientedBoxScratch scratch,
            int[] coefficients)
            => entryPoint switch
            {
                1 => OrientedBoxCandidate.TryApplyFirstProbe(first, second, scratch, coefficients, out _),
                2 => OrientedBoxCandidate.TryApplyFirstProbeWithNormal(first, second, scratch, coefficients, out _, out _),
                3 => OrientedBoxCandidate.TryApplySecondProbe(first, second, scratch, coefficients, out _),
                _ => OrientedBoxCandidate.TryApplySecondProbeWithNormal(first, second, scratch, coefficients, out _, out _)
            };

        private static OrientedBoxScratch BuildScratch(CarMemory first, CarMemory second)
        {
            OrientedBoxScratch scratch = new();
            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            return scratch;
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = 2 * X86Math.One;
            car[0x110] = 3 * X86Math.One;

            return car;
        }
    }
}