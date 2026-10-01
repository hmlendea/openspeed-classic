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
    }
}