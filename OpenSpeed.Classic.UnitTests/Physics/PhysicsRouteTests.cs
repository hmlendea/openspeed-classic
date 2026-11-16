using System;
using System.Buffers.Binary;
using System.IO;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PhysicsRouteTests
    {
        [Test]
        public void GivenNativeRouteBytes_WhenReading_ThenSignednessAndUnalignedLoadsArePreserved()
        {
            byte[] bytes = new byte[PhysicsRoute.RecordSize * 2];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, -0x10000);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(36), 0x10000);
            bytes[0x0C] = 0x80;
            bytes[0x0D] = 0x7F;
            bytes[0x0E] = 0xFF;
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(0x1A), -32);
            PhysicsRoute route = new(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(route.Count, Is.EqualTo(2));
                Assert.That(route.Position(0).First, Is.EqualTo(-0x10000));
                Assert.That(route.Direction(0, 0x0C).First, Is.EqualTo(-0x10000));
                Assert.That(route.Direction(0, 0x0C).Second, Is.EqualTo(0xFE00));
                Assert.That(route.Direction(0, 0x0C).Third, Is.EqualTo(-0x200));
                Assert.That(route.ReadDword(0, 9) >> 24, Is.EqualTo(-128));
                Assert.That(route.ReadWord(0, 0x1A), Is.EqualTo(-32));
                Assert.That(route.Heading(0), Is.EqualTo(0x100));
                Assert.That(route.Heading(1), Is.EqualTo(-0x100));
            });
        }

        [TestCase(0)]
        [TestCase(35)]
        [TestCase(37)]
        public void GivenInvalidRouteBytes_WhenDecoding_ThenTheInputIsRejected(int size)
            => Assert.That(() => new PhysicsRoute(new byte[size]), Throws.TypeOf<InvalidDataException>());
    }
}