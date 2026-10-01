using System;
using System.Buffers.Binary;
using System.IO;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class CarSpecificationsTests
    {
        [Test]
        public void GivenSourceBytes_WhenDecoding_ThenOnlyTheSpecifiedFieldsAreExchanged()
        {
            byte[] source = new byte[CarSpecifications.SourceSize];

            for (int offset = 0; offset < source.Length; offset += sizeof(int))
            {
                BinaryPrimitives.WriteInt32LittleEndian(source.AsSpan(offset), offset);
            }

            CarSpecifications descriptor = CarSpecifications.Decode(source);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor[0x140], Is.EqualTo(0x144));
                Assert.That(descriptor[0x144], Is.EqualTo(0x140));
                Assert.That(descriptor[0x160], Is.EqualTo(0x160));
                Assert.That(descriptor[0x164], Is.Zero);
                Assert.That(descriptor.Snapshot(), Has.Length.EqualTo(0x1DC));
            });
        }

        [Test]
        public void GivenATruncatedDescriptor_WhenDecoding_ThenTheInputIsRejected()
            => Assert.That(() => CarSpecifications.Decode(new byte[CarSpecifications.SourceSize - 1]),
                Throws.TypeOf<InvalidDataException>());

        [Test]
        public void GivenAGearDescriptor_WhenFinalising_ThenDerivedRatiosPreserveSourceArithmetic()
        {
            CarSpecifications descriptor = new();
            descriptor[0] = 0x10000;
            descriptor[4] = 3;
            descriptor[0x0C] = -0x10000;
            descriptor[0x14] = 0x10000;
            descriptor[0x2C] = 0x10000;
            descriptor[0x30] = 0x10000;
            descriptor[0x34] = 0x10000;
            descriptor[0x4C] = 0x10000;
            descriptor[0xF0] = 0x2000;
            descriptor[0xF4] = 0x10000;
            descriptor[0x124] = 0x10000;
            descriptor[0x138] = 0x10000;
            CarRuntimeType runtimeType = new();
            runtimeType[0x24] = 0x10000;
            CarSpecificationsFinaliser.Finalise(descriptor, runtimeType, 0, 0);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor[0x164], Is.EqualTo(8));
                Assert.That(descriptor[0x1D8], Is.EqualTo(0x10000));
                Assert.That(descriptor[0x188], Is.EqualTo(-0x10000));
                Assert.That(descriptor[0x18C], Is.EqualTo(0x28F));
                Assert.That(descriptor[0x190], Is.EqualTo(0x10000));
                Assert.That(descriptor[0x1B0], Is.EqualTo(0x1999));
                Assert.That(descriptor[0x170], Is.EqualTo(8));
                Assert.That(descriptor[0x1D4], Is.EqualTo(0x10000));
            });
        }

        [Test]
        public void GivenInterleavedTuning_WhenDecoding_ThenBothRowsRetainTheOriginalFieldOrder()
        {
            byte[] source = new byte[0xE8];

            for (int index = 0; index < source.Length / sizeof(int); index += 1)
            {
                BinaryPrimitives.WriteInt32LittleEndian(source.AsSpan(index * sizeof(int)), index);
            }

            SimulationTuning tuning = SimulationTuning.Decode(source);

            Assert.Multiple(() =>
            {
                Assert.That(tuning[0, 20], Is.EqualTo(20));
                Assert.That(tuning[0, 21], Is.EqualTo(21));
                Assert.That(tuning[0, 22], Is.EqualTo(23));
                Assert.That(tuning[0, 24], Is.EqualTo(27));
                Assert.That(tuning[0, 25], Is.EqualTo(22));
                Assert.That(tuning[0, 28], Is.EqualTo(28));
                Assert.That(tuning[1, 22], Is.EqualTo(52));
                Assert.That(tuning[1, 28], Is.EqualTo(57));
            });
        }
    }
}