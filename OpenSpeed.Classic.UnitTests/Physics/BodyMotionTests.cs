using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class BodyMotionTests
    {
        [TestCase(0, 0x10000, 0x400)]
        [TestCase(0, -0x10001, -0x400)]
        [TestCase(int.MaxValue, 0x10000, unchecked((int)0x800003FF))]
        public void GivenPositionFixtures_WhenIntegrating_ThenTheNativePositionIsExact(int position, int velocity, int expected)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car[0x9C] = position;
            car[0xA8] = velocity;
            BodyMotion.IntegratePosition(car);

            Assert.That(car[0x9C], Is.EqualTo(expected));
        }

        [Test]
        public void GivenAStationaryBasis_WhenIntegrating_ThenAllBytesRemainIdentical()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8F, 1);
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            byte[] original = car.Snapshot();
            BodyMotion.IntegratePlayerBasis(car);

            Assert.That(car.Snapshot(), Is.EqualTo(original));
        }

        [Test]
        public void GivenAnInactiveCar_WhenIntegrating_ThenAllBytesRemainIdentical()
        {
            CarMemory car = new();
            car[0xA8] = 0x10000;
            car[0xEC] = 0x10000;
            byte[] original = car.Snapshot();
            BodyMotion.IntegratePosition(car);
            BodyMotion.IntegratePlayerBasis(car);

            Assert.That(car.Snapshot(), Is.EqualTo(original));
        }

        [Test]
        public void GivenAnExpiredNormalisationCountdown_WhenIntegrating_ThenItResetsImmediately()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8F, 0);
            car[0xEC] = 0x500;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            BodyMotion.IntegratePlayerBasis(car);

            Assert.That(car.ReadByte(0x8F), Is.EqualTo(0x10));
        }

        [Test]
        public void GivenAPlayerPositionScale_WhenIntegrating_ThenVelocityIsScaledBeforeTheTickShift()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car[0x218] = 0x20000;
            car[0xA8] = 0x10000;
            car[0xAC] = -0x10000;
            car[0xB0] = 0x20000;

            BodyMotion.IntegrateScaledPlayerPosition(car);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x9C], Is.EqualTo(0x800));
                Assert.That(car[0xA0], Is.EqualTo(-0x800));
                Assert.That(car[0xA4], Is.EqualTo(0x1000));
                Assert.That(car[0x94], Is.EqualTo(0x20000));
            });
        }
        [Test]
        public void GivenANormalisationCountdownAboveOne_WhenIntegrating_ThenItIsNotReset()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8F, 0x10);
            car[0xEC] = 0x500;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            BodyMotion.IntegratePlayerBasis(car);

            // `dec` followed by an unsigned `jbe` fires only for the previous countdown values
            // 0x00 and 0x01, so a countdown of 0x10 decrements to 0x0F without normalising.
            Assert.That(car.ReadByte(0x8F), Is.EqualTo(0x0F));
        }

        [Test]
        public void GivenAHighNormalisationCountdown_WhenIntegrating_ThenTheSignedInterpretationWouldHaveReset()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8F, 0x80);
            car[0xEC] = 0x500;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            BodyMotion.IntegratePlayerBasis(car);

            // A signed `<= 0` test would read 0x80 as -128 and normalise, overwriting the
            // countdown with 0x10. The unsigned source test must leave it at 0x7F.
            Assert.That(car.ReadByte(0x8F), Is.EqualTo(0x7F));
        }

        [Test]
        public void GivenBothFirstAndThirdRateComponents_WhenIntegrating_ThenTheSourceCompositionOrderIsUsed()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8F, 0x10);
            car[0xE8] = 0x2000;
            car[0xEC] = 0x2000;
            car[0xF0] = 0x2000;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            BodyMotion.IntegratePlayerBasis(car);

            // The source composes Rx * Ry * Rz for the applied delta: Rx then Ry, then the
            // accumulator multiplied on the right by Rz. The previous implementation applied
            // Rz first and Rx second, producing Rz * Rx * Ry. The two differ because the
            // rotation matrices do not commute; this fixture records which product is applied so
            // a future reordering fails here.
            FixedMatrix expectedDelta = FixedMatrices.Multiply(
                FixedMatrices.Multiply(
                    FixedMatrices.RotateFirst(X86Math.TruncatePowerOfTwo(0x2000, 6)),
                    FixedMatrices.RotateSecond(X86Math.TruncatePowerOfTwo(0x2000, 6))),
                FixedMatrices.RotateThird(X86Math.TruncatePowerOfTwo(0x2000, 6)));
            FixedMatrix expectedBasis = FixedMatrices.Multiply(FixedMatrices.Identity(), expectedDelta);

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    int actual = car[0xC4 + (row * FixedMatrix.Dimension + column) * sizeof(int)];
                    Assert.That(actual, Is.EqualTo(expectedBasis[row, column]), $"cell {row},{column}");
                }
            }
        }
    }
}