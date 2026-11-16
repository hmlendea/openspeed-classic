using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class CarMemoryTests
    {
        [Test]
        public void GivenOverlappingFields_WhenMutating_ThenAllWidthsShareLittleEndianStorage()
        {
            CarMemory car = new();
            car[0x2D4] = 0x12345678;
            car.WriteByte(0x2D5, 0xFF);
            car.WriteWord(0x2D6, 0xABCD);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x2D4], Is.EqualTo(unchecked((int)0xABCDFF78)));
                Assert.That(car.ReadByte(0x2D4), Is.EqualTo(0x78));
                Assert.That(car.ReadSignedByte(0x2D5), Is.EqualTo(-1));
                Assert.That(car.ReadWord(0x2D6), Is.EqualTo(0xABCD));
                Assert.That(car.ReadSignedWord(0x2D6), Is.EqualTo(unchecked((short)0xABCD)));
                Assert.That(car.ReadByte(0x2D3), Is.Zero);
                Assert.That(car.ReadByte(0x2D8), Is.Zero);
            });
        }

        [Test]
        public void GivenUnalignedFields_WhenMutating_ThenTheExactBytesAreStored()
        {
            CarMemory car = new();
            car[1] = -1;
            byte[] snapshot = car.Snapshot();
            snapshot[1] = 0;

            Assert.Multiple(() =>
            {
                Assert.That(car[1], Is.EqualTo(-1));
                Assert.That(car.ReadByte(0), Is.Zero);
                Assert.That(car.ReadByte(5), Is.Zero);
                Assert.That(car[0x58C], Is.Zero);
            });
        }

        [Test]
        public void GivenAnInvalidOffset_WhenAccessing_ThenTheStateIsProtected()
        {
            CarMemory car = new();

            Assert.Multiple(() =>
            {
                Assert.That(() => car[-1], Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => car[CarMemory.SizeInBytes - 3], Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => car.ReadWord(CarMemory.SizeInBytes - 1), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => car.ReadByte(CarMemory.SizeInBytes), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(car[CarMemory.SizeInBytes - 4], Is.Zero);
            });
        }
    }
}