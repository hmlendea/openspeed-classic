using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OrientedBoxScratchCalculatorTests
    {
        [Test]
        public void GivenIdenticalBases_WhenCalculating_ThenRotationIsIdentity()
        {
            CarMemory first = BuildCar(0, 0, 0);
            CarMemory second = BuildCar(0, 0, 0);
            OrientedBoxScratch scratch = new();

            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    int expected = row == column ? X86Math.One : 0;
                    Assert.That(scratch.Rotation[row * FixedMatrix.Dimension + column], Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void GivenTransposedBases_WhenCalculating_ThenRotationIsIdentity()
        {
            CarMemory first = BuildCar(0, 0, 0);
            CarMemory second = BuildCar(0, 0, 0);
            FixedMatrices.Write(second, 0xC4, FixedMatrices.Transpose(FixedMatrices.Identity()));
            OrientedBoxScratch scratch = new();

            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scratch.Rotation[0], Is.EqualTo(X86Math.One));
                Assert.That(scratch.Rotation[4], Is.EqualTo(X86Math.One));
                Assert.That(scratch.Rotation[8], Is.EqualTo(X86Math.One));
                Assert.That(scratch.Rotation[1], Is.Zero);
                Assert.That(scratch.Rotation[3], Is.Zero);
            }

        }

        [Test]
        public void GivenDifferentBases_WhenCalculating_ThenRotationIsTheRowDotProduct()
        {
            CarMemory first = BuildCar(0, 0, 0);
            CarMemory second = BuildCar(0, 0, 0);
            FixedMatrix basis = FixedMatrices.Identity();
            basis[0, 0] = 0x8000;
            basis[0, 1] = 0x8000;
            FixedMatrices.Write(second, 0xC4, basis);
            OrientedBoxScratch scratch = new();

            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scratch.Rotation[0], Is.EqualTo(0x8000));
                Assert.That(scratch.Rotation[3], Is.EqualTo(0x8000));
                Assert.That(scratch.Rotation[1], Is.Zero);
                Assert.That(scratch.Rotation[4], Is.EqualTo(X86Math.One));
            }

        }

        [Test]
        public void GivenSeparatedCentres_WhenCalculating_ThenCentreDeltasProjectOntoBothBases()
        {
            CarMemory first = BuildCar(0, 0, 0);
            CarMemory second = BuildCar(0x10000, 0x20000, -0x10000);
            OrientedBoxScratch scratch = new();

            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scratch.DeltaInFirst[0], Is.EqualTo(0x10000));
                Assert.That(scratch.DeltaInFirst[1], Is.EqualTo(0x20000));
                Assert.That(scratch.DeltaInFirst[2], Is.EqualTo(-0x10000));
                Assert.That(scratch.DeltaInSecond[0], Is.EqualTo(0x10000));
                Assert.That(scratch.DeltaInSecond[1], Is.EqualTo(0x20000));
                Assert.That(scratch.DeltaInSecond[2], Is.EqualTo(-0x10000));
            }

        }

        [Test]
        public void GivenExtentsAndRotation_WhenCalculating_ThenEachAxisUsesItsSourceExtent()
        {
            CarMemory first = BuildCar(0, 0, 0);
            CarMemory second = BuildCar(0, 0, 0);
            first[0x108] = X86Math.One;
            first[0x10C] = 0x20000;
            first[0x110] = 0x30000;
            second[0x108] = 0x4000;
            second[0x10C] = 0x8000;
            second[0x110] = 0xC000;
            OrientedBoxScratch scratch = new();

            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scratch.SecondExtentInFirst[0], Is.EqualTo(0x4000));
                Assert.That(scratch.SecondExtentInFirst[4], Is.EqualTo(0x8000));
                Assert.That(scratch.SecondExtentInFirst[8], Is.EqualTo(0xC000));
                Assert.That(scratch.FirstExtentInSecond[0], Is.EqualTo(X86Math.One));
                Assert.That(scratch.FirstExtentInSecond[4], Is.EqualTo(0x20000));
                Assert.That(scratch.FirstExtentInSecond[8], Is.EqualTo(0x30000));
            }

        }

        [Test]
        public void GivenNonZeroValues_WhenClearing_ThenEveryScratchArrayIsReset()
        {
            OrientedBoxScratch scratch = new();
            scratch.Rotation[0] = 42;
            scratch.FirstExtentInSecond[0] = 42;
            scratch.SecondExtentInFirst[0] = 42;
            scratch.DeltaInFirst[0] = 42;
            scratch.DeltaInSecond[0] = 42;

            scratch.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scratch.Rotation, Is.All.Zero);
                Assert.That(scratch.FirstExtentInSecond, Is.All.Zero);
                Assert.That(scratch.SecondExtentInFirst, Is.All.Zero);
                Assert.That(scratch.DeltaInFirst, Is.All.Zero);
                Assert.That(scratch.DeltaInSecond, Is.All.Zero);
            }

        }

        [Test]
        public void GivenMissingInputs_WhenCalculating_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar(0, 0, 0);
            OrientedBoxScratch scratch = new();

            Assert.That(
                () => OrientedBoxScratchCalculator.Calculate(null!, car, scratch),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxScratchCalculator.Calculate(car, null!, scratch),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxScratchCalculator.Calculate(car, car, null!),
                Throws.ArgumentNullException);
        }

        private static CarMemory BuildCar(int first, int second, int third)
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x9C] = first;
            car[0xA0] = second;
            car[0xA4] = third;

            return car;
        }
    }
}