using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OrientedBoxFaceCandidateTests
    {
        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { 0, 1, 0 })]
        [TestCase(new[] { 0, 0, 1 })]
        [TestCase(new[] { -1, 0, 0 })]
        [TestCase(new[] { 0, -1, 0 })]
        [TestCase(new[] { 0, 0, -1 })]
        public void GivenCoincidentBoxes_WhenTestingTheFirstProbe_ThenItIsAcceptedWithTheSecondCornerAndPositiveNormal(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxFaceCandidate.TryApplyFirstProbe(
                first, second, scratch, coefficients, out FixedVector? point, out FixedVector? normal));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(point.Second, Is.EqualTo(unchecked(coefficients[1] * (2 * X86Math.One))));
            Assert.That(point.Third, Is.EqualTo(unchecked(coefficients[2] * (3 * X86Math.One))));
            Assert.That(normal!.First, Is.EqualTo(coefficients[0] * X86Math.One));
            Assert.That(normal.Second, Is.EqualTo(coefficients[1] * X86Math.One));
            Assert.That(normal.Third, Is.EqualTo(coefficients[2] * X86Math.One));
        }

        [TestCase(new[] { 1, 0, 0 })]
        [TestCase(new[] { -1, 0, 0 })]
        [TestCase(new[] { 0, 0, -1 })]
        public void GivenCoincidentBoxes_WhenTestingTheSecondProbe_ThenItIsAcceptedWithTheFirstCornerAndNegatedNormal(
            int[] coefficients)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxFaceCandidate.TryApplySecondProbe(
                first, second, scratch, coefficients, out FixedVector? point, out FixedVector? normal));
            Assert.That(point!.First, Is.EqualTo(unchecked(coefficients[0] * X86Math.One)));
            Assert.That(point.Second, Is.EqualTo(unchecked(coefficients[1] * (2 * X86Math.One))));
            Assert.That(point.Third, Is.EqualTo(unchecked(coefficients[2] * (3 * X86Math.One))));
            Assert.That(normal!.First, Is.EqualTo(unchecked(-coefficients[0] * X86Math.One)));
            Assert.That(normal.Second, Is.EqualTo(unchecked(-coefficients[1] * X86Math.One)));
            Assert.That(normal.Third, Is.EqualTo(unchecked(-coefficients[2] * X86Math.One)));
        }

        [TestCase(0x10001)]
        [TestCase(-0x20001)]
        public void GivenAProbeOutsideTheFirstExtent_WhenTestingTheFirstProbe_ThenItIsRejected(
            int separation)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = separation;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxFaceCandidate.TryApplyFirstProbe(
                first, second, scratch, [1, 0, 0], out FixedVector? point, out FixedVector? normal), Is.False);
            Assert.That(point, Is.Null);
            Assert.That(normal, Is.Null);
        }

        [TestCase(-0x10001)]
        [TestCase(0x20001)]
        public void GivenAProbeOutsideTheSecondExtent_WhenTestingTheSecondProbe_ThenItIsRejected(
            int separation)
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = separation;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxFaceCandidate.TryApplySecondProbe(
                first, second, scratch, [1, 0, 0], out FixedVector? point, out FixedVector? normal), Is.False);
            Assert.That(point, Is.Null);
            Assert.That(normal, Is.Null);
        }

        [Test]
        public void GivenAnAcceptedCandidate_WhenComputingTheCorner_ThenTheExtentIsAppliedBeforeTheCoefficient()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x108] = 0xFFFF;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            Assert.That(OrientedBoxFaceCandidate.TryApplyFirstProbe(
                first, second, scratch, [1, 0, 0], out FixedVector? point, out _));
            Assert.That(point!.First, Is.EqualTo(0xFFFF));
        }

        [Test]
        public void GivenMissingOrMalformedInputs_WhenTesting_ThenErrorsAreReported()
        {
            CarMemory car = BuildCar();
            OrientedBoxScratch scratch = new();

            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplyFirstProbe(null!, car, scratch, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplyFirstProbe(car, null!, scratch, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplyFirstProbe(car, car, null!, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplyFirstProbe(car, car, scratch, [1, 0], out _, out _),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("coefficients"));
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplySecondProbe(null!, car, scratch, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplySecondProbe(car, null!, scratch, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplySecondProbe(car, car, null!, [1, 0, 0], out _, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxFaceCandidate.TryApplySecondProbe(car, car, scratch, [1, 0], out _, out _),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("coefficients"));
        }

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