using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class NativeContactGeometryTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void GivenAnInactiveContactBranch_WhenResolving_ThenStateRemainsUnchanged(
            byte activeBody,
            byte contactMode)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, activeBody);
            car.WriteByte(0x8C, contactMode);
            car[0xA0] = 42;
            car[0xAC] = -32;
            car[0x14C] = 0x12345678;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NativeContactGeometry.TryResolveContactMode(car), Is.False);
                Assert.That(car[0xA0], Is.EqualTo(42));
                Assert.That(car[0xAC], Is.EqualTo(-32));
                Assert.That(car[0x14C], Is.EqualTo(0x12345678));
            }

        }

        [TestCase(0, 0x78000)]
        [TestCase(0x40, 0x70000)]
        public void GivenContactMode_WhenResolving_ThenHeightAndExactWidthResetsAreApplied(
            int roleFlags,
            int expectedHeight)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8C, 1);
            car.WriteByte(0x8E, 8);
            car.WriteByte(0x8F, 16);
            car[0x1F4] = roleFlags;
            car[0x128] = X86Math.One;
            car[0x140] = 0x70000;
            car[0x10C] = 0x8000;
            car[0x14C] = 0x12345678;
            car[0xAC] = -32;
            car[0xA8] = 42;
            car[0x15C] = 613;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NativeContactGeometry.TryResolveContactMode(car));
                Assert.That(car[0xA0], Is.EqualTo(expectedHeight));
                Assert.That(car[0xAC], Is.Zero);
                Assert.That(car[0x14C], Is.EqualTo(0x12340000));
                Assert.That(car.ReadByte(0x8E), Is.Zero);
                Assert.That(car.ReadByte(0x8F), Is.EqualTo(16));
                Assert.That(car.ReadByte(0x8D), Is.EqualTo(1));
                Assert.That(car.ReadByte(0x8C), Is.EqualTo(1));
                Assert.That(car[0xA8], Is.EqualTo(42));
                Assert.That(car[0x15C], Is.EqualTo(613));
            }

        }

        [TestCase(0x1998)]
        [TestCase(0x1999)]
        public void GivenAnUnavailablePlane_WhenResolving_ThenTheSentinelIsPreserved(int normalSecond)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8C, 1);
            car[0x128] = normalSecond;
            car[0x10C] = 0x8000;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NativeContactGeometry.TryResolveContactMode(car));
                Assert.That(car[0xA0], Is.EqualTo(unchecked((int)0x83008000)));
            }

        }
    }
}