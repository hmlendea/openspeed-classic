using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PlayerStateInitialiserTests
    {
        [Test]
        public void GivenFreshState_WhenInitialising_ThenAllNativeDefaultsAreExact()
        {
            CarMemory car = new();
            PlayerStateInitialiser.Initialise(car);
            CarMemory expected = new();
            expected[0xB4] = 0xA0000;
            expected[0xB8] = 0x1999;
            expected[0xF4] = 0xF0000;
            expected[0xF8] = 0x1111;
            expected.WriteByte(0x8D, 1);
            expected.WriteByte(0x2D6, 1);
            expected.WriteByte(0x2D9, 1);
            expected.WriteByte(0x2DA, 1);
            expected[0x94] = expected[0x100] = expected[0x218] = X86Math.One;
            expected[0x180] = expected[0x184] = 1;
            expected[0x150] = 0x640000;

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenExistingCoefficientsAndGeometry_WhenInitialising_ThenTheyArePreserved()
        {
            CarMemory car = new();
            car[0xB4] = 42;
            car[0xB8] = 64;
            car[0xF4] = 128;
            car[0xF8] = 256;
            car[0x108] = 0x10000;
            car[0x10C] = 0x20000;
            car[0x110] = 0x40000;
            car[0x114] = 873;
            car[0x2F0] = 613;
            car[0x578] = 42;
            car[0xE8] = 64;
            car.WriteByte(0x2DC, 1);
            PlayerStateInitialiser.Initialise(car);

            Assert.Multiple(() =>
            {
                Assert.That(car[0xB4], Is.EqualTo(42));
                Assert.That(car[0xB8], Is.EqualTo(64));
                Assert.That(car[0xF4], Is.EqualTo(128));
                Assert.That(car[0xF8], Is.EqualTo(256));
                Assert.That(car[0x108], Is.EqualTo(0x10000));
                Assert.That(car[0x10C], Is.EqualTo(0x20000));
                Assert.That(car[0x110], Is.EqualTo(0x40000));
                Assert.That(car[0x114], Is.EqualTo(873));
                Assert.That(car[0x2F0], Is.Zero);
                Assert.That(car[0x578], Is.Zero);
                Assert.That(car[0xE8], Is.Zero);
                Assert.That(car.ReadByte(0x2DC), Is.Zero);
            });
        }

        [Test]
        public void GivenNativeBodyDimensions_WhenInitialising_ThenTheyAreInjectedWithoutGeometryInference()
        {
            CarMemory car = new();
            PlayerStateInitialiser.Initialise(
                car,
                new FixedVector { First = 0x10000, Second = 0x20000, Third = 0x30000 },
                0x40000);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x108], Is.EqualTo(0x10000));
                Assert.That(car[0x10C], Is.EqualTo(0x20000));
                Assert.That(car[0x110], Is.EqualTo(0x30000));
                Assert.That(car[0x114], Is.EqualTo(0x40000));
            });
        }
    }
}